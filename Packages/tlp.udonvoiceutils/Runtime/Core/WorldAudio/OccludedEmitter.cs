using JetBrains.Annotations;
using TLP.UdonAVLTree.Runtime;
using TLP.UdonUtils.Runtime;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Core.WorldAudio;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Core.WorldAudio
{
    [CustomEditor(typeof(OccludedEmitter))]
    public class OccludedEmitterEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Per-emitter audio lifecycle controller. Manages importance calculation, " +
                   "reverb/lowpass updates, GameObject activation/deactivation for culled sources, " +
                   "and privacy channel gating. One per audible audio source in the scene.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    /// <summary>
    /// Manages audio source lifecycle for the occlusion system.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(ReverbController))]
    [RequireComponent(typeof(AudioSourceStateController))]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(OccludedEmitter), ExecutionOrder)]
    public class OccludedEmitter : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = AudioSourceStateController.ExecutionOrder + 1;
        #endregion

        #region Constants
        // Must be > max possible active result (2 * 255 + 1 = 511) to push uninitialized/silent emitters below all active ones
        private const float UnimportanceOffset = 512;

        // Ensures priority dominates the sort: even max volume at priority N sorts before min volume at priority N+1
        private const float PriorityWeight = 2;
        #endregion

        #region Configuration
        [Header("Configuration")]
        [Tooltip(
                "When enabled, deactivates the GameObject while the AudioSource is culled. " +
                "Disable to keep the object active even when its audio is culled. " +
                "IMPORTANT: DO NOT disable gameobject if this is a source used by a Unity videoplayer -> set this option to false! " +
                "Disabling the gameobject for video player controlled sources can break audio filters until a new video is started.")]
        [SerializeField]
        private bool DisableGameObjectWhenCulled = true;

        [Tooltip(
                "Privacy channel this emitter belongs to. Players whose override shares this channel can hear it. Set to -1 for no privacy (always audible).")]
        [SerializeField]
        internal int PrivacyChannelId = PlayerAudioController.ChannelNoPrivacy;
        #endregion

        #region Dependencies
        [Header("Dependencies")]

        // Serialized field — not wrapped in #if TLP_DEBUG so reference persists when toggling debug mode
        [Tooltip("Optional, can be left empty when not debugging.")]
        [SerializeField]
        internal Renderer DebugRenderer;
        #endregion

        #region State
        internal EmitterRegistry _emitterRegistry;
        internal AudioSource _audioSource;
        private ReverbController _reverbController;
        private AudioSourceStateController _audioStateController;
        private bool _privacyCulled;
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            var worldAudioController = VoiceUtils.FindWorldAudioController();
            if (!Utilities.IsValid(worldAudioController)) {
                return false;
            }

            _emitterRegistry = worldAudioController.EmitterRegistry;
            if (!IsSet(_emitterRegistry, nameof(_emitterRegistry))) {
                return false;
            }

            _audioSource = gameObject.GetComponent<AudioSource>();
            if (!IsSet(_audioSource, nameof(_audioSource))) {
                return false;
            }

            _reverbController = gameObject.GetComponent<ReverbController>();
            if (!IsSet(_reverbController, nameof(_reverbController))) {
                return false;
            }

            _audioStateController = gameObject.GetComponent<AudioSourceStateController>();
            if (!IsSet(_audioStateController, nameof(_audioStateController))) {
                return false;
            }

#if TLP_DEBUG
            if (!IsSet(DebugRenderer, nameof(DebugRenderer))) {
                return false;
            }

            InitializeDebugVisuals();
#endif

            _emitterRegistry.Add(this);
            return true;
        }
        #endregion

        #region Cleanup
        private void OnDestroy() {
            if (!Utilities.IsValid(_emitterRegistry)) {
                return;
            }

            _emitterRegistry.Remove(this);
        }
        #endregion

        #region Public API
        public float CachedImportance { get; private set; }

        public void RefreshInTree(AvlTree avlTree, int maxAudibleSources) {
            if (!HasStartedOk) {
                return;
            }

            float emitterCachedImportance = CalculateImportance();
            avlTree.Remove(this);

            if (_privacyCulled) {
                // ensure privacy culled emitters are always below non-privacy culled emitters in the tree,
                // even if they have higher importance
                var entry = (OccludedEmitter)avlTree.Get(maxAudibleSources);
                if (Utilities.IsValid(entry)) {
                    emitterCachedImportance = Mathf.Max(emitterCachedImportance, entry.CachedImportance);
                }
            }

            CachedImportance = emitterCachedImportance;
            avlTree.Add(this);
        }

        /// <summary>
        /// Returns true if this emitter is audible, false if it is not audible due to being below the lowest priority active emitter.
        /// </summary>
        /// <param name="avlTree"></param>
        /// <param name="maxAudibleSources"></param>
        /// <param name="lowestPrioActiveEmitter">the lowest priority (yet still audible) emitter</param>
        /// <returns></returns>
        public bool IsAudible(AvlTree avlTree, int maxAudibleSources, out OccludedEmitter lowestPrioActiveEmitter) {
            if (!HasStartedOk || _privacyCulled) {
                lowestPrioActiveEmitter = null;
                return false;
            }

            lowestPrioActiveEmitter = (OccludedEmitter)avlTree.Get(maxAudibleSources);
            if (!Utilities.IsValid(lowestPrioActiveEmitter)) {
                return true;
            }

            return lowestPrioActiveEmitter.CachedImportance > CachedImportance;
        }

        public float CalculateImportance() {
            var trackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            return VoiceUtils.CalculateImportance(
                    transform.position,
                    trackingData.position,
                    _audioSource,
                    IsReceivingStart,
                    HasStartedOk,
                    UnimportanceOffset,
                    PriorityWeight
            );
        }

        /// <param name="clarity">0-1, when at 1: audio source is least affected by filters</param>
        public void UpdateAudioSourceState(
                float partition,
                bool cull,
                bool privacyCulled,
                float clarity
        ) {
            if (cull) {
                _audioStateController.Cull();

                // This is an additional layer that applies for AudioSources controlled by Unity videoplayers as
                // regular culling doesn't seem to have any effect.
                // Additionally, audio sources controlled by Unity videoplayer must not be disabled when culled as
                // it breaks audio filters when no longer culled afterwards.
                // Videos would need to be restarted to fix that.
                _reverbController.UpdateReverb(0, 0);
#if TLP_DEBUG
                UpdateDebugVisuals(1);
#endif
                if (DisableGameObjectWhenCulled) {
                    gameObject.SetActive(false);
                }

                _privacyCulled = privacyCulled;
                return;
            }

            _privacyCulled = false;

#if TLP_DEBUG
            UpdateDebugVisuals(partition);
#endif

            if (DisableGameObjectWhenCulled && !gameObject.activeSelf) {
                gameObject.SetActive(true);
            }

            _audioStateController.Reactivate();
            _reverbController.UpdateReverb(CalculateHeadDistance(), clarity);
        }
        #endregion

        #region Internal
        private float CalculateHeadDistance() {
            var trackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var headPosition = trackingData.position;
            float distance = Vector3.Distance(transform.position, headPosition);
            return distance;
        }

#if TLP_DEBUG
        private int _sColor;
        private MaterialPropertyBlock _propertyBlock;
        private float _lastPartition;

        private void InitializeDebugVisuals() {
            _propertyBlock = new MaterialPropertyBlock();
            _sColor = VRCShader.PropertyToID("_Color");
        }

        private void UpdateDebugVisuals(float partition) {
            if (Mathf.Approximately(_lastPartition, partition)) {
                return;
            }

            _lastPartition = partition;
            _propertyBlock.SetColor(_sColor, Color.Lerp(Color.green, Color.black, partition));
            DebugRenderer.SetPropertyBlock(_propertyBlock);
        }
#endif
        #endregion
    }
}