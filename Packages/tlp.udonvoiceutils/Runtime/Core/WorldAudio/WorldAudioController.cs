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
        protected override string GetDescription() {
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
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

#if TLP_DEBUG
            _debugVisualizer = gameObject.GetComponent<DebugVisualizer>();
            if (!IsSet(_debugVisualizer, nameof(_debugVisualizer))) {
                return false;
            }
#endif

            if (!IsSet(ImportanceTree, nameof(ImportanceTree))) {
                return false;
            }

            if (!IsSet(PartitionState, nameof(PartitionState))) {
                return false;
            }

            if (!IsSet(PartitionCursor, nameof(PartitionCursor))) {
                return false;
            }

            PlayerAudioController = VoiceUtils.FindPlayerAudioController();
            if (!IsSet(PlayerAudioController, nameof(PlayerAudioController))) {
                return false;
            }

            if (!IsSet(EmitterRegistry, nameof(EmitterRegistry))) {
                return false;
            }


            if (!EmitterRegistry.HasStartedOk) {
                return false;
            }

            return true;
        }
        #endregion

        #region UpdateScheduler
        internal void InitializeUpdateScheduler(int totalElements) {
#if TLP_DEBUG
            DebugLog($"{nameof(InitializeUpdateScheduler)}: {nameof(totalElements)}={totalElements}");
#endif

            _clampedUpdatesPerFrame = Mathf.Min(UpdatesPerFrame, totalElements);
            _clampedImportanceUpdatesPerFrame = Mathf.Min(ImportanceUpdatesPerFrame, totalElements);

#if TLP_DEBUG
            _debugVisualizer.InitializePartitionIndicators(PartitionState.ActivePartitions);
#endif
        }

        public override void PostLateUpdate() {
            if (!HasStartedOk) {
                return;
            }

            if (EmitterRegistry.NeedsRebuild) {
                EmitterRegistry.Initialize();
                int totalElements = EmitterRegistry.Emitters.Length;
                PartitionState.InitializePartitionManagement(totalElements);
                InitializeUpdateScheduler(totalElements);
            }

            // skip the heaviest part of the update if we just did a force update this frame,
            // since that already updated the importance tree
            if (Time.frameCount != _frameOfLastForceUpdate) {
                UpdateEmitterImportanceOrder();
            }

            UpdateEmitters();
        }

        private void UpdateEmitters() {
            PartitionCursor.ResetPartitionUpdateCounters();

            var config = PlayerAudioController.CurrentConfiguration;
            float environmentClarity = 1f - config.OcclusionFactor;
            float playerClarity = 1f - config.PlayerOcclusionFactor;
            int occlusionMask = config.OcclusionMask;
            var localOverride = PlayerAudioController.CurrentLocalOverride;

            for (int i = 0; i < _clampedUpdatesPerFrame; i++) {
                if (!PartitionCursor.FindNextOutdatedPartition(out int index, out int partitionToUpdate)) {
                    if (_lastWarnFrame + WarnFrameInterval < Time.frameCount) {
                        _lastWarnFrame = Time.frameCount;
                        Warn($"{nameof(UpdateEmitters)}: No outdated partition found after checking all partitions.");
                    }

                    break;
                }

                var emitter = (OccludedEmitter)ImportanceTree.Get(index);
                if (!Utilities.IsValid(emitter)) {
                    continue;
                }

#if TLP_DEBUG
                _debugVisualizer.OnPartitionUpdated(partitionToUpdate, emitter.transform.position);
#endif

                UpdateAudioSource(
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
        private void UpdateEmitterImportanceOrder() {
            for (int i = 0; i < _clampedImportanceUpdatesPerFrame; i++) {
                _emitterIterationCursor.MoveIndexRightLooping(EmitterRegistry.Emitters.Length);
                var occludedEmitter = EmitterRegistry.Emitters[_emitterIterationCursor];
                if (!Utilities.IsValid(occludedEmitter)) {
                    continue;
                }

                occludedEmitter.RefreshInTree(ImportanceTree, MaxActiveSources);

#if TLP_DEBUG
                _debugVisualizer.OnTreeUpdated(occludedEmitter.transform.position + Vector3.up);
#endif
            }
        }
        #endregion

        #region Internal
        internal static string ExpectedGameObjectName() {
            return $"TLP_{nameof(WorldAudioController)}";
        }

        internal void ForceUpdate(GameObject o) {
            if (!HasStartedOk) {
                return;
            }

            var emitter = o.GetComponent<OccludedEmitter>();
            if (!Utilities.IsValid(emitter)) {
                return; // no-op
            }

            _frameOfLastForceUpdate = Time.frameCount;

            emitter.RefreshInTree(ImportanceTree, MaxActiveSources);
            bool willBeAudible = emitter.IsAudible(ImportanceTree, MaxActiveSources, out var lowestPrioActiveEmitter);
            if (willBeAudible && lowestPrioActiveEmitter != null) {
                // ensure we stay below the max limit by forcing the lowest priority active emitter to be culled
                lowestPrioActiveEmitter.UpdateAudioSourceState(0, true, false, 0);
            }

            var config = PlayerAudioController.CurrentConfiguration;
            float environmentClarity = 1f - config.OcclusionFactor;
            float playerClarity = 1f - config.PlayerOcclusionFactor;
            int occlusionMask = config.OcclusionMask;
            var localOverride = PlayerAudioController.CurrentLocalOverride;

#if TLP_DEBUG
            _debugVisualizer.OnPartitionUpdated(0, emitter.transform.position);
#endif
            UpdateAudioSource(
                    localOverride,
                    emitter,
                    environmentClarity,
                    playerClarity,
                    occlusionMask,
                    !willBeAudible);
        }

        private void UpdateAudioSource(
                PlayerAudioOverride localOverride,
                OccludedEmitter emitter,
                float environmentClarity,
                float playerClarity,
                int occlusionMask,
                bool cull
        ) {
            if (Utilities.IsValid(localOverride)) {
                bool emitterInChannel = localOverride.IsChannelAudible(emitter.PrivacyChannelId);
                bool canHear;

                if (emitterInChannel) {
                    canHear = !localOverride.DisallowListeningToChannel;
                } else if (emitter.PrivacyChannelId == PlayerAudioController.ChannelNoPrivacy) {
                    canHear = localOverride.IsPrivateChannel() && !localOverride.MuteOutsiders;
                } else {
                    canHear = false;
                }

                if (!canHear) {
                    emitter.UpdateAudioSourceState(
                            PartitionCursor.GetPartitionAsRatio(),
                            true,
                            true,
                            0f);
                    return;
                }
            } else if (emitter.PrivacyChannelId != PlayerAudioController.ChannelNoPrivacy) {
                emitter.UpdateAudioSourceState(
                        PartitionCursor.GetPartitionAsRatio(),
                        true,
                        true,
                        0f);
                return;
            }

            var trackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            float clarity = PlayerAudioController.PlayerOcclusionStrategy.GetEmitterAudioClarity(
                    trackingData.position,
                    emitter.transform.position,
                    environmentClarity,
                    playerClarity,
                    occlusionMask
            );
            emitter.UpdateAudioSourceState(
                    PartitionCursor.GetPartitionAsRatio(),
                    cull,
                    false,
                    clarity);
        }
        #endregion
    }
}