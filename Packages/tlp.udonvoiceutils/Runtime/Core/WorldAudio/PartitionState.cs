using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
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
    [CustomEditor(typeof(PartitionState))]
    public class PartitionStateEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Holds partition configuration (count, growth factor) and initializes " +
                   "the PartitionCursor. Partitions divide emitters into priority buckets " +
                   "so distant/low-priority sources are updated less frequently.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PartitionState), ExecutionOrder)]
    public class PartitionState : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PartitionCursor.ExecutionOrder + 1;
        #endregion

        #region Configuration
        [Tooltip(
                "Number priority partitions. Each partition handles a range of priorities. Earlier partitions handle high priority AudioSources and later iterations handle lower priority AudioSources. Typical: 3-6")]
        [Range(1, 10)]
        [SerializeField]
        internal int Partitions = 3;

        [Tooltip(
                "Growth factor for exponential partition bucketing. Each partition is growthFactor times larger than the previous. Default 4 creates bin sizes that distribute emitters 1:4:16:... to prioritize distant-source culling. Range: 2–6 (lower = more uniform; higher = more skewed).")]
        [SerializeField]
        internal float PartitionGrowthFactor = 4;
        #endregion

        #region Dependencies
        [Header("Dependencies")]
        [SerializeField]
        internal PartitionCursor PartitionCursor;
        #endregion

        public int ActivePartitions { get; private set; }


        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) return false;

            if (!_IsSet(PartitionCursor, nameof(PartitionCursor))) {
                return false;
            }

            return true;
        }
        #endregion

        #region Public API
        public void _InitializePartitionManagement(int totalElements) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_InitializePartitionManagement)}: {nameof(totalElements)}={totalElements}");
#endif
            #endregion

            if (!HasStartedOk) return;

            ActivePartitions = Mathf.Min(Partitions, totalElements);
            PartitionCursor._InitializePartitionSizes(totalElements, ActivePartitions, PartitionGrowthFactor);
            PartitionCursor._InitializePartitionIndices();
            PartitionCursor._InitializePartitionCursors();
        }
        #endregion
    }
}