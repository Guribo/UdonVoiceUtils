using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerOcclusion
{
    public class DefaultPlayerOcclusion : PlayerOcclusionStrategy
    {
        #region State
        private readonly RaycastHit[] _rayHits = new RaycastHit[2];
        #endregion

        /// <param name="listenerHead"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <param name="occlusionFactor"></param>
        /// <param name="playerOcclusionFactor"></param>
        /// <param name="playerOcclusionMask"></param>
        /// <returns>multiplier in range 0 to 1</returns>
        public override float CalculateOcclusion(
                Vector3 listenerHead,
                Vector3 direction,
                float distance,
                float occlusionFactor,
                float playerOcclusionFactor,
                int playerOcclusionMask
        ) {
            int hits = Physics.RaycastNonAlloc(
                    listenerHead,
                    direction,
                    _rayHits,
                    distance,
                    playerOcclusionMask
            );

            if (hits == 0) {
                // nothing to do

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog("No hits");
#endif
                #endregion

                return 1f;
            }

            // if the UI layer is used for occlusion (UI layer contains the player capsules) allow at least one hit
            bool playersCanOcclude = (playerOcclusionMask | PlayerAudioConfigurationModel.UILayerMask) > 0;
            if (!playersCanOcclude) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Players can't occlude: {hits}");
#endif
                #endregion

                // result when players can't occlude other players
                return hits > 0 ? occlusionFactor : 1f;
            }

            if (hits < 2) {
                // sometimes the other player's head leaves it's own UI player capsule which causes
                // the number of hits to go down by 1
                // or there was no environment hit while the player UI capsule was hit

                // check how far away the hit is from the player and if it is above a certain threshold
                // assume an object occludes the player (threshold is 1m for now)
                // TODO find a solution that also works for taller avatars for which the radius of the capsule can exceed 1m
                float minOcclusionTriggerDistance = distance - 1f;
                bool occlusionTriggered = _rayHits[0].distance < minOcclusionTriggerDistance;
                if (!occlusionTriggered) {
                    return 1f;
                }

                // if the transform of the hit is not null (due to filtering of player objects by UDON)
                // then the environment got hit and we use regular occlusion values
                return _rayHits[0].transform ? occlusionFactor : playerOcclusionFactor;
            }

            // more than 1 hit indicates the ray hit another player first or hit the environment
            // _rayHits contains 2 valid hits now (not ordered by distance!!!
            // see https://docs.unity3d.com/ScriptReference/Physics.RaycastNonAlloc.html)

            // if in both of the hits the transform is now null (due to filtering of player objects by UDON)
            // this indicates that another player occluded the emitting player we ray casted to.
            bool anotherPlayerOccludes = !_rayHits[0].transform && !_rayHits[1].transform;
            if (anotherPlayerOccludes) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"anotherPlayerOccludes");
#endif
                #endregion

                return playerOcclusionFactor;
            }

            // just return the occlusion factor for everything else
            return occlusionFactor;
        }
    }
}