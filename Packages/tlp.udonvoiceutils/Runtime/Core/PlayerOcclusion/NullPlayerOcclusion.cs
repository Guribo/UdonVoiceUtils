using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerOcclusion
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(NullPlayerOcclusion), ExecutionOrder)]
    public class NullPlayerOcclusion : PlayerOcclusionStrategy
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerOcclusionStrategy.ExecutionOrder + 1;

        public override float CalculateOcclusion(
                Vector3 listenerHead,
                Vector3 direction,
                float distance,
                float occlusionFactor,
                float playerOcclusionFactor,
                int playerOcclusionMask
        ) {
            return 1;
        }
    }
}