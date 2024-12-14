using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonVoiceUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestPlayersReceiveMasterHeightChange), ExecutionOrder)]
    public class TestPlayersReceiveMasterHeightChange : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestMasterReceivesPlayerHeightChange.ExecutionOrder + 1;

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

        private float _initialHeight;
        private float _expectedHeight;

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

            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            if (!Networking.IsOwner(gameObject)) {
                Error("Failed to transfer ownership to local player");
                TestController.TestInitialized(false);
                return;
            }

            Networking.LocalPlayer.SetAvatarEyeHeightByMultiplier(1f);
            _initialHeight = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
            _expectedHeight = _initialHeight * 2f;

            TestController.TestInitialized(true);
        }

        protected override void RunTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RunTest));
#endif
            #endregion

            TestIsRunning = true;
            ReceivedHeightEvent = 0f;
            ReceivedHeightAvatarApi = 0f;
            ExpectedPreviousEyeHeightAvatarApi = _initialHeight;
            ErrorMessage = "";
            MarkNetworkDirty();
            RequestSerialization();

            Networking.LocalPlayer.SetAvatarEyeHeightByMeters(_expectedHeight);

            // wait for other player to respond after receiving height changes
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);

            if (!TestIsRunning) {
                return;
            }

            if (!Networking.LocalPlayer.IsMasterSafe()) {
                SendCustomEventDelayedSeconds(nameof(GetAvatarHeightDelayedAndRespond), 10f);
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

            var playerApis = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(playerApis);

            var master = playerApis.GetMaster();
            if (Utilities.IsValid(master)) {
                ReceivedHeightAvatarApi = master.GetAvatarEyeHeightAsMeters();
                if (ErrorMessage != "") {
                    Error(ErrorMessage);
                }
            } else {
                ErrorMessage = "Master not found in list of players";
                Error(ErrorMessage);
            }

            MarkNetworkDirty();
        }

        protected override void CleanUpTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CleanUpTest));
#endif
            #endregion

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
            MarkNetworkDirty();
            RequestSerialization();

            Networking.LocalPlayer.SetAvatarEyeHeightByMultiplier(1f);

            TestController.TestCleanedUp(true);
        }
    }
}