using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerOcclusion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerOcclusionStrategy), ExecutionOrder)]
    public abstract class PlayerOcclusionStrategy : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = IgnoredPlayers.ExecutionOrder + 1;

        /// <param name="listenerHead"></param>
        /// <param name="listenerLookDirection"></param>
        /// <param name="distanceBetweenPlayerHeads"></param>
        /// <param name="occlusionFactor"></param>
        /// <param name="playerOcclusionFactor"></param>
        /// <param name="playerOcclusionMask"></param>
        /// <returns>multiplier in range 0 to 1</returns>
        public abstract float GetRemainingAudioRange(
                Vector3 listenerHead,
                Vector3 listenerLookDirection,
                float distanceBetweenPlayerHeads,
                float occlusionFactor,
                float playerOcclusionFactor,
                int playerOcclusionMask
        );
    }
}