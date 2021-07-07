using Guribo.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class OverrideZoneExit : UdonSharpBehaviour
    {
        #region Teleport settings

        [Header("Teleport settings")]
        public Transform optionalExitLocation;
        public bool exitZoneOnRespawn = true;

        #endregion

        #region Mandatory references

        [Header("Mandatory references")]
        public OverrideZoneActivator overrideZoneActivator;
        public UdonDebug udonDebug;

        #endregion

        public override void Interact()
        {
            ExitZone(Networking.LocalPlayer);
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!exitZoneOnRespawn)
            {
                return;
            }
            
            ExitZone(player);
        }

        private void ExitZone(VRCPlayerApi playerApi)
        {
            if (!(udonDebug.Assert(Utilities.IsValid(overrideZoneActivator), "overrideZoneActivator invalid", this)
                  && udonDebug.Assert(Utilities.IsValid(playerApi), "player invalid", this)
                  && playerApi.isLocal))
            {
                return;
            }

            if (overrideZoneActivator.Contains(playerApi))
            {
                udonDebug.Assert(overrideZoneActivator.ExitZone(playerApi, optionalExitLocation), "ExitZone failed",
                    this);
            }
        }
    }
}