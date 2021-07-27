using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Runtime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BetterPlayerAudioOverrideList : UdonSharpBehaviour
    {
        internal BetterPlayerAudioOverride[] BetterPlayerAudioOverrides;

        public bool AddOverride(BetterPlayerAudioOverride voiceOverride)
        {
            if (!Utilities.IsValid(voiceOverride))
            {
                return false;
            }

            if (BetterPlayerAudioOverrides == null || BetterPlayerAudioOverrides.Length == 0)
            {
                BetterPlayerAudioOverrides = new[] {voiceOverride};
                return true;
            }
            
            foreach (var betterPlayerAudioOverride in BetterPlayerAudioOverrides)
            {
                if (betterPlayerAudioOverride == voiceOverride)
                {
                    return false;
                }
            }

            Remove(BetterPlayerAudioOverrides, voiceOverride);
            var slotsNeeded = Consolidate(BetterPlayerAudioOverrides) + 1;
            var insertIndex = GetInsertIndex(BetterPlayerAudioOverrides, voiceOverride);
            var tempList = new BetterPlayerAudioOverride[slotsNeeded];
            if (insertIndex > 0)
            {
                Array.ConstrainedCopy(BetterPlayerAudioOverrides, 0, tempList, 0, insertIndex);
            }

            tempList[insertIndex] = voiceOverride;
            if (slotsNeeded - insertIndex > 1)
            {
                Array.ConstrainedCopy(BetterPlayerAudioOverrides, 
                    insertIndex, 
                    tempList, 
                    insertIndex +1, 
                    slotsNeeded - insertIndex - 1);
            }

            BetterPlayerAudioOverrides = tempList;
            
            return true;
        }

        public BetterPlayerAudioOverride[] InsertNewOverride(BetterPlayerAudioOverride voiceOverride, int insertIndex,
            BetterPlayerAudioOverride[] tempList, BetterPlayerAudioOverride[] originalList)
        {
            if (insertIndex >= originalList.Length)
            {
                var tempList2 = new BetterPlayerAudioOverride[insertIndex + 1];
                tempList.CopyTo(tempList2, 0);
                tempList2[tempList2.Length - 1] = voiceOverride;
                tempList = tempList2;
            }
            else
            {
                tempList[insertIndex] = voiceOverride;
            }

            return tempList;
        }

        public BetterPlayerAudioOverride GetMaxPriority()
        {
            return Get(0);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns>the valid override or null if there is no valid override for the given index</returns>
        public BetterPlayerAudioOverride Get(int index)
        {
            if (index < 0
                || BetterPlayerAudioOverrides == null
                || BetterPlayerAudioOverrides.Length == 0
                || index >= BetterPlayerAudioOverrides.Length)
            {
                return null;
            }

            var betterPlayerAudioOverride = BetterPlayerAudioOverrides[index];
            if (Utilities.IsValid(betterPlayerAudioOverride))
            {
                return betterPlayerAudioOverride;
            }

            var remaining = Consolidate(BetterPlayerAudioOverrides);
            if (index < remaining)
            {
                return BetterPlayerAudioOverrides[index];
            }

            return null;
        }

        public int GetInsertIndex(BetterPlayerAudioOverride[] betterPlayerAudioOverrides,
            BetterPlayerAudioOverride voiceOverride)
        {
            if (betterPlayerAudioOverrides == null
                || !Utilities.IsValid(voiceOverride))
            {
                return -1;
            }

            var index = 0;
            foreach (var betterPlayerAudioOverride in betterPlayerAudioOverrides)
            {
                if (!Utilities.IsValid(betterPlayerAudioOverride))
                {
                    continue;
                }

                if (betterPlayerAudioOverride.priority > voiceOverride.priority)
                {
                    ++index;
                    continue;
                }

                break;
            }

            return index;
        }

        public int Consolidate(BetterPlayerAudioOverride[] list)
        {
            if (list == null)
            {
                return 0;
            }
            
            var valid = 0;
            var moveIndex = -1;
            for (var i = 0; i < list.Length; i++)
            {
                if (Utilities.IsValid(list[i]))
                {
                    ++valid;
                    if (moveIndex != -1)
                    {
                        list[moveIndex] = list[i];
                        list[i] = null;
                        ++moveIndex;
                    }
                }
                else
                {
                    // ensure that the entry no longer references an invalid object
                    list[i] = null;
                    if (moveIndex == -1)
                    {
                        moveIndex = i;
                    }
                }
            }

            return valid;
        }

        public bool Remove(BetterPlayerAudioOverride[] list, BetterPlayerAudioOverride betterPlayerAudioOverride)
        {
            if (list == null)
            {
                return false;
            }
            
            var removed = false;
            for (var i = 0; i < list.Length; i++)
            {
                if (list[i] == betterPlayerAudioOverride)
                {
                    list[i] = null;
                    removed = true;
                }
            }

            return removed;
        }

        public int RemoveOverride(BetterPlayerAudioOverride betterPlayerAudioOverride)
        {
            Remove(BetterPlayerAudioOverrides, betterPlayerAudioOverride);
            return Consolidate(BetterPlayerAudioOverrides);
        }
    }
}