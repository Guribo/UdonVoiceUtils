using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Testing;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
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

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_AreAllDependenciesValid()) {
                return false;
            }


            _ConfigureWallCollider();
            return true;
        }

        protected override void _InitializeTest() {
            if (_HasRequiredPlayerCount()) {
                _Error($"Test requires {RequiredPlayers} players to be in the instance");
                TestController._TestInitialized(false);
                return;
            }

            _BackupAudioControllerSettings();
            _ConfigureAudioControllerSettings();

            _otherPlayer = null;
            TestController._TestInitialized(true);
        }



        protected override void _RunTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RunTest));
#endif
            #endregion

            _PositionPlayersForOcclusionTest();
        }

        protected override void _CleanUpTest() {

            _RecoverAudioSettings();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(_RPC_Cleanup));
            base._CleanUpTest();
        }



        #region RPCs
        [NetworkCallable(1)]
        public void _RPC_PositionPlayers(Vector3 position, Quaternion rotation) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RPC_PositionPlayers));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!_IsRemoteNetworkCall(LocalPlayer)) {
                _Error($"Unexpected call to {nameof(_RPC_PositionPlayers)} from local player");
                return;
            }

            _PositionLocalPlayer(position, rotation);
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_RPC_TeleportConfirmation));
        }


        [NetworkCallable(1)]
        public void _RPC_TeleportConfirmation() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RPC_TeleportConfirmation));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (_IsLocalNetworkCall(LocalPlayer)) {
                _Error($"Unexpected call to {nameof(_RPC_TeleportConfirmation)} from local player");
                return;
            }

            _otherPlayer = NetworkCalling.CallingPlayer;
            _ScheduleOcclusionCheck();
        }


        [NetworkCallable(1)]
        public void _RPC_Cleanup() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RPC_Cleanup));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!Utilities.IsValid(NetworkCalling.CallingPlayer)) {
                _Error($"Unexpected call to {nameof(_RPC_Cleanup)} from local player");
                return;
            }

            _TeleportPlayer(_initialPlayerPosition, _initialPlayerRotation, LocalPlayer);
        }
        #endregion

        #region Delayed
        public void _Delayed_CheckOcclusion() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Delayed_CheckOcclusion));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (Status != TestCaseStatus.Running) {
                _Error($"{nameof(_Delayed_CheckOcclusion)}: Test not running");
                return;
            }

            if (!_HasRemotePlayerResponded()) {
                _FailTest("Other player did not respond");
                return;
            }

            if (!_IsPlayerMuted(_otherPlayer)) {
                _FailTest(
                        $"Player is not muted correctly. Near: {_otherPlayer.GetVoiceDistanceNear()}, " +
                        $"Far: {_otherPlayer.GetVoiceDistanceFar()}");
                return;
            }

            _Info("Test passed: Player behind the Wall is muted");
            TestController._TestCompleted(true);
        }
        #endregion

        #region Internal
        private bool _IsPlayerMuted(VRCPlayerApi player) {
            return player.GetVoiceDistanceFar() <= MutedDistanceThreshold &&
                   player.GetVoiceDistanceNear() <= MutedDistanceThreshold;
        }
        
        private bool _HasRemotePlayerResponded() {
            return Utilities.IsValid(_otherPlayer);
        }

        private void _FailTest(string message) {
            _Error(message);
            TestController._TestCompleted(false);
        }


        private void _SaveInitialPlayerPosition(VRCPlayerApi localPlayer) {
            _initialPlayerPosition = localPlayer.GetPosition();
            _initialPlayerRotation = localPlayer.GetRotation();
        }

        private void _ConfigureWallCollider() {
            WallCollider.enabled = true;
            WallCollider.gameObject.layer = LayerMask.NameToLayer("Environment");
            WallCollider.gameObject.SetActive(true);
        }

        private bool _AreAllDependenciesValid() {
            bool isValid = true;

            if (!Player1Position) {
                _Error($"{nameof(Player1Position)} is not set");
                isValid = false;
            }

            if (!Player2Position) {
                _Error($"{nameof(Player2Position)} is not set");
                isValid = false;
            }

            if (!WallCollider) {
                _Error($"{nameof(WallCollider)} is not set");
                isValid = false;
            }

            _InitializeAudioController();
            if (!Utilities.IsValid(_audioController)) {
                _Error($"{nameof(PlayerAudioController)} not found");
                isValid = false;
            }

            return isValid;
        }

        private void _InitializeAudioController() {
            _audioController = VoiceUtils._FindPlayerAudioController();
        }

        private void _ConfigureAudioControllerSettings() {
            _audioController.LocalConfiguration.ListenerDirectionality = 0;
            _audioController.LocalConfiguration.PlayerDirectionality = 0;
            _audioController.LocalConfiguration.OcclusionFactor = 1;
            _audioController.LocalConfiguration.Dirty = true;
            _audioController.LocalConfiguration._NotifyIfDirty();
        }

        private void _PositionPlayersForOcclusionTest() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_PositionPlayersForOcclusionTest));
#endif
            #endregion

            _PositionLocalPlayer(Player1Position.position, Player1Position.rotation);
            _TeleportRemotePlayerForTest(Player2Position.position, Player2Position.rotation);
        }

        private void _TeleportRemotePlayerForTest(Vector3 position, Quaternion rotation) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TeleportRemotePlayerForTest));
#endif
            #endregion

            SendCustomNetworkEvent(
                    NetworkEventTarget.Others,
                    nameof(_RPC_PositionPlayers),
                    position,
                    rotation);
        }

        private static void _TeleportPlayer(Vector3 position, Quaternion rotation, VRCPlayerApi localPlayer) {
            localPlayer.TeleportTo(
                    position,
                    rotation,
                    VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                    false);
        }

        private static bool _IsLocalNetworkCall(VRCPlayerApi localPlayer) {
            return NetworkCalling.InNetworkCall && NetworkCalling.CallingPlayer == localPlayer;
        }

        private static bool _IsRemoteNetworkCall(VRCPlayerApi localPlayer) {
            return NetworkCalling.InNetworkCall && NetworkCalling.CallingPlayer != localPlayer;
        }

        private static bool _HasRequiredPlayerCount() {
            return VRCPlayerApi.GetPlayerCount() != RequiredPlayers;
        }

        private void _ScheduleOcclusionCheck() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_ScheduleOcclusionCheck));
#endif
#endregion
            SendCustomEventDelayedSeconds(nameof(_Delayed_CheckOcclusion), CheckDelay);
        }

        private void _PositionLocalPlayer(Vector3 position, Quaternion rotation) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_PositionLocalPlayer));
#endif
            #endregion

            _SaveInitialPlayerPosition(LocalPlayer);
            _TeleportPlayer(position, rotation, LocalPlayer);
        }

        private void _BackupAudioControllerSettings() {
            _backupListenerDirectionality = _audioController.LocalConfiguration.ListenerDirectionality;
            _backupPlayerDirectionality = _audioController.LocalConfiguration.PlayerDirectionality;
            _backupOcclusionFactor = _audioController.LocalConfiguration.OcclusionFactor;
        }

        private void _RecoverAudioSettings() {
            if (!Utilities.IsValid(_audioController) || !Utilities.IsValid(_audioController.LocalConfiguration)) {
                _Error($"{nameof(PlayerAudioController)} or {nameof(PlayerAudioConfigurationModel)} not valid");
                return;
            }
            _audioController.LocalConfiguration.ListenerDirectionality = _backupListenerDirectionality;
            _audioController.LocalConfiguration.PlayerDirectionality = _backupPlayerDirectionality;
            _audioController.LocalConfiguration.OcclusionFactor = _backupOcclusionFactor;
        }
        #endregion
    }
}