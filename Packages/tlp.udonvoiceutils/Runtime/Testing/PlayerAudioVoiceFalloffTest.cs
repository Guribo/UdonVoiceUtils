using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Testing;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.UdonNetworkCalling;
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

        public PlayerAudioController PlayerAudio;

        private int _currentStep;
        private VRCPlayerApi _voiceListener;
        private VRCPlayerApi _voiceEmitter;

        protected override void _InitializeTest() {
            if (!Networking.IsMaster) {
                _Error("Non-Master is not allowed to start the test");
                TestController._TestInitialized(false);
                return;
            }

            // verify 2 players are in the world
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (playerCount != 2) {
                _Error($"Requires 2 players to be present, found {playerCount}");
                TestController._TestInitialized(false);
                return;
            }

            var players = new VRCPlayerApi[2];
            players = VRCPlayerApi.GetPlayers(players);

            if (!Utilities.IsValid(players[0])) {
                _Error("First player is invalid");
                TestController._TestInitialized(false);
                return;
            }

            if (!Utilities.IsValid(players[1])) {
                _Error("Second player is invalid");
                TestController._TestInitialized(false);
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

            if (!_EnsureLocalOwnership()) {
                _Error("Failed to set local ownership");
                TestController._TestInitialized(false);
                return;
            }

            if (Utilities.IsValid(PlayerAudio)) {
                if (!PlayerAudio.enabled) {
                    _Error($"{nameof(PlayerAudio)} component must be disabled by default for this test");
                    TestController._TestInitialized(false);
                    return;
                }

                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(_RPC_SetAudioProperties));
            }

            // ensure both players can't move around
            _voiceEmitter.Immobilize(true);

            _currentStep = 0;
            _EmitterTeleportInFrontOfListener(_currentStep);

            TestController._TestInitialized(true);
        }

        [NetworkCallable(1)]
        public void _RPC_SetAudioProperties() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(
                    $"[<color=#008000>UdonVoiceUtils</color>] [<color=#804500>Testing</color>] {nameof(_RPC_SetAudioProperties)}"
            );
#endif
            #endregion _DebugLog(

            if (!Utilities.IsValid(PlayerAudio)) {
                _Error($"{nameof(PlayerAudio)} invalid");
                TestController._TestInitialized(false);
                return;
            }

            PlayerAudio.enabled = true;
        }

        [NetworkCallable(1)]
        public void _RPC_ClearAudioProperties() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(
                    $"[<color=#008000>UdonVoiceUtils</color>] [<color=#804500>Testing</color>] {nameof(_RPC_ClearAudioProperties)}"
            );
#endif
            #endregion

            if (!Utilities.IsValid(PlayerAudio)) {
                _Error($"{nameof(PlayerAudio)} invalid");
                TestController._TestCleanedUp(false);
                return;
            }

            PlayerAudio.enabled = false;
        }

        private void _EmitterTeleportInFrontOfListener(int step) {
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

        protected override void _RunTest() {
            SendCustomEventDelayedSeconds(nameof(_PerformStep), StartDelay, EventTiming.LateUpdate);
        }

        public void _PerformStep() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_PerformStep));
#endif
            #endregion

            if (Status != TestCaseStatus.Running) return;

            if (!Utilities.IsValid(_voiceEmitter)) {
                _Error("Voice emitting player is invalid");
                TestController._TestCompleted(false);
                return;
            }

            if (!Utilities.IsValid(_voiceListener)) {
                _Error("Voice listening player is invalid");
                TestController._TestCompleted(false);
                return;
            }

            _DebugLog(
                    $"[<color=#008000>UdonVoiceUtils</color>] [<color=#804500>Testing</color>] Teleporting local player to sample position {_currentStep}"
            );
            _EmitterTeleportInFrontOfListener(_currentStep);

            _currentStep++;
            if (_currentStep < Samples) {
                SendCustomEventDelayedSeconds(nameof(_PerformStep), StepInterval, EventTiming.LateUpdate);
            } else {
                TestController._TestCompleted(true);
            }
        }

        protected override void _CleanUpTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_CleanUpTest));
#endif
            #endregion

            if (!Utilities.IsValid(_voiceEmitter)) {
                _Error("Voice emitting player is invalid");
                TestController._TestCleanedUp(false);
                return;
            }

            if (!Utilities.IsValid(_voiceListener)) {
                _Error("Voice listening player is invalid");
                TestController._TestCleanedUp(false);
                return;
            }

            _EmitterTeleportInFrontOfListener(1);

            if (Utilities.IsValid(PlayerAudio)) {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(_RPC_ClearAudioProperties));
            }

            _voiceEmitter.Immobilize(false);
            TestController._TestCleanedUp(true);
        }
    }
}