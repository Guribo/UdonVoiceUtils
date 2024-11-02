using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerAudioOverrideList), ExecutionOrder)]
    public class PlayerAudioOverrideList : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioOverride.ExecutionOrder + 1;

        // start with an initial size of 3
        internal PlayerAudioOverride[] PlayerAudioOverrides;
        internal PlayerAudioOverride[] TempList;

        #region public API
        [PublicAPI]
        public int Overrides { get; internal set; }

        [PublicAPI]
        public bool AddOverride(PlayerAudioOverride voiceOverride) {
#if TLP_DEBUG
            DebugLog(nameof(AddOverride));
#endif
            if (!Utilities.IsValid(voiceOverride)) {
                Error($"{nameof(voiceOverride)} invalid");
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

            Overrides = RemoveOverride(voiceOverride);
            int slotsNeeded = Overrides + 1;

            int insertIndex = GetInsertIndex(PlayerAudioOverrides, Overrides, voiceOverride);
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
        public int RemoveOverride(PlayerAudioOverride playerAudioOverride) {
#if TLP_DEBUG
            DebugLog(nameof(RemoveOverride));
#endif
            Remove(PlayerAudioOverrides, Overrides, playerAudioOverride);
            Overrides = Consolidate(PlayerAudioOverrides);
            return Overrides;
        }

        [PublicAPI]
        public PlayerAudioOverride GetMaxPriority(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(GetMaxPriority));
#endif

            for (int i = 0; i < Overrides; i++) {
                var entry = Get(i);
                if (!entry) {
                    return null;
                }

                if (entry.IsPlayerBlackListed(player)) {
                    continue;
                }

                return entry;
            }

            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns>the valid override or null if there is no valid override for the given index</returns>
        [PublicAPI]
        public PlayerAudioOverride Get(int index) {
#if TLP_DEBUG
            DebugLog(nameof(Get));
#endif

            if (index < 0) {
                return null;
            }

            if (index >= Overrides) {
                return null;
            }

            if (PlayerAudioOverrides.LengthSafe() == 0) {
                return null;
            }

            if (Overrides > PlayerAudioOverrides.Length) {
                Error("Overrides <= PlayerAudioOverrides.Length failed");
                return null;
            }

            Overrides = Consolidate(PlayerAudioOverrides);
            return index < Overrides ? PlayerAudioOverrides[index] : null;
        }
        #endregion

        #region internals
        internal int GetInsertIndex(
                PlayerAudioOverride[] betterPlayerAudioOverrides,
                int length,
                PlayerAudioOverride voiceOverride
        ) {
            if (betterPlayerAudioOverrides == null
                || !Utilities.IsValid(voiceOverride)) {
                return -1;
            }

            int index = 0;
            for (int i = 0; i < length; i++) {
                var betterPlayerAudioOverride = betterPlayerAudioOverrides[i];
                if (!Utilities.IsValid(betterPlayerAudioOverride)) {
                    continue;
                }

                if (betterPlayerAudioOverride.Priority > voiceOverride.Priority) {
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
        internal int Consolidate(PlayerAudioOverride[] list) {
            if (list == null) {
                return 0;
            }

            int valid = 0;
            int moveIndex = -1;
            for (int i = 0; i < list.Length; i++) {
                if (Utilities.IsValid(list[i])) {
                    ++valid;
                    if (moveIndex != -1) {
                        list[moveIndex] = list[i];
                        list[i] = null;
                        ++moveIndex;
                    }
                } else {
                    // ensure that the entry no longer references an invalid object
                    list[i] = null;
                    if (moveIndex == -1) {
                        moveIndex = i;
                    }
                }
            }

            return valid;
        }

        internal bool Remove(PlayerAudioOverride[] list, int length, PlayerAudioOverride playerAudioOverride) {
            if (list == null) {
                return false;
            }

            bool removed = false;
            for (int i = 0; i < length; i++) {
                if (list[i] == playerAudioOverride) {
                    list[i] = null;
                    removed = true;
                    break;
                }
            }

            return removed;
        }
        #endregion

        public override void OnPrepareForReturnToPool() {
            base.OnPrepareForReturnToPool();

            PlayerAudioOverrides = null;
            TempList = null;
            Overrides = 0;
        }

        public override void OnReadyForUse() {
            base.OnReadyForUse();

            PlayerAudioOverrides = null;
            TempList = null;
            Overrides = 0;
        }

        public bool Contains(PlayerAudioOverride playerAudioOverride) {
            if (!Utilities.IsValid(playerAudioOverride)) {
                Error($"{nameof(playerAudioOverride)} invalid");
                return false;
            }

            if (PlayerAudioOverrides.LengthSafe() < 1) return false;
            for (int i = 0; i < Overrides; i++) {
                if (ReferenceEquals(PlayerAudioOverrides[i], playerAudioOverride)) {
                    return true;
                }
            }

            return false;
        }
    }
}