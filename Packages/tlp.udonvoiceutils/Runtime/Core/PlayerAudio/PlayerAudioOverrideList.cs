using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio.PlayerOcclusion;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerAudioOverrideList), ExecutionOrder)]
    public class PlayerAudioOverrideList : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerOcclusionStrategy.ExecutionOrder + 10;

        // start with an initial size of 3
        internal PlayerAudioOverride[] PlayerAudioOverrides;
        internal PlayerAudioOverride[] TempList;

        #region public API
        [PublicAPI]
        public int Overrides { get; internal set; }

        [PublicAPI]
        public bool _AddOverride(PlayerAudioOverride voiceOverride) {
#if TLP_DEBUG
            _DebugLog(nameof(_AddOverride));
#endif
            if (!Utilities.IsValid(voiceOverride)) {
                _Error($"{nameof(voiceOverride)} invalid");
                return false;
            }

            if (Overrides == 0) {
                if (PlayerAudioOverrides == null) {
                    PlayerAudioOverrides = new PlayerAudioOverride[3];
                }

                PlayerAudioOverrides[0] = voiceOverride;
                Overrides = 1;
                return true;
            }

            Overrides = _RemoveOverride(voiceOverride);
            int slotsNeeded = Overrides + 1;

            int insertIndex = _GetInsertIndex(PlayerAudioOverrides, Overrides, voiceOverride);
            if (TempList == null || TempList.Length < slotsNeeded) {
                TempList = new PlayerAudioOverride[slotsNeeded];
            }

            if (insertIndex > 0) {
                Array.ConstrainedCopy(PlayerAudioOverrides, 0, TempList, 0, insertIndex);
            }

            TempList[insertIndex] = voiceOverride;
            if (slotsNeeded - insertIndex > 1) {
                Array.ConstrainedCopy(
                        PlayerAudioOverrides,
                        insertIndex,
                        TempList,
                        insertIndex + 1,
                        slotsNeeded - insertIndex - 1
                );
            }

            // ReSharper disable once SwapViaDeconstruction
            var tmp = PlayerAudioOverrides;
            PlayerAudioOverrides = TempList;
            TempList = tmp;
            Overrides = slotsNeeded;

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="playerAudioOverride"></param>
        /// <returns>number of remaining valid overrides</returns>
        [PublicAPI]
        public int _RemoveOverride(PlayerAudioOverride playerAudioOverride) {
#if TLP_DEBUG
            _DebugLog(nameof(_RemoveOverride));
#endif
            bool unused = _Remove(PlayerAudioOverrides, Overrides, playerAudioOverride);
            Overrides = _Consolidate(PlayerAudioOverrides);
            return Overrides;
        }

        [PublicAPI]
        public bool _GetMaxPriority(VRCPlayerApi player, out PlayerAudioOverride playerAudioOverride) {
            for (int i = 0; i < Overrides; i++) {
                var entry = _Get(i);
                if (!Utilities.IsValid(entry)) {
                    playerAudioOverride = null;
                    return false;
                }

                if (entry._IsPlayerBlackListed(player)) {
                    continue;
                }

                playerAudioOverride = entry;
                return true;
            }

            playerAudioOverride = null;
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns>the valid override or null if there is no valid override for the given index</returns>
        [PublicAPI]
        public PlayerAudioOverride _Get(int index) {
#if TLP_DEBUG
            _DebugLog(nameof(_Get));
#endif

            if (index < 0) {
                return null;
            }

            if (index >= Overrides) {
                return null;
            }

            if (PlayerAudioOverrides._LengthSafe() == 0) {
                return null;
            }

            if (Overrides > PlayerAudioOverrides.Length) {
                _Error("Overrides <= PlayerAudioOverrides.Length failed");
                return null;
            }

            Overrides = _Consolidate(PlayerAudioOverrides);
            return index < Overrides ? PlayerAudioOverrides[index] : null;
        }
        #endregion

        #region internals
        internal int _GetInsertIndex(
                PlayerAudioOverride[] overrides,
                int length,
                PlayerAudioOverride voiceOverride
        ) {
            if (overrides == null
                || !Utilities.IsValid(voiceOverride)) {
                return -1;
            }

            int index = 0;
            for (int i = 0; i < length; i++) {
                var entry = overrides[i];
                if (!Utilities.IsValid(entry)) {
                    continue;
                }

                if (entry.Priority > voiceOverride.Priority) {
                    ++index;
                    continue;
                }

                break;
            }

            return index;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="list"></param>
        /// <returns>number of remaining valid overrides</returns>
        internal static int _Consolidate(PlayerAudioOverride[] list) {
            if (list == null) {
                return 0;
            }

            int valid = 0;
            int moveIndex = -1;
            for (int i = 0; i < list.Length; i++) {
                valid = _ShiftValidEntries(list, i, valid, ref moveIndex);
            }

            return valid;
        }

        private static int _ShiftValidEntries(PlayerAudioOverride[] list, int i, int valid, ref int moveIndex) {
            if (Utilities.IsValid(list[i])) {
                ++valid;
                if (moveIndex == -1) {
                    return valid;
                }

                list[moveIndex] = list[i];
                list[i] = null;
                ++moveIndex;
            } else {
                // ensure that the entry no longer references an invalid object
                list[i] = null;
                if (moveIndex == -1) {
                    moveIndex = i;
                }
            }

            return valid;
        }

        internal static bool _Remove(PlayerAudioOverride[] list, int length, PlayerAudioOverride playerAudioOverride) {
            if (list == null) {
                return false;
            }

            bool removed = false;
            for (int i = 0; i < length; i++) {
                if (list[i] != playerAudioOverride) {
                    continue;
                }

                list[i] = null;
                removed = true;
                break;
            }

            return removed;
        }
        #endregion

        public override void _OnPrepareForReturnToPool() {
            base._OnPrepareForReturnToPool();

            PlayerAudioOverrides = null;
            TempList = null;
            Overrides = 0;
        }

        public override void _OnReadyForUse() {
            base._OnReadyForUse();

            PlayerAudioOverrides = null;
            TempList = null;
            Overrides = 0;
        }

        public bool _Contains(PlayerAudioOverride playerAudioOverride) {
            if (!Utilities.IsValid(playerAudioOverride)) {
                _Error($"{nameof(playerAudioOverride)} invalid");
                return false;
            }

            if (PlayerAudioOverrides._LengthSafe() < 1) return false;
            for (int i = 0; i < Overrides; i++) {
                if (ReferenceEquals(PlayerAudioOverrides[i], playerAudioOverride)) {
                    return true;
                }
            }

            return false;
        }
    }
}