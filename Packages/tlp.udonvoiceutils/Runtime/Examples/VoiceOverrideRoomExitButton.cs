using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VoiceOverrideRoomExitButton), ExecutionOrder)]
    public class VoiceOverrideRoomExitButton : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VoiceOverrideDoor.ExecutionOrder + 1;
        #endregion
        #region Teleport settings
        [FormerlySerializedAs("optionalExitLocation")]
        [Header("Teleport settings")]
        public Transform OptionalExitLocation;
        #endregion

        #region Mandatory references
        [FormerlySerializedAs("voiceOverrideRoom")]
        [Header("Mandatory references")]
        public VoiceOverrideRoom VoiceOverrideRoom;
        #endregion

        #region State
        internal bool Initialized { get; private set; }
        #endregion

        #region Player Events
        public override void Interact() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(Interact)}: {Networking.LocalPlayer._ToStringSafe()}");
#endif
            #endregion

            if (!Initialized) {
                _Error("Not initialized");
                return;
            }

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                _Error("Local player invalid");
                return;
            }

            if (!VoiceOverrideRoom._Contains(localPlayer)) {
                _Warn($"{localPlayer} was not in room");

                if (Utilities.IsValid(OptionalExitLocation)) {
                    localPlayer.TeleportTo(
                            OptionalExitLocation.position,
                            OptionalExitLocation.rotation,
                            VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                            false
                    );
                }
                return;
            }

            if (!VoiceOverrideRoom._ExitRoom(localPlayer, OptionalExitLocation)) {
                _Error($"Failed to remove {localPlayer} from {VoiceOverrideRoom._GetScriptPathInScene()}");
            }
        }
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(VoiceOverrideRoom)) {
                _Error($"{nameof(VoiceOverrideRoom)} not set");
                return false;
            }

            Initialized = true;
            return true;
        }
        #endregion
    }
}