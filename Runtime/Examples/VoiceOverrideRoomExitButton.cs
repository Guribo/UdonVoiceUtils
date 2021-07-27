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


        #region internal

        internal void ExitRoom(VRCPlayerApi playerApi)
        {
            if (!udonDebug.Assert(Utilities.IsValid(playerApi), "player invalid", this))
            {
                return;
            }
            
            if (!udonDebug.Assert(playerApi.isLocal, "player not local", this))
            {
                return;
            }
            
            if (!udonDebug.Assert(Utilities.IsValid(voiceOverrideRoom), "voiceOverrideRoom invalid", this))
            {
                return;
            }

            if (voiceOverrideRoom.Contains(playerApi))
            {
                udonDebug.Assert(voiceOverrideRoom.ExitRoom(playerApi, optionalExitLocation), "ExitRoom failed",
                    this);
            }
        }
        
        #endregion
    }
}