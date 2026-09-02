using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonVoiceUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestMasterReceivesPlayerHeightChange), ExecutionOrder)]
    public class TestMasterReceivesPlayerHeightChange : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioVoiceFalloffTest.ExecutionOrder + 1;

        private float _initialHeight;
        private float _expectedHeight;
        private bool _isExpectingHeightChange;

        protected override void _InitializeTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_InitializeTest));
#endif
            #endregion

            _isExpectingHeightChange = false;
            _initialHeight = 0;
            _expectedHeight = 0;

            if (VRCPlayerApi.GetPlayerCount() != 2) {
                _Error("Need exactly 2 players to test this");
                TestController._TestInitialized(false);
                return;
            }

            if (!Networking.LocalPlayer._IsMasterSafe()) {
                _Error("Testing player must be master");
                TestController._TestInitialized(false);
                return;
            }

            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_RPC_NonMasterInit));
        }

        [NetworkCallable(1)]
        public void _RPC_NonMasterInit() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RPC_NonMasterInit));
#endif
            #endregion

            if (LocalPlayer._IsMasterSafe()) {
                return;
            }

            Networking.LocalPlayer.SetAvatarEyeHeightByMultiplier(1f);
            SendCustomEventDelayedSeconds(nameof(_Delayed_NonMasterInitComplete), 10);
        }

        public void _Delayed_NonMasterInitComplete() {
            if (LocalPlayer._IsMasterSafe()) {
                return;
            }

            _initialHeight = LocalPlayer.GetAvatarEyeHeightAsMeters();
            _expectedHeight = _initialHeight * 2f;
            SendCustomNetworkEvent(
                    NetworkEventTarget.Others,
                    nameof(_RPC_OnNonMasterInitCompleted),
                    _initialHeight,
                    _expectedHeight);
        }

        [NetworkCallable(1)]
        public void _RPC_OnNonMasterInitCompleted(float initialHeight, float expectedHeight) {
            if (!LocalPlayer._IsMasterSafe()) {
                return;
            }

            _initialHeight = initialHeight;
            _expectedHeight = expectedHeight;
            TestController._TestInitialized(true);
        }

        protected override void _RunTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RunTest));
#endif
            #endregion

            _isExpectingHeightChange = true;
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_RPC_NonMasterTest));
        }

        [NetworkCallable(1)]
        public void _RPC_NonMasterTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RPC_NonMasterTest));
#endif
            #endregion

            if (LocalPlayer._IsMasterSafe()) {
                return;
            }

            LocalPlayer.SetAvatarEyeHeightByMeters(_expectedHeight);
        }


        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters) {
            base.OnAvatarEyeHeightChanged(player, prevEyeHeightAsMeters);

            if (!_isExpectingHeightChange) return;

            if (!Networking.LocalPlayer._IsMasterSafe()) {
                return;
            }

            if (Status != TestCaseStatus.Running) return;

            if (Math.Abs(prevEyeHeightAsMeters - _initialHeight) > 0.001f) {
                _Error($"Expected previous eye height was not {_initialHeight}m, got {prevEyeHeightAsMeters}m");
                TestController._TestCompleted(false);
                return;
            }

            float newEyeHeight = player.GetAvatarEyeHeightAsMeters();

            if (_expectedHeight <= 0f) {
                _Error("Expected height was not set");
                TestController._TestCompleted(false);
                return;
            }

            if (Math.Abs(_expectedHeight - newEyeHeight) > 0.001f) {
                _Error(
                        $"Received height {newEyeHeight}m from height change event did not match expected height {_expectedHeight}m"
                );
                TestController._TestCompleted(false);
                return;
            }

            TestController._TestCompleted(true);
        }


        protected override void _CleanUpTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_CleanUpTest));
#endif
            #endregion

            _isExpectingHeightChange = false;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(_RPC_NonMasterCleanup));
            TestController._TestCleanedUp(true);
        }

        [NetworkCallable(1)]
        public void _RPC_NonMasterCleanup() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RPC_NonMasterCleanup));
#endif
            #endregion

            _initialHeight = 0;
            _expectedHeight = 0;
            LocalPlayer.SetAvatarEyeHeightByMultiplier(1f);
        }
    }
}