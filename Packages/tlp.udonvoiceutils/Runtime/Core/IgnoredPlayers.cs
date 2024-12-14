using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    /// <summary>
    /// TODO Use Blacklist instead?
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(IgnoredPlayers), ExecutionOrder)]
    public class IgnoredPlayers : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.AudioStart + 10;

        #region State
        internal readonly DataDictionary IgnoredPlayerIds = new DataDictionary();
        #endregion

        #region Public
        public bool Add(VRCPlayerApi player) {
            // validate the player
            if (!Utilities.IsValid(player)) {
                Error($"{nameof(player)} invalid");
                return false;
            }

            if (IgnoredPlayerIds.ContainsKey(player.playerId)) {
                Error($"{player.ToStringSafe()} is already ignored");
                return false;
            }

            var ignoredPlayers = IgnoredPlayerIds.GetKeys();
            for (int i = 0; i < ignoredPlayers.Count; i++) {
                var playerId = ignoredPlayers[i];
                if (!playerId.Int.IsValidPlayer(out var unused)) {
                    IgnoredPlayerIds.Remove(playerId.Int);
                }
            }

            IgnoredPlayerIds.Add(player.playerId, new DataToken());
            return true;
        }

        public bool Remove(VRCPlayerApi player) {
            if (!Utilities.IsValid(player)) {
                Error($"{nameof(player)} invalid");
                return false;
            }

            if (!IgnoredPlayerIds.ContainsKey(player.playerId)) {
                Warn($"{player.ToStringSafe()} was not ignored");
                return false;
            }

            var ignoredPlayers = IgnoredPlayerIds.GetKeys();
            for (int i = 0; i < ignoredPlayers.Count; i++) {
                var playerId = ignoredPlayers[i];
                if (!playerId.Int.IsValidPlayer(out var unused)) {
                    IgnoredPlayerIds.Remove(playerId.Int);
                }
            }

            return IgnoredPlayerIds.Remove(player.playerId);
        }

        public bool Contains(int playerId) {
            return IgnoredPlayerIds.ContainsKey(playerId);
        }
        #endregion
    }
}