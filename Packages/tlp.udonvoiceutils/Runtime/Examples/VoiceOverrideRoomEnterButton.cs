using TLP.UdonUtils;
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

        public override void Interact() {
#if TLP_DEBUG
            DebugLog(nameof(Interact));
#endif
            EnterRoom(Networking.LocalPlayer);
        }

        internal void EnterRoom(VRCPlayerApi playerApi) {
#if TLP_DEBUG
            DebugLog(nameof(EnterRoom));
#endif
            if (!Assert(Utilities.IsValid(playerApi), "Invalid Player tried entering a room", this)) {
                return;
            }

            if (!playerApi.isLocal) {
                return;
            }

            if (!Assert(Utilities.IsValid(VoiceOverrideRoom), "VoiceOverrideRoom invalid", this)) {
                return;
            }

            if (!VoiceOverrideRoom.Contains(playerApi)) {
                Assert(
                        VoiceOverrideRoom.EnterRoom(playerApi, OptionalEnterLocation),
                        "EnterRoom failed",
                        this
                );
            }
        }
    }
}