using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sources;
using TLP.UdonUtils.Runtime.Sources.Time;
using TLP.UdonUtils.Runtime.Testing;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonVoiceUtils.Runtime.Testing
{
    /// <summary>
    /// Component which implements the base of a test case, includes preparation, execution and cleanup methods
    /// to be copied to new test scripts and filled for each individual test case.
    /// 
    /// Behaviour sync mode can be changed depending on the test performed, default is no variable sync
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestZoneEnteringNetworked), ExecutionOrder)]
    public class TestZoneEnteringNetworked : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestPlayersReceiveMasterHeightChange.ExecutionOrder + 1;

        [SerializeField]
        private Transform Enter;

        [SerializeField]
        private Transform Exit;

        [SerializeField]
        private VoiceOverrideRoom VoiceOverrideRoom;

        [SerializeField]
        private VoiceOverrideRoom[] AllRooms;

        private int _initialPlayerCount;

        #region State
        private TimeSource _networkTime;
        #endregion

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            _networkTime = TlpNetworkTime._GetInstance();
            return _IsSet(_networkTime, nameof(_networkTime));
        }

        protected override void _InitializeTest() {
            if (!HasStartedOk) {
                TestController._TestInitialized(false);
                return;
            }

            _initialPlayerCount = VRCPlayerApi.GetPlayerCount();

            foreach (var room in AllRooms) {
                if (!Utilities.IsValid(room)) {
                    _Error($"{nameof(AllRooms)} contains invalid room");
                    TestController._TestInitialized(false);
                    return;
                }
            }

            SendCustomNetworkEvent(
                    NetworkEventTarget.All,
                    nameof(_RPC_PrepareLocalPlayer),
                    _networkTime._TimeAsDouble() + 10);

            TestController._TestInitialized(true);
        }

        #region RPCs
        
        [NetworkCallable(1)]
        public void _RPC_PrepareLocalPlayer(double teleportTime) {
            double networkTime = _networkTime._TimeAsDouble();
            if (teleportTime < networkTime) {
                _Warn(
                        $"Received outdated teleport time: {teleportTime} (received time) < {networkTime} (current time)");
                return;
            }

            foreach (var overrideRoom in AllRooms) {
                if (!Utilities.IsValid(overrideRoom)) {
                    continue;
                }

                if (overrideRoom._Contains(Networking.LocalPlayer)) {
                    overrideRoom._ExitRoom(Networking.LocalPlayer, null);
                }
            }

            _EnsurePlayerAudioCleanup();
            SendCustomEventDelayedSeconds(nameof(_EnterDelayed), (float)(teleportTime - networkTime));
        }
        
        [NetworkCallable(1)]
        public void _RPC_LeaveZone() {
            VoiceOverrideRoom._ExitRoom(Networking.LocalPlayer, Exit);
            Networking.LocalPlayer.Immobilize(false);
        }
        
        #endregion

        private void _EnsurePlayerAudioCleanup() {
            LocalPlayer.Immobilize(true);

            int limit = 1000;
            int i = 0;
            for (; i < limit; i++) {
                if (VoiceOverrideRoom.PlayerAudioOverride.PlayerAudioController._GetMaxPriorityOverride(
                            Networking.LocalPlayer,
                            out var playerAudioOverride
                    )) {
                    playerAudioOverride._RemovePlayer(Networking.LocalPlayer);
                    continue;
                }

                break;
            }

            if (i != limit) {
                return;
            }

            _Error($"{nameof(_RPC_PrepareLocalPlayer)}: Failed to remove player from all rooms: infinite loop?");
            TestController._TestInitialized(false); // only relevant on the player who started the test
        }

        public void _EnterDelayed() {
            VoiceOverrideRoom._EnterRoom(Networking.LocalPlayer, Enter);
        }

        protected override void _RunTest() {
            SendCustomEventDelayedSeconds(nameof(_CheckDelayed), 70f);
        }

        public void _CheckDelayed() {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            var players = new VRCPlayerApi[playerCount];
            VRCPlayerApi.GetPlayers(players);

            int successCount = 0;
            foreach (var vrcPlayerApi in players) {
                if (!Utilities.IsValid(vrcPlayerApi)) {
                    continue;
                }

                VoiceOverrideRoom.PlayerAudioOverride.PlayerAudioController
                        ._GetMaxPriorityOverride(vrcPlayerApi, out var audioOverride);
                successCount += ReferenceEquals(audioOverride, VoiceOverrideRoom.PlayerAudioOverride) ? 1 : 0;
            }

            _DebugLog($"successCount == _initialPlayerCount? : {successCount} == {_initialPlayerCount}");

            TestController._TestCompleted(successCount == _initialPlayerCount);
        }

        protected override void _CleanUpTest() {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(_RPC_LeaveZone));
            TestController._TestCleanedUp(true);
        }
    }
}