using Guribo.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    /// <summary>
    /// All players that are in contact with the trigger are affected. Exiting the trigger removes the player from the
    /// associated voice override.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VoiceOverrideTriggerZone : UdonSharpBehaviour
    {
        #region Reverb settings

        [Header("Reverb settings")]
        public AudioReverbFilter optionalReverb;

        #endregion
        
        #region Mandatory references

        [Header("Mandatory references")]
        public BetterPlayerAudioOverride playerAudioOverride;
        
        #endregion
        

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) // do nothing for invalid players
                || !Utilities.IsValid(playerAudioOverride)) // or when the required components are invalid)
            {
                return;
            }

            // add the player to the override
            playerAudioOverride.AddPlayer(player);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) // do nothing for invalid players
                || !Utilities.IsValid(playerAudioOverride)) // or when the required components are invalid
            {
                return;
            }

            // remove player from the override
            playerAudioOverride.RemovePlayer(player);
        }
    }
}
