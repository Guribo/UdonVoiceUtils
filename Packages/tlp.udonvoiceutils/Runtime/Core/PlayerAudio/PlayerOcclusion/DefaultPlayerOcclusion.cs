using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Logger;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio.PlayerOcclusion
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DefaultPlayerOcclusion), ExecutionOrder)]
    public class DefaultPlayerOcclusion : PlayerOcclusionStrategy
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = NullPlayerOcclusion.ExecutionOrder + 1;
        #endregion

        #region Constants
        private const float MaxClarity = 1f;
        private const float MinDistanceForOcclusion = 1f;
        private const int RayBufferSize = 2;
        #endregion


        #region State
        // TODO what about all hits between players?
        //  Thinking: it is not guaranteed that the first two hits are the closest two ones
        private readonly RaycastHit[] _rayHits = new RaycastHit[RayBufferSize];
        #endregion

        /// <param name="listenerHead"></param>
        /// <param name="listenerLookDirection"></param>
        /// <param name="distanceBetweenPlayerHeads"></param>
        /// <param name="environmentClarity"></param>
        /// <param name="playerClarity"></param>
        /// <param name="playerOcclusionMask"></param>
        /// <returns>multiplier in range 0 to 1</returns>
        public override float GetAudioClarity(
                Vector3 listenerHead,
                Vector3 listenerLookDirection,
                float distanceBetweenPlayerHeads,
                float environmentClarity,
                float playerClarity,
                int playerOcclusionMask
        ) {
            int hits = Physics.RaycastNonAlloc(
                    listenerHead,
                    listenerLookDirection,
                    _rayHits,
                    distanceBetweenPlayerHeads,
                    playerOcclusionMask
            );

            return DetermineClarityFromOcclusion(
                    distanceBetweenPlayerHeads,
                    environmentClarity,
                    playerClarity,
                    playerOcclusionMask,
                    hits,
                    _rayHits);
        }

        internal static float DetermineClarityFromOcclusion(
                float distanceBetweenPlayerHeads,
                float environmentClarity,
                float playerClarity,
                int playerOcclusionMask,
                int hits,
                RaycastHit[] raycastHits
        ) {
            if (IsNotOccluded(hits)) {
                return MaxClarity;
            }

            if (PlayersCanNotOccludeEachOther(playerOcclusionMask)) {
                return GetAudioObstacleClarity(raycastHits[0], environmentClarity);
            }

            if (OnePlayerHeadMaybeLeftTheirCapsule(hits)) {
                return HandleSingleHitOcclusionWithPlayers(
                        distanceBetweenPlayerHeads,
                        environmentClarity,
                        playerClarity,
                        raycastHits[0]);
            }

            return HandleMultiHitOcclusionWithPlayers(
                    environmentClarity,
                    playerClarity,
                    raycastHits[0],
                    raycastHits[1]);
        }

        /// <summary>
        /// more than 1 hit indicates the ray hit another player first or hit the environment
        /// _rayHits contains 2 valid hits now (not ordered by distance!!!
        /// see https://docs.unity3d.com/ScriptReference/Physics.RaycastNonAlloc.html)
        /// </summary>
        /// <param name="environmentClarity"></param>
        /// <param name="playerClarity"></param>
        /// <param name="firstHit"></param>
        /// <param name="secondHit"></param>
        /// <returns></returns>
        private static float HandleMultiHitOcclusionWithPlayers(
                float environmentClarity,
                float playerClarity,
                RaycastHit firstHit,
                RaycastHit secondHit
        ) {
            if (IsOnlyPlayerHits(firstHit, secondHit)) {
                // another player occluded the emitting player we ray casted to
                return playerClarity;
            }

            // we hit one non-player obstacle
            return GetAudioObstacleClarity(FindEnvironmentHit(firstHit, secondHit), environmentClarity);
        }

        private static bool IsOnlyPlayerHits(RaycastHit firstHit, RaycastHit secondHit) {
            if (IsOccludedByPlayer(firstHit) && IsOccludedByPlayer(secondHit)) {
#if TLP_DEBUG
                TlpLogger.StaticDebugLog($"{nameof(DefaultPlayerOcclusion)}: only players occlude", null);
#endif

                return true;
            }

#if TLP_DEBUG
            TlpLogger.StaticDebugLog($"{nameof(DefaultPlayerOcclusion)}: environment hit", null);
#endif

            return false;
        }

        private static float HandleSingleHitOcclusionWithPlayers(
                float distanceBetweenPlayerHeads,
                float environmentClarity,
                float playerClarity,
                RaycastHit firstHit
        ) {
            // check how far away the hit is from the player and if it is above a certain threshold
            // assume an object occludes the player (threshold is 1m for now)
            // TODO find a solution that also works for taller avatars for which the radius of the capsule can exceed 1m
            if (firstHit.distance > distanceBetweenPlayerHeads - MinDistanceForOcclusion) {
                return MaxClarity;
            }

            if (IsOccludedByPlayer(firstHit)) {
                return playerClarity;
            }
            return GetAudioObstacleClarity(firstHit, environmentClarity);
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
        private static bool PlayersCanNotOccludeEachOther(int playerOcclusionMask) {
            return (playerOcclusionMask & PlayerAudioConfigurationModel.UILayerMask) == 0;
        }

        private static bool HasEnvironmentHit(RaycastHit hit) {
            return Utilities.IsValid(hit.transform);
        }

        private static float GetAudioObstacleClarity(RaycastHit hit, float fallbackClarity) {
            if (!Utilities.IsValid(hit.transform)) {
                return fallbackClarity;
            }
            AudioObstacle obstacle = hit.transform.GetComponent<AudioObstacle>();
            if (Utilities.IsValid(obstacle)) {
                return obstacle.ObstacleClarity;
            }
            return fallbackClarity;
        }

        private static RaycastHit FindEnvironmentHit(RaycastHit firstHit, RaycastHit secondHit) {
            if (HasEnvironmentHit(firstHit)) {
                return firstHit;
            }
            return secondHit;
        }
    }
}