using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerOcclusion
{
    public class NullPlayerOcclusion : PlayerOcclusionStrategy
    {
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