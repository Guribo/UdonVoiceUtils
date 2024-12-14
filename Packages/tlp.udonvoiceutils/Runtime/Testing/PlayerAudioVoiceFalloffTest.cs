using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Testing;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonVoiceUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerAudioVoiceFalloffTest), ExecutionOrder)]
    public class PlayerAudioVoiceFalloffTest : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestCase.ExecutionOrder + 101;

        [FormerlySerializedAs("samples")]
        public int Samples = 100;

        [FormerlySerializedAs("stepSize")]
        public float StepSize = 1f;

        [FormerlySerializedAs("stepInterval")]
        public float StepInterval = 1f;

        [FormerlySerializedAs("startDelay")]
        public float StartDelay = 13f;

        [FormerlySerializedAs("emitterAngle")]
        [Range(0, 180f)]
        public float EmitterAngle;

        [FormerlySerializedAs("listenerAngle")]
        [Range(0, 180f)]
        public float ListenerAngle;

        [FormerlySerializedAs("BetterPlayerAudio")]
        [FormerlySerializedAs("betterPlayerAudio")]
        public PlayerAudioController PlayerAudio;

        private int _currentStep;
        private VRCPlayerApi _voiceListener;
        private VRCPlayerApi _voiceEmitter;

        protected override void InitializeTest() {
            if (!Networking.IsMaster) {
                Error("Non-Master is not allowed to start the test");
                TestController.TestInitialized(false);
                return;
            }

            // verify 2 players are in the world
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (playerCount != 2) {
                Error($"Requires 2 players to be present, found {playerCount}");
                TestController.TestInitialized(false);
                return;
            }

            var players = new VRCPlayerApi[2];
            players = VRCPlayerApi.GetPlayers(players);

            if (!Utilities.IsValid(players[0])) {
                Error("First player is invalid");
                TestController.TestInitialized(false);
                return;
            }

            if (!Utilities.IsValid(players[1])) {
                Error("Second player is invalid");
                TestController.TestInitialized(false);
                return;
            }

            // turn the local player into the voice emitter and the other player into the listener
            if (players[0].isLocal) {
                _voiceEmitter = players[0];
                _voiceListener = players[1];
            } else {
                _voiceEmitter = players[1];
                _voiceListener = players[0];
            }

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (Utilities.IsValid(PlayerAudio)) {
                if (!PlayerAudio.enabled) {
                    Error($"{nameof(PlayerAudio)} component must be disabled by default for this test");
                    TestController.TestInitialized(false);
                    return;
                }

                SendCustomNetworkEvent(NetworkEventTarget.All, "SetAudioProperties");
            }

            // ensure both players can't move around
            _voiceEmitter.Immobilize(true);

            _currentStep = 0;
            EmitterTeleportInFrontOfListener(_currentStep);

            TestController.TestInitialized(true);
        }

        public void SetAudioProperties() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"[<color=#008000>UdonVoiceUtils</color>] [<color=#804500>Testing</color>] {nameof(SetAudioProperties)}"
            );
#endif
            #endregion DebugLog(

            if (!Utilities.IsValid(PlayerAudio)) {
                Error($"{nameof(PlayerAudio)} invalid");
                TestController.TestInitialized(false);
                return;
            }

            PlayerAudio.enabled = true;
        }

        public void ClearAudioProperties() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"[<color=#008000>UdonVoiceUtils</color>] [<color=#804500>Testing</color>] {nameof(ClearAudioProperties)}"
            );
#endif
            #endregion

            if (!Utilities.IsValid(PlayerAudio)) {
                Error($"{nameof(PlayerAudio)} invalid");
                TestController.TestCleanedUp(false);
                return;
            }

            PlayerAudio.enabled = false;
        }

        private void EmitterTeleportInFrontOfListener(int step) {
            var forward = _voiceListener.GetRotation() * Quaternion.Euler(0, ListenerAngle, 0) * Vector3.forward;
            var positionOffset = step * StepSize * forward;
            var teleportPosition = _voiceListener.GetPosition() + positionOffset;
            _voiceEmitter.TeleportTo(
                    teleportPosition,
                    Quaternion.LookRotation(-forward, Vector3.up) * Quaternion.Euler(0, EmitterAngle, 0),
                    VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                    false
            );
        }

        protected override void RunTest() {
            SendCustomEventDelayedSeconds(nameof(PerformStep), StartDelay, EventTiming.LateUpdate);
        }

        public void PerformStep() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(PerformStep));
#endif
            #endregion

            if (Utilities.IsValid(this) || !gameObject.activeInHierarchy) {
                Error($"{name} is no longer valid");
                TestController.TestCompleted(false);
            }

            if (!Utilities.IsValid(_voiceEmitter)) {
                Error("Voice emitting player is invalid");
                TestController.TestCompleted(false);
                return;
            }

            if (!Utilities.IsValid(_voiceListener)) {
                Error("Voice listening player is invalid");
                TestController.TestCompleted(false);
                return;
            }

            DebugLog(
                    $"[<color=#008000>UdonVoiceUtils</color>] [<color=#804500>Testing</color>] Teleporting local player to sample position {_currentStep}"
            );
            EmitterTeleportInFrontOfListener(_currentStep);

            _currentStep++;
            if (_currentStep < Samples) {
                SendCustomEventDelayedSeconds("PerformStep", StepInterval, EventTiming.LateUpdate);
            } else {
                TestController.TestCompleted(true);
            }
        }

        protected override void CleanUpTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CleanUpTest));
#endif
            #endregion

            if (!Utilities.IsValid(this) || !gameObject.activeInHierarchy) {
                Error($"{name} is no longer valid");
                TestController.TestCleanedUp(false);
            }

            if (!Utilities.IsValid(_voiceEmitter)) {
                Error("Voice emitting player is invalid");
                TestController.TestCleanedUp(false);
                return;
            }

            if (!Utilities.IsValid(_voiceListener)) {
                Error("Voice listening player is invalid");
                TestController.TestCleanedUp(false);
                return;
            }

            EmitterTeleportInFrontOfListener(1);

            if (Utilities.IsValid(PlayerAudio)) {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ClearAudioProperties));
            }

            _voiceEmitter.Immobilize(false);
            TestController.TestCleanedUp(true);
        }
    }
}