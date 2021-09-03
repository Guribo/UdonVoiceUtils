using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    /// <summary>
    /// Updates the far voice range of all players every time a player leaves or joins the world.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AutoPlayerRange : UdonSharpBehaviour
    {
        /// <summary>
        /// default voice far range defined by VRChat
        /// <remarks>https://docs.vrchat.com/docs/playewar-audio#set-voice-distance-far</remarks>
        /// </summary>
        internal const float DefaultRange = 25f;
        
        [Tooltip("Defines the far voice range of all players depending on the number of players in the world")]
        public AnimationCurve playerFarRangeMapping = AnimationCurve.Linear(1,25,80,10);

        internal void OnEnable()
        {
            UpdatePlayerVoiceRange(false);
        }

        internal void OnDisable()
        {
            UpdatePlayerVoiceRange(true);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            UpdatePlayerVoiceRange(false);
        }
        
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            UpdatePlayerVoiceRange(false);
        }

        internal float GetRange(AnimationCurve rangeMapping, int playerCount)
        {
            if (!Utilities.IsValid(rangeMapping))
            {
                Debug.LogWarning("playerRangeMapping invalid, returning default range 25");
                return 25f;
            }

            return rangeMapping.Evaluate(playerCount);
        }

        internal void UpdateVoiceRange(VRCPlayerApi[] playerApis, float voiceRange)
        {
            if (!Utilities.IsValid(playerApis))
            {
                return;
            }

            foreach (var vrcPlayerApi in playerApis)
            {
                if (!Utilities.IsValid(vrcPlayerApi))
                {
                    continue;
                }
                vrcPlayerApi.SetVoiceDistanceFar(voiceRange);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reset">if true all player voices are reset to use the <see cref="DefaultRange"/></param>
        internal void UpdatePlayerVoiceRange(bool reset)
        {
            var playerCount = VRCPlayerApi.GetPlayerCount();
            var players = new VRCPlayerApi[playerCount];
            VRCPlayerApi.GetPlayers(players);
            
            var range = reset ? DefaultRange : GetRange(playerFarRangeMapping, playerCount);
            UpdateVoiceRange(players, range);
        }
    }
}
