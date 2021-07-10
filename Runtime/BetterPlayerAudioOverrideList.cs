using System;
using UdonSharp;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Runtime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BetterPlayerAudioOverrideList : UdonSharpBehaviour
    {
        private BetterPlayerAudioOverride[] _betterPlayerAudioOverride;

        public bool AddOverride(BetterPlayerAudioOverride voiceOverride)
        {
            if (!Utilities.IsValid(voiceOverride))
            {
                return false;
            }

            if (_betterPlayerAudioOverride == null || _betterPlayerAudioOverride.Length == 0)
            {
                _betterPlayerAudioOverride = new[] {voiceOverride};
                return true;
            }
            
            foreach (var betterPlayerAudioOverride in _betterPlayerAudioOverride)
            {
                if (betterPlayerAudioOverride == voiceOverride)
                {
                    return false;
                }
            }

            Remove(_betterPlayerAudioOverride, voiceOverride);
            var slotsNeeded = Consolidate(_betterPlayerAudioOverride) + 1;
            var insertIndex = GetInsertIndex(_betterPlayerAudioOverride, voiceOverride);
            var tempList = new BetterPlayerAudioOverride[slotsNeeded];
            if (insertIndex > 0)
            {
                Array.ConstrainedCopy(_betterPlayerAudioOverride, 0, tempList, 0, insertIndex);
            }

            tempList[insertIndex] = voiceOverride;
            if (slotsNeeded - insertIndex > 1)
            {
                Array.ConstrainedCopy(_betterPlayerAudioOverride, 
                    insertIndex, 
                    tempList, 
                    insertIndex +1, 
                    slotsNeeded - insertIndex - 1);
            }

            _betterPlayerAudioOverride = tempList;
            
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

        public int CopyHighPriorityOverridesToNewList(BetterPlayerAudioOverride[] betterPlayerAudioOverrides,
            BetterPlayerAudioOverride[] tempList,
            int insertIndex)
        {
            var copied = 0;
            var endIndex = 0;
            foreach (var betterPlayerAudioOverride in betterPlayerAudioOverrides)
            {
                endIndex++;
                if (copied == insertIndex)
                {
                    break;
                }

                if (Utilities.IsValid(betterPlayerAudioOverride))
                {
                    tempList[copied] = betterPlayerAudioOverride;
                    ++copied;
                }
            }

            return endIndex;
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns>the valid override or null if there is no valid override for the given index</returns>
        public BetterPlayerAudioOverride Get(int index)
        {
            if (index < 0
                || _betterPlayerAudioOverride == null
                || _betterPlayerAudioOverride.Length == 0
                || index >= _betterPlayerAudioOverride.Length)
            {
                return null;
            }

            var betterPlayerAudioOverride = _betterPlayerAudioOverride[index];
            if (Utilities.IsValid(betterPlayerAudioOverride))
            {
                return betterPlayerAudioOverride;
            }

            if (Consolidate(_betterPlayerAudioOverride) == 0)
            {
                return null;
            }
            return _betterPlayerAudioOverride[index];
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
            Remove(_betterPlayerAudioOverride, betterPlayerAudioOverride);
            return Consolidate(_betterPlayerAudioOverride);
        }
    }
}