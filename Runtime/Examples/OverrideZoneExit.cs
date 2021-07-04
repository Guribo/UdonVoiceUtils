using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class OverrideZoneExit : UdonSharpBehaviour
    {
        public OverrideZoneActivator overrideZoneActivator;
        public Transform optionalExitLocation;
        public bool exitZoneOnRespawn = true;
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
            if (!Utilities.IsValid(overrideZoneActivator) 
                || !Utilities.IsValid(playerApi) 
                || !playerApi.isLocal)
            {
                return;
            }

            overrideZoneActivator.ExitZone(playerApi, optionalExitLocation);
        }
    }
}
