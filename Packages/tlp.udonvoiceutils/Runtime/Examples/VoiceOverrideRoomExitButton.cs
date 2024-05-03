using TLP.UdonUtils;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceOverrideRoomExitButton : TlpBaseBehaviour
    {
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

        public override void Interact() {
            ExitRoom(Networking.LocalPlayer);
        }


        #region internal
        internal void ExitRoom(VRCPlayerApi playerApi) {
            if (!Assert(Utilities.IsValid(playerApi), "player invalid", this)) {
                return;
            }

            if (!Assert(playerApi.isLocal, "player not local", this)) {
                return;
            }

            if (!Assert(Utilities.IsValid(VoiceOverrideRoom), "voiceOverrideRoom invalid", this)) {
                return;
            }

            if (VoiceOverrideRoom.Contains(playerApi)) {
                Assert(
                        VoiceOverrideRoom.ExitRoom(playerApi, OptionalExitLocation),
                        "ExitRoom failed",
                        this
                );
            }
        }
        #endregion
    }
}