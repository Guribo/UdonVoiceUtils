using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Extensions;
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
    [CustomEditor(typeof(ReverbController))]
    public class ReverbControllerEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Updates reverb and lowpass filter parameters on an emitter's AudioSource " +
                   "based on distance and clarity. Requires AudioReverbFilter and AudioLowPassFilter " +
                   "on the same GameObject.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter), typeof(AudioReverbFilter))]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ReverbController), ExecutionOrder)]
    public class ReverbController : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        public new const int ExecutionOrder = WorldAudioController.ExecutionOrder + 1;
        #endregion

        #region Constants
        private const float MaxFrequency = 22000;
        private const float MinFrequency = 10;
        private const float MinLateReverberationLevel = -10000f;
        private const float MinEarlyReflectionLevel = -10000f;
        private const float MaxDryLevel = 0f;
        #endregion

        #region Configuration
        [Header("Configuration")]
        [Tooltip(
                "Exponent controlling how sharply lowpass cutoff drops as clarity decreases. " +
                "1 = linear fade (audible at any range). " +
                "2 = quadratic (subtle at close range, sharp drop near max range). " +
                "3+ = very sharp drop, audio stays clear until nearly out of range.")]
        [SerializeField]
        private float LowpassFilterCurveExponent = 2;

        [Tooltip(
                "Controls how much reverb applies when the player is near min range. " +
                "At 0, reverb is effectively off at min range. " +
                "At 0.5, reverb is at 50% at min range. " +
                "At 1, reverb is always 100% regardless of distance.")]
        [SerializeField]
        [Range(0, 1)]
        private float ReverbImpactWhenClose = 0.5f;
        #endregion

        #region State
        private AudioSource _audioSource;
        private AudioLowPassFilter _lowPassFilter;
        private AudioReverbFilter _reverbFilter;

        private float _maxReverbLevel;
        private float _maxReflectionsLevel;
        private float _maxDryLevel;
        private AnimationCurve _spatialBlendCurveBackup;
        #endregion

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            _audioSource = GetComponent<AudioSource>();
            if (!_IsSet(_audioSource, nameof(_audioSource))) {
                return false;
            }

            _reverbFilter = GetComponent<AudioReverbFilter>();
            if (!_IsSet(_reverbFilter, nameof(_reverbFilter))) {
                return false;
            }

            _lowPassFilter = GetComponent<AudioLowPassFilter>();
            if (!_IsSet(_lowPassFilter, nameof(_lowPassFilter))) {
                return false;
            }

            _maxReverbLevel = _reverbFilter.reverbLevel;
            _maxReflectionsLevel = _reverbFilter.reflectionsLevel;
            _maxDryLevel = _reverbFilter.dryLevel;
            _reverbFilter.reverbPreset = AudioReverbPreset.User;

            _spatialBlendCurveBackup = _audioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
            // _audioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, null);

            return true;
        }

        public void _UpdateReverb(float distance, float clarity) {
            if (!HasStartedOk) {
                return;
            }

            float logarithmicFalloff = VoiceUtils._LogarithmicFalloff(_audioSource.minDistance, distance);
            float cutoffFrequency = UdonMath._Remap(
                    0,
                    1,
                    MinFrequency,
                    MaxFrequency,
                    Mathf.Pow(clarity, LowpassFilterCurveExponent) * logarithmicFalloff);
            _lowPassFilter.cutoffFrequency = cutoffFrequency;

            float reverbImpact = Mathf.Lerp(1 - logarithmicFalloff, 1, ReverbImpactWhenClose);

            float lerp = Mathf.Lerp(1f, reverbImpact, clarity);
            _reverbFilter.reverbLevel = Mathf.Lerp(MinLateReverberationLevel, _maxReverbLevel, lerp);
            _reverbFilter.reflectionsLevel = Mathf.Lerp(
                    MinEarlyReflectionLevel,
                    _maxReflectionsLevel,
                    lerp);
            _reverbFilter.dryLevel = Mathf.Lerp(MaxDryLevel, _maxDryLevel, lerp);

            float spatialBlend = _spatialBlendCurveBackup.Evaluate(distance / _audioSource.maxDistance);
            float adjusted = Mathf.Lerp(1, spatialBlend, clarity);
            _audioSource.spatialBlend = adjusted;
        }
    }
}