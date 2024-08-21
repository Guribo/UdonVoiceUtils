using TLP.UdonUtils.Runtime;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerOcclusion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public abstract class PlayerOcclusionStrategy : TlpBaseBehaviour
    {
        /// <param name="listenerHead"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <param name="occlusionFactor"></param>
        /// <param name="playerOcclusionFactor"></param>
        /// <param name="playerOcclusionMask"></param>
        /// <returns>multiplier in range 0 to 1</returns>
        public abstract float CalculateOcclusion(
                Vector3 listenerHead,
                Vector3 direction,
                float distance,
                float occlusionFactor,
                float playerOcclusionFactor,
                int playerOcclusionMask
        );
    }
}