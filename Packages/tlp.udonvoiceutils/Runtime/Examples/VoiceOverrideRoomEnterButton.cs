using TLP.UdonUtils;
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
    public class VoiceOverrideRoomEnterButton : TlpBaseBehaviour
    {
        #region Teleport settings
        [FormerlySerializedAs("optionalEnterLocation")]
        [Header("Teleport settings")]
        public Transform OptionalEnterLocation;
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

            if (VoiceOverrideRoom.Contains(localPlayer)) {
#if TLP_DEBUG
                Warn($"{localPlayer.ToStringSafe()} already in room");
#endif

                if (Utilities.IsValid(OptionalEnterLocation)) {
                    localPlayer.TeleportTo(
                            OptionalEnterLocation.position,
                            OptionalEnterLocation.rotation,
                            VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                            false
                    );
                }

                return;
            }

            if (!VoiceOverrideRoom.EnterRoom(localPlayer, OptionalEnterLocation)) {
                Error($"Failed to add {localPlayer} to {VoiceOverrideRoom.GetScriptPathInScene()}");
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