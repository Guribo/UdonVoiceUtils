using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Core.WorldAudio;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Core.WorldAudio
{
    [CustomEditor(typeof(PartitionCursor))]
    public class PartitionCursorEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Manages partition iteration state: tracks cursor position per partition, " +
                   "cycles through outdated partitions, and enforces per-frame update budgets. " +
                   "Drives the round-robin emitter update loop in WorldAudioController.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PartitionCursor), ExecutionOrder)]
    public class PartitionCursor : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = EmitterRegistry.ExecutionOrder + 1;
        #endregion
        
        #region State
        [SerializeField]
        internal int[] PartitionSizes;

        [SerializeField]
        internal int[] PartitionIndices;

        [SerializeField]
        internal int[] PartitionCursors;

        [SerializeField]
        internal int[] PartitionUpdatesInFrame;

        private int _partitions;
        private int _partitionToUpdate;
        #endregion
        
        #region Public API
        public float GetPartitionAsRatio() {
            if (_partitions == 0) return 0;
            return (float)_partitionToUpdate / _partitions;
        }

        public void ResetPartitionUpdateCounters() {
            for (int i = 0; i < _partitions; i++) {
                PartitionUpdatesInFrame[i] = 0;
            }
        }
        
        public bool FindNextOutdatedPartition(out int index, out int partitionToUpdate) {
            _partitionToUpdate.MoveIndexRightLooping(_partitions);
            int partitionsChecked = 0;
            while (PartitionUpdatesInFrame[_partitionToUpdate] >= PartitionSizes[_partitionToUpdate]) {
                if (partitionsChecked >= _partitions) {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    Warn("Infinite loop detected in UpdateEmitters, partition index: " + _partitionToUpdate);
#endif
                    #endregion

                    index = 0;
                    partitionToUpdate = 0;
                    return false;
                }

                _partitionToUpdate.MoveIndexRightLooping(_partitions);
                partitionsChecked++;
            }

            PartitionUpdatesInFrame[_partitionToUpdate]++;
            index = ArrayUtils.UpdatePartitionCursorWithLooping(
                    PartitionIndices[_partitionToUpdate],
                    PartitionCursors[_partitionToUpdate],
                    PartitionSizes[_partitionToUpdate]);
            PartitionCursors[_partitionToUpdate] = index;
            partitionToUpdate = _partitionToUpdate;
            return true;
        }
        #endregion

        #region Internal
        internal void InitializePartitionSizes(int totalElements, int partitions, float partitionGrowthFactor) {
            _partitions = partitions;
            PartitionSizes = ArrayUtils.CreateExponentialBuckets(
                    totalElements,
                    _partitions,
                    partitionGrowthFactor);
            PartitionSizes = PartitionSizes.ResizeOrCreate(_partitions); // ensures is not null
        }

        internal void InitializePartitionCursors() {
            PartitionCursors = new int[_partitions];
            for (int i = 0; i < _partitions; i++) {
                PartitionCursors[i] = PartitionIndices[i];
            }
        }

        internal void InitializePartitionIndices() {
            PartitionIndices = new int[_partitions];
            PartitionUpdatesInFrame = new int[_partitions];
            int index = 0;
            for (int i = 0; i < _partitions; i++) {
                PartitionIndices[i] = index;
                index += PartitionSizes[i];
            }
        }
        #endregion
    }
}