using JetBrains.Annotations;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio.PlayerOcclusion
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(NullPlayerOcclusion), ExecutionOrder)]
    public class NullPlayerOcclusion : PlayerOcclusionStrategy
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerOcclusionStrategy.ExecutionOrder + 1;

        public override float _GetAudioClarity(
                Vector3 listenerHead,
                Vector3 listenerLookDirection,
                float distanceBetweenPlayerHeads,
                float environmentClarity,
                float playerClarity,
                int playerOcclusionMask
        ) {
            return 1;
        }
    }
}