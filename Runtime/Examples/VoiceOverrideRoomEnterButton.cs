using Guribo.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VoiceOverrideRoomEnterButton : UdonSharpBehaviour
    {
        #region Teleport settings

        [Header("Teleport settings")]
        public Transform optionalEnterLocation;

        #endregion

        #region Mandatory references

        [Header("Mandatory references")]
        public VoiceOverrideRoom voiceOverrideRoom;
        public UdonDebug udonDebug;

        #endregion

        public override void Interact()
        {
            EnterRoom(Networking.LocalPlayer);
        }

        private void EnterRoom(VRCPlayerApi playerApi)
        {
            if (!(udonDebug.Assert(Utilities.IsValid(voiceOverrideRoom), "overrideZoneActivator invalid", this)
                  && udonDebug.Assert(Utilities.IsValid(playerApi), "player invalid", this)
                  && playerApi.isLocal))
            {
                return;
            }

            if (!voiceOverrideRoom.Contains(playerApi))
            {
                udonDebug.Assert(voiceOverrideRoom.EnterRoom(playerApi, optionalEnterLocation), "EnterRoom failed",
                    this);
            }
        }
    }
}