using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonVoiceUtils.Runtime.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Debugging
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VoiceRangeVisualizer), ExecutionOrder)]
    public class VoiceRangeVisualizer : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = AdjustableGain.ExecutionOrder + 1;

        #region State
        private VRCPlayerApi _owner;
        #endregion

        public PlayerAudioController PlayerAudioController;

        [SerializeField]
        private Transform Near;

        [SerializeField]
        private Transform Far;

        [SerializeField]
        private Transform NearField;

        public bool Initialized { private set; get; }

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!IsSet(PlayerAudioController, nameof(PlayerAudioController))) {
                return false;
            }

            _owner = Networking.GetOwner(gameObject);
            if (!IsSet(_owner, nameof(_owner))) {
                return false;
            }

            Initialized = true;
            return true;
        }

        public override void PostLateUpdate() {
            base.PostLateUpdate();
            if (!HasStartedOk) {
                return;
            }

            PlayerAudioController.GetRemotePlayerAudioListenerTransform(_owner, out var pos, out var rot);
            transform.SetPositionAndRotation(pos, rot);
            VoiceValuesUpdate();
        }
        #endregion

        #region Events
        private void VoiceValuesUpdate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(VoiceValuesUpdate));
#endif
            #endregion

            gameObject.SetActive(!_owner.isLocal);

            if (Near) {
                float voiceDistanceNear = _owner.GetVoiceDistanceNear();
                Near.localScale = new Vector3(voiceDistanceNear, voiceDistanceNear, voiceDistanceNear);
            }

            if (Far) {
                float voiceDistanceFar = _owner.GetVoiceDistanceFar();
                Far.localScale = new Vector3(voiceDistanceFar, voiceDistanceFar, voiceDistanceFar);
            }

            if (NearField) {
                float voiceVolumetricRadius = _owner.GetVoiceVolumetricRadius();
                NearField.localScale = new Vector3(voiceVolumetricRadius, voiceVolumetricRadius, voiceVolumetricRadius);
            }
        }
        #endregion
    }
}