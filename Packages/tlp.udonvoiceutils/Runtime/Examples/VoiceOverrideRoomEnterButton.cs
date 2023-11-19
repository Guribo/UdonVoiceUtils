using TLP.UdonUtils;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceOverrideRoomEnterButton : TlpBaseBehaviour
    {
        #region Teleport settings

        [Header("Teleport settings")]
        public Transform optionalEnterLocation;

        #endregion

        #region Mandatory references

        [Header("Mandatory references")]
        public VoiceOverrideRoom voiceOverrideRoom;

        #endregion

        public override void Interact()
        {
#if TLP_DEBUG
            DebugLog(nameof(Interact));
#endif
            EnterRoom(Networking.LocalPlayer);
        }

        internal void EnterRoom(VRCPlayerApi playerApi)
        {
#if TLP_DEBUG
            DebugLog(nameof(EnterRoom));
#endif
            if (!Assert(Utilities.IsValid(playerApi), "Invalid Player tried entering a room", this))
            {
                return;
            }

            if (!playerApi.isLocal)
            {
                return;
            }

            if (!Assert(Utilities.IsValid(voiceOverrideRoom), "VoiceOverrideRoom invalid", this))
            {
                return;
            }

            if (!voiceOverrideRoom.Contains(playerApi))
            {
                Assert(
                    voiceOverrideRoom.EnterRoom(playerApi, optionalEnterLocation),
                    "EnterRoom failed",
                    this
                );
            }
        }
    }
}