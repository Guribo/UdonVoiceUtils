using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts.Tests
{
    public class BetterAudioTestController : UdonSharpBehaviour
    {
        private bool _isTesting;

        public UdonSharpBehaviour[] tests;

        private bool _testInitialized;
        private bool _testCompleted;
        private bool _testCleanedUp;

        private int _testIndex = 0;

        private bool _pendingNextStep;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.LocalPlayer == player)
            {
                // start the tests
                _isTesting = true;
                ContinueTesting();
            }
        }

        public void Update()
        {
            if (_pendingNextStep)
            {
                _pendingNextStep = false;
                ContinueTesting();
            }

            if (!_isTesting && Input.GetKey(KeyCode.T) && Input.GetKey(KeyCode.E) && Input.GetKey(KeyCode.S))
            {
                StartTestRun();
            }
        }

        public void StartTestRun()
        {
            if (_isTesting)
            {
                Debug.LogWarning(
                    "BetterAudioTestController.StartTestRun: can not start a new test while another one is still running", this);
                return;
            }

            Debug.Log("BetterAudioTestController.StartTestRun", this);

            _isTesting = true;
            _testIndex = 0;
            _pendingNextStep = true;
        }

        private void ContinueTesting()
        {
            if (tests != null && tests.Length > 0 && _testIndex > -1 && _testIndex < tests.Length)
            {
                var udonSharpBehaviour = tests[_testIndex];
                if (!udonSharpBehaviour)
                {
                    Debug.LogError("BetterAudioTestController.ContinueTesting: tests contains invalid behaviour", this);
                    return;
                }

                var context = udonSharpBehaviour.gameObject;
                if (!_testInitialized)
                {
                    Debug.Log($"BetterAudioTestController.ContinueTesting: Initializing test {context}", context);
                    udonSharpBehaviour.SetProgramVariable("betterAudioTestController", this);
                    udonSharpBehaviour.SendCustomEvent("Initialize");
                    return;
                }

                if (!_testCompleted)
                {
                    Debug.Log($"BetterAudioTestController.ContinueTesting: Running test {context}", context);
                    udonSharpBehaviour.SendCustomEvent("Run");
                    return;
                }

                if (!_testCleanedUp)
                {
                    Debug.Log($"BetterAudioTestController.ContinueTesting: Cleaning up test {context}", context);
                    udonSharpBehaviour.SendCustomEvent("CleanUp");
                    return;
                }

                ++_testIndex;
                _isTesting = _testIndex < tests.Length;

                _testInitialized = false;
                _testCompleted = false;
                _testCleanedUp = false;

                _pendingNextStep = _isTesting;

                if (!_isTesting)
                {
                    Debug.Log("BetterAudioTestController.ContinueTesting: All test completed");
                }
            }
            else
            {
                Debug.LogError("Nothing to test");
                _testIndex = 0;
                _isTesting = false;
                _testInitialized = false;
                _testCompleted = false;
                _testCleanedUp = false;
                _pendingNextStep = false;
            }
        }

        public void TestInitialized(bool success)
        {
            Debug.Log($"BetterAudioTestController.TestInitialized: Initialized test successfully: {success}",this);
            _testInitialized = true;
            if (!success)
            {
                _testCompleted = true;
            }
            _pendingNextStep = true;
        }

        public void TestCompleted(bool success)
        {
            Debug.Log($"BetterAudioTestController.TestCompleted: Test ran successfully {success}",this);
            _testCompleted = true;
            _pendingNextStep = true;
        }

        public void TestCleanedUp(bool success)
        {
            Debug.Log($"BetterAudioTestController.TestCleanedUp: Cleaned up test successfully: {success}", this);
            _testCleanedUp = true;
            _pendingNextStep = true;
        }
    }
}