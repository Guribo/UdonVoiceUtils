using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Adapters.Cyan;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Player;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonVoiceUtils.Runtime.Debugging
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class VoiceRangeVisualizer : CyanPooledObject
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioController.ExecutionOrder + 1;

        #region State
        private TrackingDataFollowerUI _playerFollower;
        #endregion


        [SerializeField]
        private Transform Near;

        [SerializeField]
        private Transform Far;

        [SerializeField]
        private Transform NearField;

        [SerializeField]
        private PlayerAudioController PlayerAudioController;


        #region Event Data
        public bool VoiceLowpass;
        public float VoiceGain;
        public float VoiceDistanceFar;
        public float VoiceDistanceNear;
        public float VoiceVolumetricRadius;
        #endregion

        private int _playerId = -1;

        public override void _OnOwnerSet() {
            base._OnOwnerSet();

            _playerId = Owner.PlayerIdSafe();
            if (!Utilities.IsValid(Owner)) {
                Error($"{nameof(Owner)} is invalid");
                return;
            }

            if (!Utilities.IsValid(PlayerAudioController)) {
                Error($"{nameof(PlayerAudioController)} is invalid");
                return;
            }

            PlayerAudioController.PlayerUpdateListeners[_playerId] = this;

            gameObject.GetComponent<TrackingDataFollower>().Player = Owner;
        }

        public override void _OnCleanup() {
            base._OnCleanup();
            gameObject.SetActive(false);
            if (!Utilities.IsValid(PlayerAudioController)) {
                Error($"{nameof(PlayerAudioController)} is invalid");
                return;
            }

            if (!PlayerAudioController.PlayerUpdateListeners.Remove(_playerId)) {
                Error($"Failed to remove listener of player {_playerId}");
            }
        }

        [PublicAPI]
        public void VoiceValuesUpdate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            //DebugLog(nameof(VoiceValuesUpdate));
#endif
            #endregion

            if (!Utilities.IsValid(Owner)) {
                return;
            }

            gameObject.SetActive(!Owner.isLocal);

            if (Near) {
                // _player.Voice(); // TODO getting values is not yet implemented, wait for resolve of https://vrchat.canny.io/vrchat-udon-closed-alpha-feedback/p/getters-for-player-audio-settings
                Near.localScale = new Vector3(VoiceDistanceNear, VoiceDistanceNear, VoiceDistanceNear);
            }

            if (Far) {
                Far.localScale = new Vector3(VoiceDistanceFar, VoiceDistanceFar, VoiceDistanceFar);
            }

            if (NearField) {
                NearField.localScale = new Vector3(VoiceVolumetricRadius, VoiceVolumetricRadius, VoiceVolumetricRadius);
            }
        }

        public override void Start(){
            base.Start();

            _playerFollower = GetComponent<TrackingDataFollowerUI>();
            if (!Utilities.IsValid(_playerFollower)) {
                ErrorAndDisableGameObject($"{nameof(TrackingDataFollowerUI)} component missing on {name}");
                return;
            }

            if (Utilities.IsValid(PlayerAudioController)) {
                return;
            }

            ErrorAndDisableGameObject($"{nameof(PlayerAudioController)} not set");
        }
    }
}