using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Sources.Time;
using TLP.UdonVoiceUtils.Runtime.Core.WorldAudio;
using TLP.UdonVoiceUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Testing
{
    [CustomEditor(typeof(TestSoundLooper))]
    public class TestSoundLooperEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Test helper that plays a sound at fixed intervals using network time. " +
                   "Used for testing WorldAudio emitter behavior with timed playback.";
        }
    }
}
#endif

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
[RequireComponent(typeof(OccludedEmitter))]
[TlpDefaultExecutionOrder(typeof(TestSoundLooper), 1000)]
public class TestSoundLooper : TlpBaseBehaviour
{
    #region ExecutionOrder
    public override int ExecutionOrderReadOnly => ExecutionOrder;

    [PublicAPI]
    public new const int ExecutionOrder = TestPlayersReceiveMasterHeightChange.ExecutionOrder + 1;
    #endregion

    private TlpNetworkTime _networkTime;
    public float Interval = 3f;
    private AudioSourceStateController _audioSourceStateController;

    protected override bool SetupAndValidate() {
        if (!base.SetupAndValidate()) {
            return false;
        }

        _networkTime = TlpNetworkTime.GetInstance();
        if (!Utilities.IsValid(_networkTime)) {
            return false;
        }

        _audioSourceStateController = GetComponent<AudioSourceStateController>();
        if (!IsSet(_audioSourceStateController, nameof(_audioSourceStateController))) {
            return false;
        }

        SendCustomEventDelayedSeconds(nameof(DelayedPlaySound), CalculateDelayUntilNextPlay());
        return true;
    }

    public void DelayedPlaySound() {
        if (!HasStartedOk) {
            return;
        }

        _audioSourceStateController.Play();
        SendCustomEventDelayedSeconds(nameof(DelayedPlaySound), CalculateDelayUntilNextPlay());
    }

    private float CalculateDelayUntilNextPlay() {
        double currentTime = _networkTime.TimeAsDouble();
        if (Interval <= 0) {
            return 1f; // fallback to 1 second if interval is invalid
        }

        double nextPlayTime = Mathf.Ceil((float)currentTime / Interval) * Interval;
        return (float)(nextPlayTime - currentTime);
    }
}