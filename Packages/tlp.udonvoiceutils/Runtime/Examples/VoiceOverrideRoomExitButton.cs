using TLP.UdonUtils;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceOverrideRoomExitButton : TlpBaseBehaviour
    {
        #region Teleport settings

        [Header("Teleport settings")]
        public Transform optionalExitLocation;

        #endregion

        #region Mandatory references

        [Header("Mandatory references")]
        public VoiceOverrideRoom voiceOverrideRoom;

        #endregion

        public override void Interact()
        {
            ExitRoom(Networking.LocalPlayer);
        }


        #region internal

        internal void ExitRoom(VRCPlayerApi playerApi)
        {
            if (!Assert(Utilities.IsValid(playerApi), "player invalid", this))
            {
                return;
            }

            if (!Assert(playerApi.isLocal, "player not local", this))
            {
                return;
            }

            if (!Assert(Utilities.IsValid(voiceOverrideRoom), "voiceOverrideRoom invalid", this))
            {
                return;
            }

            if (voiceOverrideRoom.Contains(playerApi))
            {
                Assert(
                    voiceOverrideRoom.ExitRoom(playerApi, optionalExitLocation),
                    "ExitRoom failed",
                    this
                );
            }
        }

        #endregion
    }
}