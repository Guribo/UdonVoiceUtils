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
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class VoiceOverrideRoomExitButton : TlpBaseBehaviour
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VoiceOverrideRoom.ExecutionOrder + 1;
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
            DebugLog($"{nameof(Interact)}: {Networking.LocalPlayer.ToStringSafe()}");
#endif
            #endregion

            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                Error("Local player invalid");
                return;
            }

            if (!VoiceOverrideRoom.Contains(localPlayer)) {
                Warn($"{localPlayer} was not in room");

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

            if (!VoiceOverrideRoom.ExitRoom(localPlayer, OptionalExitLocation)) {
                Error($"Failed to remove {localPlayer} from {VoiceOverrideRoom.GetScriptPathInScene()}");
            }
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(VoiceOverrideRoom)) {
                Error($"{nameof(VoiceOverrideRoom)} not set");
                return false;
            }

            Initialized = true;
            return true;
        }
        #endregion
    }
}