using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio.PlayerOcclusion
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
        /// <param name="environmentClarity">Impact of the environment blocking the sound, range 0-1 where 1 means no negative impact on clarity</param>
        /// <param name="playerClarity">Impact of another player blocking the sound, range 0-1 where 1 means no negative impact on clarity</param>
        /// <param name="playerOcclusionMask"></param>
        /// <returns>0-1, when at 1: audio source shall be least affected by filters</returns>
        public abstract float GetAudioClarity(
                Vector3 listenerHead,
                Vector3 listenerLookDirection,
                float distanceBetweenPlayerHeads,
                float environmentClarity,
                float playerClarity,
                int playerOcclusionMask
        );

        /// <summary>
        /// Computes the look direction and distance from the listener head to the emitter,
        /// then delegates to <see cref="GetAudioClarity"/>.
        /// </summary>
        public float GetEmitterAudioClarity(
                Vector3 listenerHead,
                Vector3 emitterPosition,
                float environmentClarity,
                float playerClarity,
                int playerOcclusionMask
        ) {
            Vector3 direction = emitterPosition - listenerHead;
            return GetAudioClarity(
                    listenerHead,
                    direction.normalized,
                    direction.magnitude,
                    environmentClarity,
                    playerClarity,
                    playerOcclusionMask
            );
        }
    }
}