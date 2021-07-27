using System;
using Guribo.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Enums;
using VRC.Udon.Common.Interfaces;

namespace Guribo.UdonBetterAudio.Tests.Runtime.BetterPlayerAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BetterPlayerAudioVoiceFalloffTest : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [NonSerialized] public TestController TestController;

        public void Initialize()
        {
            if (!TestController)
            {
                Debug.LogError(
                    "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!TestController)
            {
                Debug.LogError(
                    "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!TestController)
            {
                Debug.LogError(
                    "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp", this);
            CleanUpTest();
        }

        private bool Assert(bool condition, string message)
        {
            if (!condition)
            {
                if (Utilities.IsValid(this))
                {
                    Debug.LogError(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Assertion failed : '" +
                        GetType() + " : " + message + "'", this);
                }
                else
                {
                    Debug.LogError(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Assertion failed :  'UNKNOWN TYPE: " +
                        message + "'");
                }

                return false;
            }

            Debug.Assert(condition, message);
            return true;
        }

        #endregion

        #region EDIT HERE

        public int samples = 100;
        public float stepSize = 1f;
        public float stepInterval = 1f;
        public float startDelay = 13f;

        [Range(0, 180f)]
        public float emitterAngle = 0f;
        [Range(0, 180f)]
        public float listenerAngle = 0f;
        public UdonBehaviour betterPlayerAudio;

        private int _currentStep;
        private VRCPlayerApi _voiceListener;
        private VRCPlayerApi _voiceEmitter;

        private void InitializeTest()
        {
            if (!Assert(Networking.IsMaster, "Non-Master is not allowed to start the test"))
            {
                TestController.TestInitialized(false);
                return;
            }

            // verify 2 players are in the world
            var playerCount = VRCPlayerApi.GetPlayerCount();
            if (!Assert(playerCount == 2, $"Requires 2 players to be present, found {playerCount}"))
            {
                TestController.TestInitialized(false);
                return;
            }

            var players = new VRCPlayerApi[2];
            players = VRCPlayerApi.GetPlayers(players);

            if (!Assert(Utilities.IsValid(players[0]), "First player is invalid"))
            {
                TestController.TestInitialized(false);
                return;
            }

            if (!Assert(Utilities.IsValid(players[1]), "Second player is invalid"))
            {
                TestController.TestInitialized(false);
                return;
            }

            // turn the local player into the voice emitter and the other player into the listener
            if (players[0].isLocal)
            {
                _voiceEmitter = players[0];
                _voiceListener = players[1];
            }
            else
            {
                _voiceEmitter = players[1];
                _voiceListener = players[0];
            }

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (Utilities.IsValid(betterPlayerAudio))
            {
                if (!Assert(!betterPlayerAudio.enabled,
                    "betterPlayerAudio component must be disabled by default for this test"))
                {
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

        public void SetAudioProperties()
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] SetAudioProperties",
                this);
            if (!Assert(Utilities.IsValid(betterPlayerAudio), $"betterPlayerAudio invalid"))
            {
                TestController.TestInitialized(false);
                return;
            }

            betterPlayerAudio.enabled = true;
        }

        public void ClearAudioProperties()
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] ClearAudioProperties",
                this);
            if (!Assert(Utilities.IsValid(betterPlayerAudio), $"betterPlayerAudio invalid"))
            {
                TestController.TestCleanedUp(false);
                return;
            }

            betterPlayerAudio.enabled = false;
        }

        private void EmitterTeleportInFrontOfListener(int step)
        {
            var forward = (_voiceListener.GetRotation() * Quaternion.Euler(0, listenerAngle, 0)) * Vector3.forward;
            var positionOffset = step * stepSize * forward;
            var teleportPosition = _voiceListener.GetPosition() + positionOffset;
            _voiceEmitter.TeleportTo(teleportPosition,
                Quaternion.LookRotation(-forward, Vector3.up) * Quaternion.Euler(0, emitterAngle, 0),
                VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                false);
        }

        private void RunTest()
        {
            SendCustomEventDelayedSeconds("PerformStep", startDelay, EventTiming.LateUpdate);
        }

        public void PerformStep()
        {
            if (!Assert(Utilities.IsValid(this) || !gameObject.activeInHierarchy, "This component is no longer valid"))
            {
                TestController.TestCompleted(false);
            }

            if (!Assert(Utilities.IsValid(_voiceEmitter), "Voice emitting player is invalid"))
            {
                TestController.TestCompleted(false);
                return;
            }

            if (!Assert(Utilities.IsValid(_voiceListener), "Voice listening player is invalid"))
            {
                TestController.TestCompleted(false);
                return;
            }

            Debug.Log(
                $"[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Teleporting local player to sample position {_currentStep}",
                this);
            EmitterTeleportInFrontOfListener(_currentStep);

            _currentStep++;
            if (_currentStep < samples)
            {
                SendCustomEventDelayedSeconds("PerformStep", stepInterval, EventTiming.LateUpdate);
            }
            else
            {
                TestController.TestCompleted(true);
            }
        }

        private void CleanUpTest()
        {
            if (!Assert(Utilities.IsValid(this) || !gameObject.activeInHierarchy, "This component is no longer valid"))
            {
                TestController.TestCleanedUp(false);
            }

            if (!Assert(Utilities.IsValid(_voiceEmitter), "Voice emitting player is invalid"))
            {
                TestController.TestCleanedUp(false);
                return;
            }

            if (!Assert(Utilities.IsValid(_voiceListener), "Voice listening player is invalid"))
            {
                TestController.TestCleanedUp(false);
                return;
            }

            EmitterTeleportInFrontOfListener(1);

            if (Utilities.IsValid(betterPlayerAudio))
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, "ClearAudioProperties");
            }

            _voiceEmitter.Immobilize(false);
            TestController.TestCleanedUp(true);
        }

        #endregion
    }
}