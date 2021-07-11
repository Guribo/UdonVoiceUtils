using Guribo.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VoiceOverrideRoomExitButton : UdonSharpBehaviour
    {
        #region Teleport settings

        [Header("Teleport settings")]
        public Transform optionalExitLocation;
        public bool exitZoneOnRespawn = true;

        #endregion

        #region Mandatory references

        [Header("Mandatory references")]
        public VoiceOverrideRoom voiceOverrideRoom;
        public UdonDebug udonDebug;

        #endregion

        public override void Interact()
        {
            ExitRoom(Networking.LocalPlayer);
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!exitZoneOnRespawn)
            {
                return;
            }

            if (!udonDebug.Assert(Utilities.IsValid(voiceOverrideRoom), "overrideZoneActivator invalid", this))
            {
                return;
            }
            
            if (voiceOverrideRoom.exitZoneOnRespawn)
            {
                return;
            }
            
            ExitRoom(player);
        }

        private void ExitRoom(VRCPlayerApi playerApi)
        {
            if (!(udonDebug.Assert(Utilities.IsValid(voiceOverrideRoom), "overrideZoneActivator invalid", this)
                  && udonDebug.Assert(Utilities.IsValid(playerApi), "player invalid", this)
                  && playerApi.isLocal))
            {
                return;
            }

            if (voiceOverrideRoom.Contains(playerApi))
            {
                udonDebug.Assert(voiceOverrideRoom.ExitRoom(playerApi, optionalExitLocation), "ExitRoom failed",
                    this);
            }
        }
    }
}