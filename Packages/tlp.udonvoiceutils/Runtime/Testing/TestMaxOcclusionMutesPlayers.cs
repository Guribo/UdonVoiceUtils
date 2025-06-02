using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Testing;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;


namespace TLP.UdonVoiceUtils.Runtime.Testing
{
    /// <summary>
    /// Tests whether maximum occlusion setting properly mutes players
    /// when they are separated by a wall or other obstacle.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestMaxOcclusionMutesPlayers), ExecutionOrder)]
    public class TestMaxOcclusionMutesPlayers : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestZoneEnteringNetworked.ExecutionOrder + 1;

        #region Constants
        /// <summary>
        /// When a player is fully muted, their voice distances should be below this threshold
        /// </summary>
        private const float MutedDistanceThreshold = 1e-3f;

        private const int RequiredPlayers = 2;

        /// <summary>
        /// It takes a few frames for the <see cref="PlayerAudioController"/> to update all players
        /// </summary>
        private const float CheckDelay = 5;
        #endregion

        #region Dependencies
        public Transform Player1Position;
        public Transform Player2Position;
        public BoxCollider WallCollider;
        #endregion

        #region State
        private PlayerAudioController _audioController;
        private Vector3 _initialPlayerPosition;
        private Quaternion _initialPlayerRotation;
        private VRCPlayerApi _otherPlayer;
        #endregion

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!AreAllDependenciesValid()) {
                return false;
            }

            ConfigureAudioControllerSettings();
            ConfigureWallCollider();
            return true;
        }

        protected override void InitializeTest() {
            if (HasRequiredPlayerCount()) {
                Error($"Test requires {RequiredPlayers} players to be in the instance");
                TestController.TestInitialized(false);
                return;
            }

            _otherPlayer = null;
            base.InitializeTest();
        }

        protected override void RunTest() {
            PositionPlayersForOcclusionTest();
        }

        protected override void CleanUpTest() {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RPC_Cleanup));
            base.CleanUpTest();
        }

        #region RPCs
        [NetworkCallable]
        public void RPC_PositionPlayers(Vector3 position, Quaternion rotation) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RPC_PositionPlayers));
#endif
            #endregion

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                Error("Local player invalid");
                return;
            }

            SaveInitialPlayerPosition(localPlayer);
            TeleportPlayer(position, rotation, localPlayer);

            if (IsRemoteNetworkCall(localPlayer)) {
                SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(RPC_TeleportConfirmation));
            }
        }

        [NetworkCallable]
        public void RPC_TeleportConfirmation() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RPC_TeleportConfirmation));
#endif
            #endregion

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                Error("Local player invalid");
                return;
            }

            if (IsLocalNetworkCall(localPlayer)) {
                Error($"Unexpected call to {nameof(RPC_TeleportConfirmation)} from local player");
                return;
            }

            _otherPlayer = NetworkCalling.CallingPlayer;
            ScheduleOcclusionCheck();
        }


        [NetworkCallable]
        public void RPC_Cleanup() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RPC_Cleanup));
#endif
            #endregion

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                Error("Local player invalid");
                return;
            }

            TeleportPlayer(_initialPlayerPosition, _initialPlayerRotation, localPlayer);
        }
        #endregion

        #region Delayed
        public void Delayed_CheckOcclusion() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Delayed_CheckOcclusion));
#endif
            #endregion

            if (Status != TestCaseStatus.Running) {
                Error($"{nameof(Delayed_CheckOcclusion)}: Test not running");
                return;
            }

            if (!HasRemotePlayerResponded()) {
                FailTest("Other player did not respond");
                return;
            }

            if (!IsPlayerMuted(_otherPlayer)) {
                FailTest($"Player is not muted correctly. Near: {_otherPlayer.GetVoiceDistanceNear()}, " +
                         $"Far: {_otherPlayer.GetVoiceDistanceFar()}");
                return;
            }

            Info("Test passed: Player behind the Wall is muted");
            TestController.TestCompleted(true);
        }
        #endregion

        #region Internal
        private bool IsPlayerMuted(VRCPlayerApi player) {
            return player.GetVoiceDistanceFar() <= MutedDistanceThreshold &&
                   player.GetVoiceDistanceNear() <= MutedDistanceThreshold;
        }


        private bool HasRemotePlayerResponded() {
            return Utilities.IsValid(_otherPlayer);
        }

        private void FailTest(string message) {
            Error(message);
            TestController.TestCompleted(false);
        }


        private void SaveInitialPlayerPosition(VRCPlayerApi localPlayer) {
            _initialPlayerPosition = localPlayer.GetPosition();
            _initialPlayerRotation = localPlayer.GetRotation();
        }

        private void ConfigureWallCollider() {
            WallCollider.enabled = true;
            WallCollider.gameObject.layer = LayerMask.NameToLayer("Environment");
            WallCollider.gameObject.SetActive(true);
        }

        private bool AreAllDependenciesValid() {
            bool isValid = true;

            if (!Player1Position) {
                Error($"{nameof(Player1Position)} is not set");
                isValid = false;
            }

            if (!Player2Position) {
                Error($"{nameof(Player2Position)} is not set");
                isValid = false;
            }

            if (!WallCollider) {
                Error($"{nameof(WallCollider)} is not set");
                isValid = false;
            }

            InitializeAudioController();
            if (!Utilities.IsValid(_audioController)) {
                Error($"{nameof(PlayerAudioController)} not found");
                isValid = false;
            }

            return isValid;
        }

        private void InitializeAudioController() {
            _audioController = VoiceUtils.FindPlayerAudioController();
        }

        private void ConfigureAudioControllerSettings() {
            _audioController.LocalConfiguration.ListenerDirectionality = 0;
            _audioController.LocalConfiguration.PlayerDirectionality = 0;
            _audioController.LocalConfiguration.OcclusionFactor = 1;
            _audioController.LocalConfiguration.Dirty = true;
            _audioController.LocalConfiguration.NotifyIfDirty();
        }

        private void PositionPlayersForOcclusionTest() {
            TeleportLocalPlayer(Player1Position.position, Player1Position.rotation);
            TeleportRemotePlayerForTest(Player2Position.position, Player2Position.rotation);
        }

        private void TeleportLocalPlayer(Vector3 position, Quaternion rotation) {
            RPC_PositionPlayers(position, rotation);
        }

        private void TeleportRemotePlayerForTest(Vector3 position, Quaternion rotation) {
            SendCustomNetworkEvent(
                    NetworkEventTarget.Others,
                    nameof(RPC_PositionPlayers),
                    position,
                    rotation);
        }

        private static void TeleportPlayer(Vector3 position, Quaternion rotation, VRCPlayerApi localPlayer) {
            localPlayer.TeleportTo(
                    position,
                    rotation,
                    VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                    false);
        }

        private static bool IsLocalNetworkCall(VRCPlayerApi localPlayer) {
            return NetworkCalling.InNetworkCall && NetworkCalling.CallingPlayer == localPlayer;
        }

        private static bool IsRemoteNetworkCall(VRCPlayerApi localPlayer) {
            return NetworkCalling.InNetworkCall && NetworkCalling.CallingPlayer != localPlayer;
        }

        private static bool HasRequiredPlayerCount() {
            return VRCPlayerApi.GetPlayerCount() != RequiredPlayers;
        }

        private void ScheduleOcclusionCheck() {
            SendCustomEventDelayedSeconds(nameof(Delayed_CheckOcclusion), CheckDelay);
        }
        #endregion
    }
}