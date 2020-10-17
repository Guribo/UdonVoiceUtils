#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Scripts
{
    [RequireComponent(typeof(UdonBehaviour))]
    [RequireComponent(typeof(AudioSource))]
    public class BetterAudioValidator : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.IsPlaying(gameObject))
            {
                Destroy(this);
            }
        }


        [Tooltip(
            "Save the scene while having this gameobject selected to update the graph, the displayed length is equivalent to 1000 meters")]
        [SerializeField]
        private AnimationCurve noisePreview = AnimationCurve.EaseInOut(0, 0, 1, 0);

        private void OnValidate()
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource.enabled)
            {
                audioSource.enabled = false;
            }

            if (audioSource.playOnAwake)
            {
                audioSource.playOnAwake = false;
                Debug.LogError("Please set PlayOnEnable in the udon behaviour instead", this);
            }

            if (audioSource.outputAudioMixerGroup)
            {
                Debug.LogError(
                    "outputAudioMixerGroup not yet supported by Udon, set it globally in the pool prefab instead",
                    this);
                audioSource.outputAudioMixerGroup = null;
            }

            if (audioSource.bypassEffects)
            {
                Debug.LogError("bypassEffects not yet supported by Udon, set it globally in the pool prefab instead",
                    this);
                audioSource.bypassEffects = false;
            }

            if (audioSource.bypassListenerEffects)
            {
                Debug.LogError(
                    "bypassListenerEffects not yet supported by Udon, set it globally in the pool prefab instead",
                    this);
                audioSource.bypassListenerEffects = false;
            }

            if (audioSource.mute)
            {
                Debug.LogWarning("audio source is muted", this);
            }

            var udonBehaviour = GetComponent<UdonBehaviour>();
            udonBehaviour.OnValidate();
            udonBehaviour.SendCustomEvent("OnValidate");

            if (Selection.activeObject == gameObject)
            {
                GenerateNoisePreview(udonBehaviour);
            }
        }

        public class MyAssetModificationProcessor : UnityEditor.AssetModificationProcessor
        {
            public static string[] OnWillSaveAssets(string[] paths)
            {
                foreach (var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (var betterAudioValidator in rootGameObject.GetComponentsInChildren<BetterAudioValidator>(
                        true))
                    {
                        betterAudioValidator.OnValidate();
                    }
                }

                return paths;
            }
        }

        private void GenerateNoisePreview(UdonBehaviour udonBehaviour)
        {
            if (!udonBehaviour.isActiveAndEnabled)
            {
                Debug.LogWarning("Can not generate noise preview on disabled udon behaviours/game objects", this);
                return;
            }

            var distanceNoise = GetFloat(udonBehaviour, "distanceNoise");
            var positionScale = GetFloat(udonBehaviour, "noiseScale");
            var octaves = GetInteger(udonBehaviour, "noiseLayers");
            var persistence = GetFloat(udonBehaviour, "noiseAmplitudeWeight");
            var lacunarity = GetFloat(udonBehaviour, "noiseFrequencyIncrease");
            var changeRate = GetFloat(udonBehaviour, "noiseChangeRate");

            noisePreview = AnimationCurve.Linear(0, 0, 0, 0);

            for (int i = 0; i < 1000; i++)
            {
                float time = i * 1f;
                float value = NoiseHeight(time,
                                  0,
                                  positionScale,
                                  octaves,
                                  persistence,
                                  lacunarity,
                                  changeRate)
                              * distanceNoise;
                noisePreview.AddKey(time, value);
            }
        }

        private float GetFloat(UdonBehaviour udonBehaviour, string name)
        {
            if (!udonBehaviour.publicVariables.TryGetVariableValue(name, out var obj))
            {
                Debug.LogError(name, this);
                return 0;
            }

            return (float) obj;
        }

        private int GetInteger(UdonBehaviour udonBehaviour, string name)
        {
            if (!udonBehaviour.publicVariables.TryGetVariableValue(name, out var obj))
            {
                Debug.LogError(name, this);
                return 0;
            }

            return (int) obj;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="positionScale"></param>
        /// <param name="octaves"> >= 1</param>
        /// <param name="persistence"></param>
        /// <param name="lacunarity"> >= 1</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="changeRate"></param>
        /// <returns></returns>
        private float NoiseHeight(
            float x,
            float y,
            float positionScale = 0.01f,
            int octaves = 3,
            float persistence = 0.33f,
            float lacunarity = 2f,
            float changeRate = 0.1f)
        {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            var xPos = x * positionScale;
            var yPos = y * positionScale;

            var offset = 0;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = xPos / frequency + offset;
                float sampleY = yPos / frequency + offset;

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return noiseHeight;
        }
    }
}
#endif