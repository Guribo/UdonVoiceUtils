using TLP.UdonUtils.Runtime.Common;
using TLP.UdonVoiceUtils.Runtime.Core.WorldAudio;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio
{
    public static class VoiceUtils
    {
        /// <summary>
        /// Searches the scene for the GameObject 'TLP_PlayerAudioController' and its PlayerAudioController
        /// </summary>
        /// <returns>The first found PlayerAudioController in the scene or null if not found</returns>
        public static PlayerAudioController _FindPlayerAudioController() {
            var found = GameObject.Find(PlayerAudioController._ExpectedGameObjectName());
            if (!Utilities.IsValid(found)) {
                Debug.LogError($"GameObject '{PlayerAudioController._ExpectedGameObjectName()}' does not exist in the scene");
                return null;
            }

            var controller = found.GetComponent<PlayerAudioController>();
            if (Utilities.IsValid(controller)) {
                return controller;
            }

            Debug.LogError(
                    $"GameObject '{found.transform._GetPathInScene()}' has no valid {nameof(PlayerAudioController)} component");
            return null;
        }
        
        /// <summary>
        /// Searches the scene for the GameObject 'TLP_WorldAudioController' and its WorldAudioController
        /// </summary>
        /// <returns>The first found WorldAudioController in the scene or null if not found</returns>
        public static WorldAudioController _FindWorldAudioController() {
            var found = GameObject.Find(WorldAudioController._ExpectedGameObjectName());
            if (!Utilities.IsValid(found)) {
                Debug.LogError($"GameObject '{WorldAudioController._ExpectedGameObjectName()}' does not exist in the scene");
                return null;
            }

            var controller = found.GetComponent<WorldAudioController>();
            if (Utilities.IsValid(controller)) {
                return controller;
            }

            Debug.LogError(
                    $"GameObject '{found.transform._GetPathInScene()}' has no valid {nameof(WorldAudioController)} component");
            return null;
        }

        /// <summary>Linear falloff from min to max distance, clamped to 0-1.</summary>
        public static float _LinearFalloff(float min, float max, float distance) {
            if (distance >= max) return 0;
            if (distance <= min) return 1;
            float divisor = max - min;
            return divisor > 0 ? Mathf.Clamp01(1f - (distance - min) / divisor) : 1f;
        }

        /// <summary>Logarithmic falloff from min distance outward.</summary>
        public static float _LogarithmicFalloff(float min, float distance) {
            if (min <= 0) return 0;
            if (distance <= min) return 1;
            return min / distance;
        }

        /// <summary>
        /// Calculates directionality multiplier based on head rotation of listener and speaker.
        /// </summary>
        /// <param name="listenerHeadRotation">Listener's head rotation</param>
        /// <param name="playerHeadRotation">Speaker's head rotation</param>
        /// <param name="directionToPlayer">Direction from listener to speaker (normalized)</param>
        /// <param name="listenerDirectionality">How much listener facing direction affects range (0-1)</param>
        /// <param name="playerDirectionality">How much speaker facing direction affects range (0-1)</param>
        /// <returns>Range multiplier between 0 and 1</returns>
        public static float _CalculateDirectionality(
                Quaternion listenerHeadRotation,
                Quaternion playerHeadRotation,
                Vector3 directionToPlayer,
                float listenerDirectionality,
                float playerDirectionality
        ) {
            var listenerForward = listenerHeadRotation * Vector3.forward;
            var playerBackward = playerHeadRotation * Vector3.back;

            float dotListener = 0.5f * (Vector3.Dot(listenerForward, directionToPlayer) + 1f);
            float dotSource = 0.5f * (Vector3.Dot(playerBackward, directionToPlayer) + 1f);

            return Mathf.Clamp01(dotListener + (1f - listenerDirectionality)) *
                   Mathf.Clamp01(dotSource + (1f - playerDirectionality));
        }

        public static float _CalculateImportance(
                Vector3 emitterPosition,
                Vector3 listenerHeadPosition,
                AudioSource audioSource,
                bool isReceivingStart,
                bool hasStartedOk,
                float unimportanceOffset,
                float priorityWeight
        ) {
            float distance = Vector3.Distance(emitterPosition, listenerHeadPosition);
            if (isReceivingStart || !hasStartedOk) {
                return unimportanceOffset + distance;
            }

            float volume = _CalculateAudioVolume(audioSource, distance);
            if (volume == 0) {
                return unimportanceOffset + distance;
            }

            return priorityWeight * audioSource.priority + (1f - volume);
        }

        private static float _CalculateAudioVolume(AudioSource audioSource, float distance) {
            float volume;
            switch (audioSource.rolloffMode) {
                case AudioRolloffMode.Logarithmic:
                    volume = audioSource.volume * _LogarithmicFalloff(audioSource.minDistance, distance);
                    break;
                case AudioRolloffMode.Linear:
                    volume = audioSource.volume * _LinearFalloff(
                            audioSource.minDistance,
                            audioSource.maxDistance,
                            distance);
                    break;
                case AudioRolloffMode.Custom:
                    if (audioSource.maxDistance > 0) {
                        volume = audioSource.volume * audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff)
                                .Evaluate(distance / audioSource.maxDistance);
                    } else {
                        volume = 0;
                    }

                    break;
                default:
                    volume = 0f;
                    break;
            }

            return volume;
        }
    }
}
