using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    /// <summary>
    /// Updates the far voice range of all players every time a player leaves or joins the world.
    /// </summary>
    public class AutoPlayerRange : UdonSharpBehaviour
    {
        [Tooltip("Defines the far voice range of all players depending on the number of players in the world")]
        public AnimationCurve playerFarRangeMapping = AnimationCurve.Linear(1,25,80,10);
        public override void OnPlayerJoined(VRC.SDKBase.VRCPlayerApi player)
        {
            UpdatePlayers();
        }
        
        public override void OnPlayerLeft(VRC.SDKBase.VRCPlayerApi player)
        {
            UpdatePlayers();
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

        public void UpdateVoiceRange(VRCPlayerApi[] playerApis, float voiceRange)
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

        internal void UpdatePlayers()
        {
            var playerCount = VRCPlayerApi.GetPlayerCount();
            var players = new VRCPlayerApi[playerCount];
            VRCPlayerApi.GetPlayers(players);
            var range = GetRange(playerFarRangeMapping, playerCount);

            UpdateVoiceRange(players, range);
        }
    }
}
