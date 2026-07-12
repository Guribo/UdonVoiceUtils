using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Core.WorldAudio;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Core.WorldAudio
{
    [CustomEditor(typeof(DebugVisualizer))]
    public class DebugVisualizerEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Visual debug overlay for the importance system. " +
                   "Instantiates transform indicators showing which emitter is being re-evaluated " +
                   "and which partition is currently updating. Only active in TLP_DEBUG builds.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DebugVisualizer), ExecutionOrder)]
    public class DebugVisualizer : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PartitionState.ExecutionOrder + 1;
        #endregion

        #region Settings
        [Header("Settings")]
        [Tooltip("Single transform marking which emitter is being re-evaluated in the importance tree")]
        [SerializeField]
        internal Transform TreeUpdateIndicator;

        [Tooltip("Template transform used to Instantiate per-partition update indicators")]
        [SerializeField]
        internal Transform PartitionUpdateIndicatorTemplate;

        [Tooltip("Vertical offset applied to indicator positions above emitters")]
        [SerializeField]
        internal float IndicatorOffsetY = 2f;
        #endregion

        #region State
        private Transform[] _partitionIndicators = new Transform[0];
        #endregion

        #region Test Accessors
        internal int PartitionIndicatorCount => _partitionIndicators.Length;
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (Utilities.IsValid(PartitionUpdateIndicatorTemplate)) {
                PartitionUpdateIndicatorTemplate.gameObject.SetActive(false);
            }

            return true;
        }
        #endregion

        #region Public API
        public void InitializePartitionIndicators(int count) {
            DestroyPartitionIndicators();
            _partitionIndicators = new Transform[count];

            if (!Utilities.IsValid(PartitionUpdateIndicatorTemplate)) {
                return;
            }

            for (int i = 0; i < count; i++) {
                var clone = Instantiate(PartitionUpdateIndicatorTemplate.gameObject);
                clone.SetActive(true);
                _partitionIndicators[i] = clone.transform;
            }
        }

        public void OnTreeUpdated(Vector3 position) {
            if (!Utilities.IsValid(TreeUpdateIndicator)) {
                return;
            }

            TreeUpdateIndicator.position = position;
        }

        public void OnPartitionUpdated(int partitionIndex, Vector3 emitterPosition) {
            if (partitionIndex < 0 || partitionIndex >= _partitionIndicators.Length) {
                return;
            }

            var indicator = _partitionIndicators[partitionIndex];
            if (!Utilities.IsValid(indicator)) {
                return;
            }

            indicator.position = emitterPosition + Vector3.up * IndicatorOffsetY;
        }
        #endregion

        #region Unity Lifecycle
        private void OnDestroy() {
            DestroyPartitionIndicators();
        }
        #endregion

        #region Internal
        private void DestroyPartitionIndicators() {
            foreach (var indicator in _partitionIndicators) {
                if (!Utilities.IsValid(indicator)) {
                    continue;
                }

                Destroy(indicator.gameObject);
            }

            _partitionIndicators = new Transform[0];
        }
        #endregion
    }
}