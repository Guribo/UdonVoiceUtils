using JetBrains.Annotations;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerOcclusion
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DefaultPlayerOcclusion), ExecutionOrder)]
    public class DefaultPlayerOcclusion : PlayerOcclusionStrategy
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = NullPlayerOcclusion.ExecutionOrder + 1;

        private const float NoDistanceReduction = 1f;
        private const float MinDistanceForOcclusion = 1f;

        #region State
        // TODO what about all hits between players?
        //  Thinking: it is not guaranteed that the first two hits are the closest two ones
        private readonly RaycastHit[] _rayHits = new RaycastHit[2];
        #endregion

        /// <param name="listenerHead"></param>
        /// <param name="listenerLookDirection"></param>
        /// <param name="distanceBetweenPlayerHeads"></param>
        /// <param name="occlusionFactor"></param>
        /// <param name="playerOcclusionFactor"></param>
        /// <param name="playerOcclusionMask"></param>
        /// <returns>multiplier in range 0 to 1</returns>
        public override float GetRemainingAudioRange(
                Vector3 listenerHead,
                Vector3 listenerLookDirection,
                float distanceBetweenPlayerHeads,
                float occlusionFactor,
                float playerOcclusionFactor,
                int playerOcclusionMask
        ) {
            int hits = Physics.RaycastNonAlloc(
                    listenerHead,
                    listenerLookDirection,
                    _rayHits,
                    distanceBetweenPlayerHeads,
                    playerOcclusionMask
            );

            if (IsNotOccluded(hits)) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog("No hits");
#endif
                #endregion

                return NoDistanceReduction;
            }

            if (PlayersCanOccludeEachOther(playerOcclusionMask)) {
                return OnePlayerHeadMaybeLeftTheirCapsule(hits)
                        ? HandleSingleHitOcclusionWithPlayers(
                                distanceBetweenPlayerHeads,
                                occlusionFactor,
                                playerOcclusionFactor)
                        : HandleMultiHitOcclusionWithPlayers(occlusionFactor, playerOcclusionFactor);
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(DefaultPlayerOcclusion)}: Non-player occlusion: {hits}");
#endif
            #endregion

            return occlusionFactor;
        }

        /// <summary>
        /// more than 1 hit indicates the ray hit another player first or hit the environment
        /// _rayHits contains 2 valid hits now (not ordered by distance!!!
        /// see https://docs.unity3d.com/ScriptReference/Physics.RaycastNonAlloc.html)
        /// </summary>
        /// <param name="occlusionFactor"></param>
        /// <param name="playerOcclusionFactor"></param>
        /// <returns></returns>
        private float HandleMultiHitOcclusionWithPlayers(float occlusionFactor, float playerOcclusionFactor) {
            return IsOnlyPlayerHits() ?
                    // another player occluded the emitting player we ray casted to
                    playerOcclusionFactor :
                    // we hit one non-player obstacle
                    occlusionFactor;
        }

        private bool IsOnlyPlayerHits() {
            if (IsOccludedByPlayer(_rayHits[0]) && IsOccludedByPlayer(_rayHits[1])) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"{nameof(DefaultPlayerOcclusion)}: only players occlude");
#endif
                #endregion

                return true;
            }
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(DefaultPlayerOcclusion)}: environment hit");
#endif
            #endregion

            return false;
        }

        private float HandleSingleHitOcclusionWithPlayers(
                float distanceBetweenPlayerHeads,
                float occlusionFactor,
                float playerOcclusionFactor
        ) {
            // check how far away the hit is from the player and if it is above a certain threshold
            // assume an object occludes the player (threshold is 1m for now)
            // TODO find a solution that also works for taller avatars for which the radius of the capsule can exceed 1m
            if (_rayHits[0].distance > distanceBetweenPlayerHeads - MinDistanceForOcclusion) {
                return NoDistanceReduction;
            }

            return IsOccludedByPlayer(_rayHits[0]) ? playerOcclusionFactor : occlusionFactor;
        }

        /// <summary>
        /// if the transform of the hit is null (due to filtering of player objects by UDON), it means a player was hit
        /// </summary>
        /// <param name="raycastHit"></param>
        /// <returns></returns>
        private static bool IsOccludedByPlayer(RaycastHit raycastHit) {
            return !raycastHit.transform;
        }

        /// <summary>
        /// sometimes the other player's head leaves it's own UI player capsule which causes
        /// the number of hits to go down by 1
        /// or there was no environment hit while the player UI capsule was hit
        /// </summary>
        /// <param name="hits"></param>
        /// <returns></returns>
        private static bool OnePlayerHeadMaybeLeftTheirCapsule(int hits) {
            return hits == 1;
        }

        private static bool IsNotOccluded(int hits) {
            return hits == 0;
        }

        /// <summary>
        /// if the UI layer is used for occlusion (UI layer contains the player capsules) allow at least one hit
        /// </summary>
        /// <param name="playerOcclusionMask"></param>
        /// <returns></returns>
        private static bool PlayersCanOccludeEachOther(int playerOcclusionMask) {
            return (playerOcclusionMask & PlayerAudioConfigurationModel.UILayerMask) != 0;
        }
    }
}