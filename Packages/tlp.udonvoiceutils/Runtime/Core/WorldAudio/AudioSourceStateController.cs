using TLP.UdonUtils.Runtime;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
using UdonSharp;
using UnityEngine;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Core.WorldAudio;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Core.WorldAudio
{
    [CustomEditor(typeof(AudioSourceStateController))]
    public class AudioSourceStateControllerEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Manages AudioSource play/pause/stop state and handles culling suspension. " +
                   "When culled, playback is frozen and the logical position is preserved so " +
                   "Reactivate() can resume seamlessly. One per AudioSource on OccludedEmitter.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    public enum AudioSourceState
    {
        Playing,
        Paused,
        Stopped
    }

    /// <summary>
    /// Manages audio source lifecycle with support for culling (suspend/resume) and position tracking.
    /// When culled, the actual AudioSource is stopped but the logical position and state are preserved
    /// so playback can resume seamlessly via <see cref="Reactivate"/>.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [RequireComponent(typeof(AudioSource))]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(AudioSourceStateController), ExecutionOrder)]
    public class AudioSourceStateController : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        public new const int ExecutionOrder = ReverbController.ExecutionOrder + 1;
        #endregion

        #region Configuration
        [Header("Configuration")]
        [Tooltip(
                "If true, the priority of the AudioSource will instantly be recalculated resulting in the " +
                "lowest possible latency when reactivating after culling (useful for e.g. gunshots, " +
                "explosions etc.). If false, the AudioSource will resume whenever the world audio system processes " +
                "it again a few frames later. Enabling this may cause a minor hitch when multiple " +
                "AudioSourceStateController are Started/Unpaused in the same frame.")]
        public bool LowLatencyMode = true;
        #endregion

        #region State
        private AudioSource _audioSource;
        private double _playTimeWhenCulled;
        internal double _cullStartTime = -1f;
        internal AudioSourceState _requestedState;
        private bool _isCulled;
        internal float _clipLength;
        private WorldAudioController _worldAudioController;
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            _audioSource = GetComponent<AudioSource>();
            if (!_IsSet(_audioSource, nameof(_audioSource))) {
                return false;
            }

            _worldAudioController = VoiceUtils._FindWorldAudioController();
            if (!_IsSet(_worldAudioController, nameof(_worldAudioController))) {
                return false;
            }

            _requestedState = _audioSource.isPlaying || _audioSource.playOnAwake
                    ? AudioSourceState.Playing
                    : AudioSourceState.Stopped;

            _playTimeWhenCulled = _audioSource.time;
            _cullStartTime = Time.realtimeSinceStartupAsDouble;

            return true;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Starts playback and sets requested state to <see cref="AudioSourceState.Playing"/>.
        /// If culled, resets the play position and cull start time instead of playing directly —
        /// actual playback resumes on the next <see cref="Reactivate"/>.
        /// </summary>
        public void _Play() {
            if (!HasStartedOk) {
                return;
            }

            _requestedState = AudioSourceState.Playing;

            if (_isCulled) {
                _playTimeWhenCulled = 0f;
                _cullStartTime = Time.realtimeSinceStartupAsDouble;

                if (LowLatencyMode) {
                    _worldAudioController._ForceUpdate(gameObject);
                }

                return;
            }

            _PlayWithBugFix();
        }

        /// <summary>
        /// Pauses playback and saves the current position. If already culled, accumulates elapsed cull time
        /// into the saved position instead of pausing the AudioSource.
        /// Sets requested state to <see cref="AudioSourceState.Paused"/>.
        /// </summary>
        public void _Pause() {
            if (!HasStartedOk) {
                return;
            }

            _requestedState = AudioSourceState.Paused;
            if (_isCulled) {
                double elapsed = Time.realtimeSinceStartupAsDouble - _cullStartTime;
                _playTimeWhenCulled += elapsed;
                if (_audioSource.loop) {
                    _playTimeWhenCulled %= _clipLength;
                }

                _cullStartTime = Time.realtimeSinceStartupAsDouble;
                return;
            }

            _playTimeWhenCulled = _audioSource.time;
            _audioSource.Pause();
        }

        /// <summary>
        /// Resumes playback from the saved position. If culled, resets the cull start time so the clip
        /// is treated as virtually playing.
        /// Sets requested state to <see cref="AudioSourceState.Playing"/>.
        /// </summary>
        public void _UnPause() {
            if (!HasStartedOk) {
                return;
            }

            _requestedState = AudioSourceState.Playing;

            if (_isCulled) {
                _cullStartTime = Time.realtimeSinceStartupAsDouble; // ensure that the clip is virtually playing now

                if (LowLatencyMode) {
                    _worldAudioController._ForceUpdate(gameObject);
                }

                return;
            }

            _PlayWithBugFix();
            _audioSource.time = (float)_playTimeWhenCulled;
        }

        /// <summary>
        /// Stops playback and sets requested state to <see cref="AudioSourceState.Stopped"/>.
        /// If culled, only updates state without stopping the AudioSource (it is already stopped).
        /// </summary>
        public void _Stop() {
            if (!HasStartedOk) {
                return;
            }

            _requestedState = AudioSourceState.Stopped;
            if (_isCulled) {
                return;
            }

            _audioSource.Stop();
        }

        /// <summary>
        /// Suspends playback: stops the AudioSource and records current time and clip length.
        /// The logical playback position is saved so <see cref="Reactivate"/> can resume seamlessly.
        /// Only has an effect when <see cref="_requestedState"/> is <see cref="AudioSourceState.Playing"/>.
        /// </summary>
        public void _Cull() {
            if (!HasStartedOk) {
                return;
            }

            if (_isCulled) {
                return;
            }

            _isCulled = true;
            if (_requestedState != AudioSourceState.Playing) {
                return;
            }

            _playTimeWhenCulled = _audioSource.time;
            _cullStartTime = Time.realtimeSinceStartupAsDouble;
            _clipLength = _audioSource.clip != null ? _audioSource.clip.length : 0f;
            _audioSource.Stop();
        }

        /// <summary>
        /// Resumes playback after a <see cref="Cull"/>. Calculates the elapsed cull time and resumes
        /// from the correct position, wrapping time if the clip is looping. Does nothing if the
        /// AudioSource is already playing, the requested state is not <see cref="AudioSourceState.Playing"/>,
        /// or the clip has ended during culling.
        /// </summary>
        public void _Reactivate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Reactivate));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!_isCulled) {
                return;
            }

            _isCulled = false;
            if (_requestedState != AudioSourceState.Playing) {
                return;
            }

            if (_audioSource.clip == null) {
                return;
            }

            if (!Mathf.Approximately(_audioSource.clip.length, _clipLength)) {
                _PlayWithBugFix(); // clip changed since cull, just play from the start
                return;
            }

            double elapsed = Time.realtimeSinceStartupAsDouble - _cullStartTime;
            _cullStartTime = -1f;

            if (_clipLength <= 0f) {
                return;
            }

            double resumePlayTime = _playTimeWhenCulled + elapsed;
            if (_audioSource.loop) {
                resumePlayTime %= _clipLength;
            }

            if (resumePlayTime >= _clipLength) {
                return;
            }

            _PlayWithBugFix();
            _audioSource.time = (float)resumePlayTime;

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"Reactivating at time {resumePlayTime}");
#endif
            #endregion
        }
        #endregion

        #region Internal
        private void _PlayWithBugFix() {
            // ensures audio filters initialize properly afterward in case the gameobject was disabled
            _audioSource.Stop();
            _audioSource.Play();
        }
        #endregion
    }
}