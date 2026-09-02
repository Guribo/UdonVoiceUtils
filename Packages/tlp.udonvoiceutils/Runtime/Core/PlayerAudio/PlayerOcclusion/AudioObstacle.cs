using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UdonSharp;
using UnityEngine;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio.PlayerOcclusion;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Core.PlayerAudio.PlayerOcclusion
{
    [CustomEditor(typeof(AudioObstacle))]
    public class AudioObstacleEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Attach to environment colliders to define per-obstacle occlusion clarity. " +
                   "When a raycast hits this collider, the configured clarity value is used " +
                   "instead of the global environment clarity.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio.PlayerOcclusion
{
    /// <summary>
    /// Attach to environment colliders to define per-obstacle occlusion properties.
    /// When a raycast hits this collider, <see cref="ObstacleClarity"/> is used
    /// instead of the global environment clarity.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(AudioObstacle), ExecutionOrder)]
    public class AudioObstacle : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = DefaultPlayerOcclusion.ExecutionOrder + 1;
        #endregion

        #region Configuration
        [Header("Configuration")]
        [Tooltip(
                "How clearly sound passes through this obstacle. " +
                "0 = completely blocks sound, 1 = fully transparent (no effect on clarity).")]
        [SerializeField]
        internal float ObstacleClarity = 1f;
        #endregion
    }
}
