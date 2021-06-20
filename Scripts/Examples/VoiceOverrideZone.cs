using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Scripts.Examples
{
    public class VoiceOverrideZone : UdonSharpBehaviour
    {
        public BetterPlayerAudioOverride playerAudioOverride;
        

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) // do nothing for invalid players
                || !Utilities.IsValid(playerAudioOverride)) // or when the required components are invalid)
            {
                return;
            }

            // add the player to the override
            playerAudioOverride.AffectPlayer(player);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) // do nothing for invalid players
                || !Utilities.IsValid(playerAudioOverride)) // or when the required components are invalid
            {
                return;
            }

            // remove player from the override
            playerAudioOverride.RemoveAffectedPlayer(player);
        }
    }
}
