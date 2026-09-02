using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonVoiceUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestPlayersReceiveMasterHeightChange), ExecutionOrder)]
    public class TestPlayersReceiveMasterHeightChange : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestMasterReceivesPlayerHeightChange.ExecutionOrder + 1;

        private float _initialHeight;
        private float _expectedHeight;
        private bool _isExpectingResponse;

        protected override void _InitializeTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_InitializeTest));
#endif
            #endregion

            _isExpectingResponse = false;
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

            LocalPlayer.SetAvatarEyeHeightByMultiplier(1f);
            SendCustomEventDelayedSeconds(nameof(_Delayed_FinishInitialization), 10f);
        }

        public override void OnAvatarChanged(VRCPlayerApi player) {
            base.OnAvatarChanged(player);
            _Info(
                    $"{player._DisplayNameUniqueSafe()}'s avatar changed: eye height is now {player.GetAvatarEyeHeightAsMeters()}m");
        }

        public void _Delayed_FinishInitialization() {
            _initialHeight = LocalPlayer.GetAvatarEyeHeightAsMeters();
            _expectedHeight = _initialHeight * 2f;

            TestController._TestInitialized(true);
        }

        protected override void _RunTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RunTest));
#endif
            #endregion

            _isExpectingResponse = true;
            LocalPlayer.SetAvatarEyeHeightByMeters(_expectedHeight);
            // wait for other player to respond after receiving height changes
        }

        [NetworkCallable(1)]
        public void _RPC_ReceivedHeightResponse(float previousEyeHeight, float newEyeHeight) {
            if (!LocalPlayer._IsMasterSafe()) {
                return;
            }

            if (Status != TestCaseStatus.Running) {
                return;
            }

            if (!_isExpectingResponse) {
                return;
            }
            
            if (_expectedHeight <= 0f) {
                _Error("Expected height was not set");
                TestController._TestCompleted(false);
                return;
            }

            if (Math.Abs(_initialHeight - previousEyeHeight) > 0.001f) {
                _Error(
                        $"Received height {previousEyeHeight}m from height change event did not match expected initial height {_initialHeight}m"
                );
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

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters) {
            base.OnAvatarEyeHeightChanged(player, prevEyeHeightAsMeters);
            if (Networking.LocalPlayer._IsMasterSafe()) {
                return;
            }
            
            // only remote player responds
            SendCustomNetworkEvent(
                    NetworkEventTarget.Others,
                    nameof(_RPC_ReceivedHeightResponse),
                    prevEyeHeightAsMeters,
                    player.GetAvatarEyeHeightAsMeters()
            );
        }
        

        protected override void _CleanUpTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_CleanUpTest));
#endif
            #endregion
            _isExpectingResponse = false;
            Networking.LocalPlayer.SetAvatarEyeHeightByMultiplier(1f);
            TestController._TestCleanedUp(true);
        }
    }
}