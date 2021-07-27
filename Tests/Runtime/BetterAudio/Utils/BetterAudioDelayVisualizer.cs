using Guribo.UdonBetterAudio.Runtime;
using UdonSharp;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Tests.Runtime.BetterAudio.Utils
{
    public class BetterAudioDelayVisualizer : UdonSharpBehaviour
    {
        [SerializeField] private BetterAudioSource audioSource;

        [Tooltip("")] [SerializeField] private Transform visualizationObject;
        private float _radius;

        protected void Start()
        {
            OnEnable();
        }

        protected void OnEnable()
        {
            if (!audioSource)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804545>Visualization</color>] Invalid better audio source",
                    this);
                gameObject.SetActive(false);
                return;
            }

            if (!visualizationObject)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804545>Visualization</color>] Invalid visualizationObject",
                    this);
                gameObject.SetActive(false);
                return;
            }

            var visualizationRenderer = visualizationObject.GetComponent<Renderer>();
            if (!visualizationRenderer)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804545>Visualization</color>] VisualizationObject must have a renderer component",
                    this);
                gameObject.SetActive(false);
                return;
            }

            if (visualizationRenderer.bounds.center.sqrMagnitude > 0.001f)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804545>Visualization</color>] VisualizationObject must be centered at 0,0,0",
                    this);
                gameObject.SetActive(false);
                return;
            }

            var boundsExtents = visualizationRenderer.bounds.extents;
            if (Mathf.Min(boundsExtents.x, boundsExtents.y, boundsExtents.z) - boundsExtents.magnitude > 0.001f)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804545>Visualization</color>] VisualizationObject must be symmetrical on all 3 axis",
                    this);
                gameObject.SetActive(false);
                return;
            }

            // any of the 3 extents values works for radius as symmetry is ensured here
            _radius = boundsExtents.x;

            if (_radius < 0.01f)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804545>Visualization</color>] VisualizationObject is too small, preferably use an object with a radius of 1m",
                    this);
                gameObject.SetActive(false);
            }
        }
        private const float SpeedOfSoundSeaLevel = 343f;
        private void FixedUpdate()
        {
            if (!audioSource) return;


            // var actualAudioSource = audioSource.GetActualAudioSource();
            // if (!actualAudioSource) return;
            // if (actualAudioSource.isPlaying) return;

            if (audioSource.IsPendingSonicBoom())
            {
                visualizationObject.gameObject.SetActive(true);
                visualizationObject.position = audioSource.GetSonicBoomAudioPosition();
                var delay = audioSource.GetSonicBoomDelay();
                var scale = 1f / _radius * delay * SpeedOfSoundSeaLevel;
                visualizationObject.localScale = new Vector3(scale, scale, scale);
            }
            else
            {
                visualizationObject.gameObject.SetActive(false);
            }
        }
    }
}