using TLP.UdonUtils.Runtime.Extensions;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    public static class ArrayUtils
    {
        public static int[] _CreateExponentialBuckets(
                int totalElements,
                int bucketCount = 8,
                float growthFactor = 2,
                bool smallestFirst = true
        ) {
            if (totalElements <= 0) {
                Debug.LogError($"{nameof(_CreateExponentialBuckets)}: Total elements must be > 0");
                return null;
            }

            if (bucketCount <= 0) {
                #region TLP_DEBUG
#if TLP_DEBUG
          Debug.LogError("Bucket count must be > 0");
#endif
                #endregion

                return null;
            }

            if (growthFactor <= 1.0) {
                #region TLP_DEBUG
#if TLP_DEBUG
            Debug.LogError("Growth factor must be > 1");
#endif
                #endregion

                return null;
            }

            // Create exponential weights
            float[] weights = new float[bucketCount];

            for (int i = 0; i < bucketCount; i++) {
                int exponent = smallestFirst
                        ? i
                        : bucketCount - 1 - i;

                weights[i] = Mathf.Pow(growthFactor, exponent);
            }

            float totalWeight = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (float weight in weights) {
                totalWeight += weight;
            }

            // Compute bucket sizes
            int[] buckets = new int[bucketCount];
            int allocated = 0;

            for (int i = 0; i < bucketCount; i++) {
                buckets[i] = Mathf.Max(1,Mathf.FloorToInt(
                        totalElements * weights[i] / totalWeight));

                allocated += buckets[i];
            }

            // Distribute leftover elements
            int remainder = totalElements - allocated;

            for (int i = 0; i < remainder; i++) {
                buckets[i % bucketCount]++;
            }

            return buckets;
        }
        
        public static int _UpdatePartitionCursorWithLooping(int partitionIndex, int partitionCursor, int partitionSize) {
            return partitionIndex + (partitionCursor - partitionIndex)._AddLooping(partitionSize);
        }
    }
}