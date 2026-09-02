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
    [TlpDefaultExecutionOrder(typeof(VoiceOverrideRoomEnterButton), ExecutionOrder)]
    public class VoiceOverrideRoomEnterButton : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VoiceOverrideRoomExitButton.ExecutionOrder + 1;

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

            if (VoiceOverrideRoom._Contains(localPlayer)) {
                _Warn($"{localPlayer._ToStringSafe()} already in room");

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

            if (!VoiceOverrideRoom._EnterRoom(localPlayer, OptionalEnterLocation)) {
                _Error($"Failed to add {localPlayer} to {VoiceOverrideRoom._GetScriptPathInScene()}");
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