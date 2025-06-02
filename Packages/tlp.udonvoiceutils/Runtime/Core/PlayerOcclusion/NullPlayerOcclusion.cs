using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerOcclusion
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(NullPlayerOcclusion), ExecutionOrder)]
    public class NullPlayerOcclusion : PlayerOcclusionStrategy
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerOcclusionStrategy.ExecutionOrder + 1;

        public override float GetRemainingAudioRange(
                Vector3 listenerHead,
                Vector3 listenerLookDirection,
                float distanceBetweenPlayerHeads,
                float occlusionFactor,
                float playerOcclusionFactor,
                int playerOcclusionMask
        ) {
            return 1;
        }
    }
}