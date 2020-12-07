using UdonSharp;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Scripts.Tests
{
    public class TestTemplate : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [HideInInspector] public BetterAudioTestController betterAudioTestController;

        public void Initialize()
        {
            if (!betterAudioTestController)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller", this);
                return;
            }
            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!betterAudioTestController)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller", this);
                return;
            }

            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!betterAudioTestController)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller", this);
                return;
            }
            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.CleanUp", this);
            CleanUpTest();
        }

        #endregion

        #region EDIT HERE

        private void InitializeTest()
        {
            // TODO your init behaviour here
            // ...

            // whenever the test is ready to be started call betterAudioTestController.TestInitialized,
            // can be later in update or whenever but MUST be called at some point
            betterAudioTestController.TestInitialized(true);
        }

        private void RunTest()
        {
            // TODO your test behaviour here
            // ...

            // whenever the test is completed call betterAudioTestController.TestCompleted,
            // can be later in update or whenever but MUST be called at some point
            betterAudioTestController.TestCompleted(true);
        }

        private void CleanUpTest()
        {
            // TODO your clean up behaviour here
            // ...

            // whenever the test is cleaned up call betterAudioTestController.TestCleanedUp,
            // can be later in update or whenever but MUST be called at some point
            betterAudioTestController.TestCleanedUp(true);
        }

        #endregion
    }
}
