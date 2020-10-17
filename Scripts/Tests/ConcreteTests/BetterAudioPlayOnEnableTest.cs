using System;
using UdonSharp;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Scripts.Tests.ConcreteTests
{
    public class BetterAudioPlayOnEnableTest : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [HideInInspector] public BetterAudioTestController betterAudioTestController;

        public void Initialize()
        {
            if (!betterAudioTestController)
            {
                Debug.LogError("Test.Initialize: invalid test controller", this);
                return;
            }
            Debug.Log("Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!betterAudioTestController)
            {
                Debug.LogError("Test.Run: invalid test controller", this);
                return;
            }

            Debug.Log("Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!betterAudioTestController)
            {
                Debug.LogError("Test.CleanUp: invalid test controller", this);
                return;
            }
            Debug.Log("Test.CleanUp", this);
            CleanUpTest();
        }

        #endregion
        #region EDIT HERE

        [SerializeField] private BetterAudioSource betterAudioSource;

        private bool _pendingCheck;
        private float _scheduledCheckTime;
        private float _expectedAudioTime;
        private float _maxDifference;
        private bool _secondPass;

        private void InitializeTest()
        {
            if (!betterAudioSource)
            {
                Debug.LogError("Invalid better audio source", this);
                betterAudioTestController.TestInitialized(false);
                return;
            }

            if (!betterAudioSource.gameObject.activeInHierarchy)
            {
                Debug.LogError("Better audio source must be active in the scene", this);
                betterAudioTestController.TestInitialized(false);
                return;
            }

            if (!betterAudioSource.playOnEnable)
            {
                Debug.LogError("Better audio source must have play on enable set to true", this);
                betterAudioTestController.TestInitialized(false);
                return;
            }

            if (betterAudioSource.playDelayedAccordingToDistance)
            {
                Debug.LogError("Better audio source must have playDelayedAccordingToDistance set to false", this);
                betterAudioTestController.TestInitialized(false);
                return;
            }

            _pendingCheck = false;
            _secondPass = false;

            betterAudioTestController.TestInitialized(true);
        }

        private void RunTest()
        {
            var proxyAudioSource = betterAudioSource.GetAudioSourceProxy();
            if (!proxyAudioSource)
            {
                Debug.LogError("Better audio source has no audio source proxy", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (!proxyAudioSource.loop)
            {
                Debug.LogError("Proxy audio source must have loop set to true", this);
                betterAudioTestController.TestInitialized(false);
                return;
            }

            var actualAudioSource = betterAudioSource.GetActualAudioSource();
            if (!actualAudioSource)
            {
                Debug.LogError("Better audio source has no actual audio source and is thus not playing", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (!actualAudioSource.isPlaying)
            {
                Debug.LogError("Actual audio source is not playing", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(proxyAudioSource.minDistance - actualAudioSource.minDistance) > 0.001f)
            {
                Debug.LogError("Actual audio source has not been initialized properly from the proxy audio source",
                    this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(proxyAudioSource.maxDistance - actualAudioSource.maxDistance) > 0.001f)
            {
                Debug.LogError("Actual audio source has not been initialized properly from the proxy audio source",
                    this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(actualAudioSource.dopplerLevel) > 0.001f)
            {
                Debug.LogError($"DopplerLevel is not 0, is {actualAudioSource.dopplerLevel}", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(proxyAudioSource.pitch - 1f) > 0.001f)
            {
                Debug.LogError($"Test requires proxy audio source to have a pitch of 1, is {proxyAudioSource.pitch}",
                    this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(proxyAudioSource.pitch - actualAudioSource.pitch) > 0.001f)
            {
                Debug.LogError("Actual audio source must have the same pitch as the proxy audio source for this test",
                    this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (proxyAudioSource.clip.length < 4f)
            {
                Debug.LogError("Test requires the audio clip to be at least 4 seconds long", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            var waitDuration = 3f;

            _scheduledCheckTime = Time.time + waitDuration;

            actualAudioSource.time = 0f;
            _expectedAudioTime = waitDuration;
            _pendingCheck = true;
            _maxDifference = Time.deltaTime;

            Debug.Log("Waiting for delayed checks", this);
        }

        private void Update()
        {
            if (_pendingCheck && Time.time > _scheduledCheckTime)
            {
                _pendingCheck = false;

                var actualAudioSource = betterAudioSource.GetActualAudioSource();
                if (!actualAudioSource)
                {
                    Debug.LogError("No actual audio available", this);
                    betterAudioTestController.TestCompleted(false);
                    return;
                }

                if (!actualAudioSource.isPlaying)
                {
                    Debug.LogError("Actual audio source is not playing", this);
                    betterAudioTestController.TestCompleted(false);
                    return;
                }

                var playedForExpectedDuration = Mathf.Abs(actualAudioSource.time - _expectedAudioTime) <
                                                _maxDifference + Time.deltaTime;
                if (!playedForExpectedDuration)
                {
                    Debug.LogError(
                        $"Actual audio didn't play for the expected duration of {_expectedAudioTime}, is {actualAudioSource.time}",
                        this);
                    betterAudioTestController.TestCompleted(false);
                    return;
                }

                if (_secondPass)
                {
                    betterAudioTestController.TestCompleted(true);
                }
                else
                {
                    // test toggling the audio source and test once more
                    betterAudioSource.gameObject.SetActive(false);
                    betterAudioSource.gameObject.SetActive(true);

                    _secondPass = true;
                    RunTest();
                }
            }
        }


        private void CleanUpTest()
        {
            _pendingCheck = false;
            betterAudioTestController.TestCleanedUp(true);
        }

        #endregion
    }
}