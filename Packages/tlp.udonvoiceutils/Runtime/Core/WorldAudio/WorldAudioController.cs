using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TLP.UdonAVLTree.Runtime;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Core.WorldAudio;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Core.WorldAudio
{
    [CustomEditor(typeof(WorldAudioController))]
    public class WorldAudioControllerEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Core audio controller managing player voice emitters, " +
                   "importance sorting, and per-frame audio updates. " +
                   "Only one allowed per scene/world.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
#if TLP_DEBUG
    [RequireComponent(typeof(DebugVisualizer))]
#endif
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(WorldAudioController), ExecutionOrder)]
    public class WorldAudioController : TlpSingleton
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = DebugVisualizer.ExecutionOrder + 1;
        #endregion

        #region Constants
        private const int WarnFrameInterval = 60;
        #endregion

        #region Settings
        [Header("Settings")]
        [Tooltip(
                "Number of recalculations of emitter importance (distance) per frame. Higher values = more responsive updates but higher CPU cost per frame. Typical: 1-3. Anything > 1 has a noticable impact on CPU usage.")]
        [Range(1, 100)]
        [SerializeField]
        private int ImportanceUpdatesPerFrame = 1;

        [Tooltip(
                "Number of AudioSources to update per frame. When equal to Partitions, one AudioSource is updated per partition each frame. Higher values = more responsive but higher CPU cost per frame. Typical: 1-8")]
        [Range(1, 100)]
        [SerializeField]
        private int UpdatesPerFrame = 3;

        [Tooltip(
                "Limits concurrent active emitters. Beyond this count, emitters are culled starting from lowest importance. " +
                "Constrained by the audio system's virtual voice limit.")]
        [SerializeField]
        private int MaxActiveSources = 32;
        #endregion

        #region Dependencies
        [Header("Dependencies")]
        [Tooltip("Structure for efficient emitter lookup and sorting by distance (later importance)")]
        [SerializeField]
        internal AvlTree ImportanceTree;

        [SerializeField]
        internal PartitionState PartitionState;

        [SerializeField]
        internal PartitionCursor PartitionCursor;

        [SerializeField]
        internal EmitterRegistry EmitterRegistry;
        #endregion

#if TLP_DEBUG
        internal DebugVisualizer _debugVisualizer;
#endif

        #region State
        internal PlayerAudioController PlayerAudioController;
        private int _emitterIterationCursor;
        internal int _clampedUpdatesPerFrame;
        internal int _clampedImportanceUpdatesPerFrame;
        private int _lastWarnFrame;
        private int _frameOfLastForceUpdate = int.MinValue;
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

#if TLP_DEBUG
            _debugVisualizer = gameObject.GetComponent<DebugVisualizer>();
            if (!_IsSet(_debugVisualizer, nameof(_debugVisualizer))) {
                return false;
            }
#endif

            if (!_IsSet(ImportanceTree, nameof(ImportanceTree))) {
                return false;
            }

            if (!_IsSet(PartitionState, nameof(PartitionState))) {
                return false;
            }

            if (!_IsSet(PartitionCursor, nameof(PartitionCursor))) {
                return false;
            }

            PlayerAudioController = VoiceUtils._FindPlayerAudioController();
            if (!_IsSet(PlayerAudioController, nameof(PlayerAudioController))) {
                return false;
            }

            if (!_IsSet(EmitterRegistry, nameof(EmitterRegistry))) {
                return false;
            }


            if (!EmitterRegistry.HasStartedOk) {
                return false;
            }

            return true;
        }
        #endregion

        #region UpdateScheduler
        internal void _InitializeUpdateScheduler(int totalElements) {
#if TLP_DEBUG
            _DebugLog($"{nameof(_InitializeUpdateScheduler)}: {nameof(totalElements)}={totalElements}");
#endif

            _clampedUpdatesPerFrame = Mathf.Min(UpdatesPerFrame, totalElements);
            _clampedImportanceUpdatesPerFrame = Mathf.Min(ImportanceUpdatesPerFrame, totalElements);

#if TLP_DEBUG
            _debugVisualizer._InitializePartitionIndicators(PartitionState.ActivePartitions);
#endif
        }

        public override void PostLateUpdate() {
            if (!HasStartedOk) {
                return;
            }

            if (EmitterRegistry.NeedsRebuild) {
                EmitterRegistry._Initialize();
                int totalElements = EmitterRegistry.Emitters.Length;
                PartitionState._InitializePartitionManagement(totalElements);
                _InitializeUpdateScheduler(totalElements);
            }

            // skip the heaviest part of the update if we just did a force update this frame,
            // since that already updated the importance tree
            if (Time.frameCount != _frameOfLastForceUpdate) {
                _UpdateEmitterImportanceOrder();
            }

            _UpdateEmitters();
        }

        private void _UpdateEmitters() {
            PartitionCursor._ResetPartitionUpdateCounters();

            var config = PlayerAudioController.CurrentConfiguration;
            float environmentClarity = 1f - config.OcclusionFactor;
            float playerClarity = 1f - config.PlayerOcclusionFactor;
            int occlusionMask = config.OcclusionMask;
            var localOverride = PlayerAudioController.CurrentLocalOverride;

            for (int i = 0; i < _clampedUpdatesPerFrame; i++) {
                if (!PartitionCursor._FindNextOutdatedPartition(out int index, out int partitionToUpdate)) {
                    if (_lastWarnFrame + WarnFrameInterval < Time.frameCount) {
                        _lastWarnFrame = Time.frameCount;
                        _Warn($"{nameof(_UpdateEmitters)}: No outdated partition found after checking all partitions.");
                    }

                    break;
                }

                var emitter = (OccludedEmitter)ImportanceTree._Get(index);
                if (!Utilities.IsValid(emitter)) {
                    continue;
                }

#if TLP_DEBUG
                _debugVisualizer._OnPartitionUpdated(partitionToUpdate, emitter.transform.position);
#endif

                _UpdateAudioSource(
                        localOverride,
                        emitter,
                        environmentClarity,
                        playerClarity,
                        occlusionMask,
                        index >= MaxActiveSources);
            }
        }
        #endregion

        #region Importance Updater
        private void _UpdateEmitterImportanceOrder() {
            for (int i = 0; i < _clampedImportanceUpdatesPerFrame; i++) {
                _emitterIterationCursor._MoveIndexRightLooping(EmitterRegistry.Emitters.Length);
                var occludedEmitter = EmitterRegistry.Emitters[_emitterIterationCursor];
                if (!Utilities.IsValid(occludedEmitter)) {
                    continue;
                }

                occludedEmitter._RefreshInTree(ImportanceTree, MaxActiveSources);

#if TLP_DEBUG
                _debugVisualizer._OnTreeUpdated(occludedEmitter.transform.position + Vector3.up);
#endif
            }
        }
        #endregion

        #region Internal
        internal static string _ExpectedGameObjectName() {
            return $"TLP_{nameof(WorldAudioController)}";
        }

        internal void _ForceUpdate(GameObject o) {
            if (!HasStartedOk) {
                return;
            }

            var emitter = o.GetComponent<OccludedEmitter>();
            if (!Utilities.IsValid(emitter)) {
                return; // no-op
            }

            _frameOfLastForceUpdate = Time.frameCount;

            emitter._RefreshInTree(ImportanceTree, MaxActiveSources);
            bool willBeAudible = emitter._IsAudible(ImportanceTree, MaxActiveSources, out var lowestPrioActiveEmitter);
            if (willBeAudible && lowestPrioActiveEmitter != null) {
                // ensure we stay below the max limit by forcing the lowest priority active emitter to be culled
                lowestPrioActiveEmitter._UpdateAudioSourceState(0, true, false, 0);
            }

            var config = PlayerAudioController.CurrentConfiguration;
            float environmentClarity = 1f - config.OcclusionFactor;
            float playerClarity = 1f - config.PlayerOcclusionFactor;
            int occlusionMask = config.OcclusionMask;
            var localOverride = PlayerAudioController.CurrentLocalOverride;

#if TLP_DEBUG
            _debugVisualizer._OnPartitionUpdated(0, emitter.transform.position);
#endif
            _UpdateAudioSource(
                    localOverride,
                    emitter,
                    environmentClarity,
                    playerClarity,
                    occlusionMask,
                    !willBeAudible);
        }

        private void _UpdateAudioSource(
                PlayerAudioOverride localOverride,
                OccludedEmitter emitter,
                float environmentClarity,
                float playerClarity,
                int occlusionMask,
                bool cull
        ) {
            if (Utilities.IsValid(localOverride)) {
                bool emitterInChannel = localOverride._IsChannelAudible(emitter.PrivacyChannelId);
                bool canHear;

                if (emitterInChannel) {
                    canHear = !localOverride.DisallowListeningToChannel;
                } else if (emitter.PrivacyChannelId == PlayerAudioController.ChannelNoPrivacy) {
                    canHear = localOverride._IsPrivateChannel() && !localOverride.MuteOutsiders;
                } else {
                    canHear = false;
                }

                if (!canHear) {
                    emitter._UpdateAudioSourceState(
                            PartitionCursor._GetPartitionAsRatio(),
                            true,
                            true,
                            0f);
                    return;
                }
            } else if (emitter.PrivacyChannelId != PlayerAudioController.ChannelNoPrivacy) {
                emitter._UpdateAudioSourceState(
                        PartitionCursor._GetPartitionAsRatio(),
                        true,
                        true,
                        0f);
                return;
            }

            var trackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            float clarity = PlayerAudioController.PlayerOcclusionStrategy._GetEmitterAudioClarity(
                    trackingData.position,
                    emitter.transform.position,
                    environmentClarity,
                    playerClarity,
                    occlusionMask
            );
            emitter._UpdateAudioSourceState(
                    PartitionCursor._GetPartitionAsRatio(),
                    cull,
                    false,
                    clarity);
        }
        #endregion
    }
}