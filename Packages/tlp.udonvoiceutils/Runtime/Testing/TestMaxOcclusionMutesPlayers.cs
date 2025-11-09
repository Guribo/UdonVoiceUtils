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
        private float _backupListenerDirectionality;
        private float _backupPlayerDirectionality;
        private float _backupOcclusionFactor;
        #endregion

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!AreAllDependenciesValid()) {
                return false;
            }


            ConfigureWallCollider();
            return true;
        }

        protected override void InitializeTest() {
            if (HasRequiredPlayerCount()) {
                Error($"Test requires {RequiredPlayers} players to be in the instance");
                TestController.TestInitialized(false);
                return;
            }

            BackupAudioControllerSettings();
            ConfigureAudioControllerSettings();

            _otherPlayer = null;
            TestController.TestInitialized(true);
        }



        protected override void RunTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RunTest));
#endif
            #endregion

            PositionPlayersForOcclusionTest();
        }

        protected override void CleanUpTest() {

            RecoverAudioSettings();
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

            if (!HasStartedOk) {
                return;
            }

            if (!IsRemoteNetworkCall(LocalPlayer)) {
                Error($"Unexpected call to {nameof(RPC_PositionPlayers)} from local player");
                return;
            }

            PositionLocalPlayer(position, rotation);
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(RPC_TeleportConfirmation));
        }


        [NetworkCallable]
        public void RPC_TeleportConfirmation() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RPC_TeleportConfirmation));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (IsLocalNetworkCall(LocalPlayer)) {
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

            if (!HasStartedOk) {
                return;
            }

            if (!Utilities.IsValid(NetworkCalling.CallingPlayer)) {
                Error($"Unexpected call to {nameof(RPC_Cleanup)} from local player");
                return;
            }

            TeleportPlayer(_initialPlayerPosition, _initialPlayerRotation, LocalPlayer);
        }
        #endregion

        #region Delayed
        public void Delayed_CheckOcclusion() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Delayed_CheckOcclusion));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (Status != TestCaseStatus.Running) {
                Error($"{nameof(Delayed_CheckOcclusion)}: Test not running");
                return;
            }

            if (!HasRemotePlayerResponded()) {
                FailTest("Other player did not respond");
                return;
            }

            if (!IsPlayerMuted(_otherPlayer)) {
                FailTest(
                        $"Player is not muted correctly. Near: {_otherPlayer.GetVoiceDistanceNear()}, " +
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
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(PositionPlayersForOcclusionTest));
#endif
            #endregion

            PositionLocalPlayer(Player1Position.position, Player1Position.rotation);
            TeleportRemotePlayerForTest(Player2Position.position, Player2Position.rotation);
        }

        private void TeleportRemotePlayerForTest(Vector3 position, Quaternion rotation) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TeleportRemotePlayerForTest));
#endif
            #endregion

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
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(ScheduleOcclusionCheck));
#endif
#endregion
            SendCustomEventDelayedSeconds(nameof(Delayed_CheckOcclusion), CheckDelay);
        }

        private void PositionLocalPlayer(Vector3 position, Quaternion rotation) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(PositionLocalPlayer));
#endif
            #endregion

            SaveInitialPlayerPosition(LocalPlayer);
            TeleportPlayer(position, rotation, LocalPlayer);
        }

        private void BackupAudioControllerSettings() {
            _backupListenerDirectionality = _audioController.LocalConfiguration.ListenerDirectionality;
            _backupPlayerDirectionality = _audioController.LocalConfiguration.PlayerDirectionality;
            _backupOcclusionFactor = _audioController.LocalConfiguration.OcclusionFactor;
        }

        private void RecoverAudioSettings() {
            if (!Utilities.IsValid(_audioController) || !Utilities.IsValid(_audioController.LocalConfiguration)) {
                Error($"{nameof(PlayerAudioController)} or {nameof(PlayerAudioConfigurationModel)} not valid");
                return;
            }
            _audioController.LocalConfiguration.ListenerDirectionality = _backupListenerDirectionality;
            _audioController.LocalConfiguration.PlayerDirectionality = _backupPlayerDirectionality;
            _audioController.LocalConfiguration.OcclusionFactor = _backupOcclusionFactor;
        }
        #endregion
    }
}