using System;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonVoiceUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TestMasterReceivesPlayerHeightChange : TestCase
    {
        [UdonSynced]
        [HideInInspector]
        public bool TestIsRunning;

        [UdonSynced]
        [HideInInspector]
        public float ReceivedHeightEvent;

        [UdonSynced]
        [HideInInspector]
        public float ReceivedHeightAvatarApi;

        [UdonSynced]
        [HideInInspector]
        public float ExpectedPreviousEyeHeightAvatarApi;

        [UdonSynced]
        [HideInInspector]
        public string ErrorMessage;

        [UdonSynced]
        [HideInInspector]
        public bool InitCompleted;

        private float _initialHeight;
        private float _expectedHeight;
        private bool _initCompleted;

        protected override void InitializeTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InitializeTest));
#endif
            #endregion

            if (VRCPlayerApi.GetPlayerCount() != 2) {
                Error("Need exactly 2 players to test this");
                TestController.TestInitialized(false);
                return;
            }

            if (!Networking.LocalPlayer.IsMasterSafe()) {
                Error("Testing player must be master");
                TestController.TestInitialized(false);
                return;
            }

            _initCompleted = false;
            InitCompleted = false;

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(NonMasterInit));
        }

        public void NonMasterInit() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(NonMasterInit));
#endif
            #endregion

            if (Networking.LocalPlayer.IsMasterSafe()) {
                return;
            }

            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            if (!Networking.IsOwner(gameObject)) {
                ErrorMessage = "Failed to transfer ownership to local player";
                Error(ErrorMessage);
                return;
            }

            Networking.LocalPlayer.SetAvatarEyeHeightByMultiplier(1f);
            _initialHeight = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
            _expectedHeight = _initialHeight * 2f;

            InitCompleted = true;
            MarkNetworkDirty();
            RequestSerialization();
        }

        protected override void RunTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RunTest));
#endif
            #endregion

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(NonMasterTest));
        }

        public void NonMasterTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(NonMasterTest));
#endif
            #endregion

            if (Networking.LocalPlayer.IsMasterSafe()) {
                return;
            }

            TestIsRunning = true;
            ReceivedHeightEvent = 0f;
            ReceivedHeightAvatarApi = 0f;
            ExpectedPreviousEyeHeightAvatarApi = _initialHeight;
            ErrorMessage = "";
            MarkNetworkDirty();
            RequestSerialization();

            Networking.LocalPlayer.SetAvatarEyeHeightByMeters(_expectedHeight);

            // wait for master to respond after receiving height changes
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);

            if (Networking.LocalPlayer.IsMasterSafe() && InitCompleted && !_initCompleted) {
                _initCompleted = true;

                if (ErrorMessage != "") {
                    Error(ErrorMessage);
                    TestController.TestInitialized(false);
                    return;
                }

                TestController.TestInitialized(true);
            }


            if (!TestIsRunning) {
                return;
            }

            if (Networking.LocalPlayer.IsMasterSafe()) {
                SendCustomEventDelayedSeconds(nameof(GetAvatarHeightDelayedAndRespond), 10f);
                return;
            }

            if (!Networking.LocalPlayer.IsMasterSafe()) {
                return;
            }

            if (ErrorMessage != "") {
                Error($"Received from client: {ErrorMessage}");
                TestController.TestCompleted(false);
            }

            if (_expectedHeight <= 0f) {
                Error("Expected height was not set");
                TestController.TestCompleted(false);
                return;
            }

            if (Math.Abs(_expectedHeight - ReceivedHeightEvent) > 0.001f) {
                Error(
                        $"Received height {ReceivedHeightEvent}m from height change event did not match expected height {_expectedHeight}m"
                );
                TestController.TestCompleted(false);
                return;
            }

            if (Math.Abs(_expectedHeight - ReceivedHeightAvatarApi) > 0.001f) {
                Error(
                        $"Received height {ReceivedHeightAvatarApi}m from VrcPlayerApi did not match expected height {_expectedHeight}m"
                );
                TestController.TestCompleted(false);
                return;
            }

            TestController.TestCompleted(true);
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters) {
            base.OnAvatarEyeHeightChanged(player, prevEyeHeightAsMeters);
            if (!TestIsRunning) {
                return;
            }

            if (Networking.LocalPlayer.IsMasterSafe()) {
                return;
            }

            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            ErrorMessage = "";

            if (ExpectedPreviousEyeHeightAvatarApi <= 0.199f) // minimum height allowed by VRChat is 0.2f
            {
                ErrorMessage =
                        $"Expected previous eye height was not set correctly, expected at least 0.2m but got {ExpectedPreviousEyeHeightAvatarApi}m";
                return;
            }

            if (Math.Abs(prevEyeHeightAsMeters - ExpectedPreviousEyeHeightAvatarApi) > 0.001f) {
                ErrorMessage =
                        $"Expected previous eye height was not {ExpectedPreviousEyeHeightAvatarApi}m, got {prevEyeHeightAsMeters}m";
                return;
            }

            ReceivedHeightEvent = player.GetAvatarEyeHeightAsMeters();
        }

        public void GetAvatarHeightDelayedAndRespond() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(GetAvatarHeightDelayedAndRespond));
#endif
            #endregion

            if (!Networking.LocalPlayer.IsMasterSafe()) {
                return;
            }

            ReceivedHeightAvatarApi = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
            if (ErrorMessage != "") {
                Error(ErrorMessage);
                TestController.TestCompleted(false);
                return;
            }

            TestController.TestCompleted(true);
        }

        protected override void CleanUpTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CleanUpTest));
#endif
            #endregion


            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(NonMasterCleanup));
            TestController.TestCleanedUp(true);
        }

        public void NonMasterCleanup() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(NonMasterCleanup));
#endif
            #endregion

            if (Networking.LocalPlayer.IsMasterSafe()) {
                return;
            }

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (!Networking.IsOwner(gameObject)) {
                Error("Failed to transfer ownership to local player");
                TestController.TestCleanedUp(false);
                return;
            }

            TestIsRunning = false;
            ReceivedHeightEvent = 0f;
            ReceivedHeightAvatarApi = 0f;
            ExpectedPreviousEyeHeightAvatarApi = 0f;
            ErrorMessage = "";
            InitCompleted = false;
            MarkNetworkDirty();
            RequestSerialization();

            Networking.LocalPlayer.SetAvatarEyeHeightByMultiplier(1f);
        }
    }
}