using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Pool;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio.PlayerOcclusion;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDK3.Rendering;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio
{
    [RequireComponent(typeof(DummyView))]
    [TlpDefaultExecutionOrder(typeof(PlayerAudioController), ExecutionOrder)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlayerAudioController : Controller
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioOverrideList.ExecutionOrder + 1;

        #region General settings
        [Header("General settings")]
        public const int ChannelNoPrivacy = -1;

        private const float MaxRelativeAudioRange = 1f;
        private const float SmallNumber = 0.001f;
        private const float MinRelativeAudioRange = 0f;

        /// <summary>
        /// How many player updates should be performed every frame.
        /// Controls how responsive the system is.
        ///
        /// If set to 0 it will update ALL PLAYERS EVERY FRAME!
        /// Don't use this option in worlds with many people as this can have a serious performance impact!
        ///
        /// Example 1:
        /// With 80 players and a value of 1 it takes 80 frames to update every player.
        /// If the game is rendering 20 fps it takes (80 / 1) / 20 = 4 seconds to update every player.
        ///
        /// Example 2:
        /// With 80 players and a value of 4 it takes 20 frames to update every player.
        /// If the game is rendering 20 fps it takes (80 / 4) / 20 = 1 second to update every player.
        ///
        /// // Example 3:
        /// With 80 players and a value of 80 it takes 1 frame to update every player.
        /// If the game is rendering 20 fps it takes (80 / 80) / 20 = 0.05 seconds to update every player.
        ///
        /// </summary>
        [Tooltip(
                "How many player updates should be performed every frame. " +
                "If set to 0 it will update ALL PLAYERS EVERY FRAME!" +
                "Don't use this option in worlds with many people as this can have a serious performance impact!"
        )]
        [SerializeField]
        protected int PlayerUpdateRate = 1;
        #endregion

        #region Mandatory references
        [Header("Mandatory references")]
        public PlayerAudioOverrideList LocalPlayerOverrideList;

        [SerializeField]
        internal AudioReverbFilter MainAudioReverbFilter;

        [SerializeField]
        internal Pool PlayerOverrideListPool;

        [SerializeField]
        internal PlayerOcclusionStrategy PlayerOcclusionStrategy;

        [SerializeField]
        internal IgnoredPlayers IgnoredPlayers;
        #endregion

        #region Optional
        [FormerlySerializedAs("OptionalView")]
        [SerializeField]
        internal View Menu;
        #endregion

        public PlayerAudioConfigurationModel LocalConfiguration;
        public PlayerAudioConfigurationModel SyncedMasterConfiguration;
        public PlayerAudioConfigurationModel DefaultConfiguration;
        internal PlayerAudioConfigurationModel _currentConfiguration;

        /// <summary>
        /// Exposes the currently active audio configuration (local or master-synced).
        /// </summary>
        public PlayerAudioConfigurationModel CurrentConfiguration => _currentConfiguration;

        /// <summary>
        /// Exposes the local player's current audio override, if any.
        /// </summary>
        public PlayerAudioOverride CurrentLocalOverride => _localOverride;

        #region State
        internal readonly DataList ExistingRemotePlayerIds = new DataList();


        /// <summary>
        /// Map of playerIDs to PlayerAudioOverrideList
        /// </summary>
        internal readonly DataDictionary PlayersToOverride = new DataDictionary();

        private bool _receivedStart;
        private int _playerIndex;

        private PlayerAudioOverride _localOverride;
        #endregion

        #region Unity Lifecycle
        public void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(OnEnable)}: Not initialized");
            }
        }

        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDisable));
#endif
            #endregion

            _UseReverbSettings(null);
            _ResetAllPlayerVoices();

            if (!HasStartedOk) {
                _Warn($"{nameof(OnDisable)}: Not initialized, nothing to do");
                HasReceivedStart = false; // allow re-start
                return;
            }

            HasStartedOk = false;

            if (Menu.HasStartedOk
                && Menu.IsViewInitialized
                && !Menu._DeInitialize()) {
                _Error($"{nameof(OnDisable)}: {nameof(_DeInitialize)} of {nameof(Menu)} failed");
            }

            if (!_DeInitialize()) {
                _Error($"{nameof(OnDisable)} {nameof(_DeInitialize)} of {GetUdonTypeName()} failed");
            }

            if (LocalConfiguration.HasStartedOk
                && LocalConfiguration.IsModelInitialized
                && !LocalConfiguration._DeInitialize()) {
                _Error($"DeInitialization of {nameof(LocalConfiguration)} failed");
            }
        }

        public override void PostLateUpdate() {
            if (!HasStartedOk || !isActiveAndEnabled) {
                // !isActiveAndEnabled is needed so that PostLateUpdate doesn't run after OnDisable
                return;
            }

            LocalPlayerOverrideList._GetMaxPriority(LocalPlayer, out var localPlayerOverride);
            _localOverride = _UpdateAudioFilters(localPlayerOverride, _localOverride);
            for (int playerUpdate = 0;
                 playerUpdate < _GetPendingPlayerUpdates(ExistingRemotePlayerIds.Count);
                 ++playerUpdate) {
                _UpdateNextPlayer(ref playerUpdate);
            }
        }

        protected override void OnDestroy() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDestroy));
#endif
            #endregion

            _UseReverbSettings(null);
            _ResetAllPlayerVoices();
        }
        #endregion


        #region Public API
        /// <summary>
        /// Add a player that shall not be affected by any effect of this script. Upon adding the player
        /// all values of the player will be set to the currently defined values on this script.
        /// Occlusion and directionality effects are reverted if the player was affected.
        /// This function is local only.
        /// </summary>
        /// <param name="playerToIgnore"></param>
        /// <returns>true on success, false if argument invalid or player doesn't exist or is already ignored</returns>
        public bool _IgnorePlayer(VRCPlayerApi playerToIgnore) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_IgnorePlayer)} {playerToIgnore._ToStringSafe()}");
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(_IgnorePlayer)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                _Error($"{nameof(_IgnorePlayer)}: MVC not initialized");
                return false;
            }

            return IgnoredPlayers._Add(playerToIgnore);
        }

        /// <summary>
        /// Remove a player from the ignore list and let it be affected again by this script.
        /// This function is local only.
        /// </summary>
        /// <param name="ignoredPlayer"></param>
        /// <returns>false if player was invalid or not ignored, true if the player was removed from the ignore list</returns>
        public bool _UnIgnorePlayer(VRCPlayerApi ignoredPlayer) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_UnIgnorePlayer)} {ignoredPlayer._ToStringSafe()}");
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(_UnIgnorePlayer)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                _Error($"{nameof(_UnIgnorePlayer)}: MVC not initialized");
                return false;
            }

            return IgnoredPlayers._Remove(ignoredPlayer);
        }

        public bool _OverridePlayerSettings(
                PlayerAudioOverride playerAudioOverride,
                VRCPlayerApi playerToAffect
        ) {
#if TLP_DEBUG
            _DebugLog(nameof(_OverridePlayerSettings));
#endif
            if (!HasStartedOk) {
                _Error($"{nameof(_OverridePlayerSettings)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                _Error($"{nameof(_OverridePlayerSettings)}: MVC not initialized");
                return false;
            }

            if (!Utilities.IsValid(playerAudioOverride)) {
                _Error($"{nameof(playerAudioOverride)} invalid");
                return false;
            }

            if (!Utilities.IsValid(playerToAffect)) {
                _Error($"{nameof(playerToAffect)} invalid");
                return false;
            }

            if (playerToAffect.isLocal) {
                return LocalPlayerOverrideList._AddOverride(playerAudioOverride);
            }

            // check if the player already has an override
            if (PlayersToOverride.TryGetValue(playerToAffect.playerId, out var value)) {
                var existingList = (PlayerAudioOverrideList)value.Reference;
                if (Utilities.IsValid(existingList)) {
                    return _AddOverrideToExistingList(playerAudioOverride, playerToAffect, existingList);
                }
            }

            return _AddOverrideToNewList(playerAudioOverride, playerToAffect);
        }

        public bool _ClearPlayerOverride(PlayerAudioOverride playerAudioOverride, VRCPlayerApi playerToClear) {
#if TLP_DEBUG
            _DebugLog(nameof(_ClearPlayerOverride));
#endif
            if (!HasStartedOk) {
                _Error($"{nameof(_ClearPlayerOverride)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                _Error($"{nameof(_ClearPlayerOverride)}: MVC not initialized");
                return false;
            }

            if (!Utilities.IsValid(playerToClear)) {
                _Error($"{nameof(playerToClear)} invalid");
                return false;
            }

            if (!Utilities.IsValid(playerAudioOverride)) {
                _Error($"{nameof(playerAudioOverride)} invalid");
                return false;
            }

            if (playerToClear.isLocal) {
                if (LocalPlayerOverrideList._Contains(playerAudioOverride)) {
                    LocalPlayerOverrideList._RemoveOverride(playerAudioOverride);
                    return true;
                }

                _DebugLog(
                        $"{playerToClear._ToStringSafe()} didn't have the local override {playerAudioOverride._GetScriptPathInScene()}");
                return false;
            }

            if (!PlayersToOverride.TryGetValue(playerToClear.playerId, out var list)) {
                _Warn(
                        $"{playerToClear._ToStringSafe()} doesn't have any overrides");
                return false;
            }

            var overrideList = (PlayerAudioOverrideList)list.Reference;
            if (Utilities.IsValid(overrideList) && overrideList._Contains(playerAudioOverride)) {
                overrideList._RemoveOverride(playerAudioOverride);
                return true;
            }

            _Warn(
                    $"{playerToClear._ToStringSafe()} didn't have the override {playerAudioOverride._GetScriptPathInScene()}");
            return false;
        }

        /// <param name="player"></param>
        /// <returns>null on error or if no overrides present</returns>
        public bool _GetMaxPriorityOverride(VRCPlayerApi player, out PlayerAudioOverride playerAudioOverride) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_GetMaxPriorityOverride)} {player._ToStringSafe()}");
#endif
            #endregion

            if (!IsReceivingStart && !HasStartedOk) {
                _Error($"{nameof(_GetMaxPriorityOverride)}: Not initialized");
                playerAudioOverride = null;
                return false;
            }

            if (!IsControllerInitialized) {
                _Error($"{nameof(_GetMaxPriorityOverride)}: MVC not initialized");
                playerAudioOverride = null;
                return false;
            }

            if (!Utilities.IsValid(player)) {
                _Error($"{nameof(player)} invalid");
                playerAudioOverride = null;
                return false;
            }

            if (player.isLocal) {
                return LocalPlayerOverrideList._GetMaxPriority(player, out playerAudioOverride);
            }

            if (!PlayersToOverride.TryGetValue(player.playerId, out var list)) {
                playerAudioOverride = null;
                return false;
            }

            var overrideList = (PlayerAudioOverrideList)list.Reference;
            if (Utilities.IsValid(overrideList)) {
                return overrideList._GetMaxPriority(player, out playerAudioOverride);
            }

            playerAudioOverride = null;
            return false;
        }
        #endregion

        #region OnPlayerLeft
        private void _PurgeOverridesOfPlayer(int playerId) {
            if (!PlayersToOverride.TryGetValue(playerId, out var list)) {
                return;
            }

            var overrideList = (PlayerAudioOverrideList)list.Reference;
            if (Utilities.IsValid(overrideList)) {
                PlayerOverrideListPool._Return(overrideList.gameObject);
            }

            PlayersToOverride.Remove(playerId);
        }

        private void _PurgeOverridesOfInvalidPlayers() {
            var playerIds = PlayersToOverride.GetKeys();
            for (int i = 0; i < playerIds.Count; i++) {
                var entry = playerIds[i];
                int playerId = entry.Int;
                if (!playerId._IsValidPlayer(out var unused)) {
                    _PurgeOverridesOfPlayer(playerId);
                }
            }
        }
        #endregion

        private void _UpdateNextPlayer(ref int playerUpdate) {
            _playerIndex._MoveIndexRightLooping(ExistingRemotePlayerIds.Count);
            int playerId = ExistingRemotePlayerIds[_playerIndex].Int;

            if (!playerId._IsValidPlayer(out var player)) {
                // this is not supposed to happen as Players should be removed by OnPlayerLeft before they become invalid
                _Warn(
                        $"{nameof(PostLateUpdate)}: Player {playerId} at position " +
                        $"{_playerIndex} has unexpectedly become invalid, deleting entry");

                // remove invalid entry
                ExistingRemotePlayerIds.RemoveAt(_playerIndex);

                // ensure that we don't skip an entry in the next iteration after deleting the invalid one
                _playerIndex._MoveIndexLeftLooping(ExistingRemotePlayerIds.Count);
                --playerUpdate;
                return;
            }

            if (IgnoredPlayers._Contains(playerId)) {
                return;
            }

            _UpdateOtherPlayer(player);
        }

        private bool _ValidateAndSetupMvc() {
            if (!LocalConfiguration.IsModelInitialized &&
                !LocalConfiguration._Initialize(LocalConfiguration.gameObject.GetComponent<UdonEvent>())) {
                _ErrorAndDisableComponent($"Failed to initialize {nameof(LocalConfiguration)}");
                return false;
            }

            if (SyncedMasterConfiguration.transform.childCount != 1) {
                _ErrorAndDisableComponent(
                        $"Expected the {nameof(SyncedMasterConfiguration)} to have one child with the {nameof(UdonEvent)}"
                );
                return false;
            }

            var child = SyncedMasterConfiguration.transform.GetChild(0);
            if (!child) {
                _ErrorAndDisableComponent(
                        $"Expected the {nameof(SyncedMasterConfiguration)} to have one child with the {nameof(UdonEvent)}"
                );
                return false;
            }

            var masterConfigChangeEvent = child.gameObject.GetComponent<UdonEvent>();
            if (!masterConfigChangeEvent) {
                _ErrorAndDisableComponent(
                        $"Expected the {nameof(SyncedMasterConfiguration)} to have one child with the {nameof(UdonEvent)}"
                );
                return false;
            }

            if (!SyncedMasterConfiguration.IsModelInitialized &&
                !SyncedMasterConfiguration._Initialize(masterConfigChangeEvent)) {
                _ErrorAndDisableComponent($"Failed to initialize {nameof(SyncedMasterConfiguration)}");
                return false;
            }

            if (!_AddConfigurationListeners()) {
                return false;
            }

            if (!Utilities.IsValid(Menu)) {
                Menu = GetComponent<DummyView>();
            }

            _currentConfiguration = LocalConfiguration;
            if (!_Initialize(_currentConfiguration, Menu)) {
                _ErrorAndDisableComponent($"Init of {GetUdonTypeName()} failed");
                return false;
            }

            return _InitializeMenu();
        }

        private bool _AddConfigurationListeners() {
            if (!LocalConfiguration.ChangeEvent._AddListenerVerified(this, nameof(_OnModelChanged))) {
                _ErrorAndDisableComponent("Failed to listen to local configuration");
                return false;
            }

            if (!SyncedMasterConfiguration.ChangeEvent._AddListenerVerified(this, nameof(_OnModelChanged))) {
                _ErrorAndDisableComponent("Failed to listen to master configuration");
                return false;
            }

            return true;
        }

        private bool _InitializeMenu() {
            if (!Utilities.IsValid(Menu) || Menu.IsViewInitialized) {
                _ErrorAndDisableComponent($"{nameof(Menu)} is already initialized");
                return false;
            }

            if (Menu._Initialize(this, _currentConfiguration)) {
                return true;
            }

            _ErrorAndDisableComponent($"Init of {nameof(Menu)} failed");
            return false;
        }


        internal bool _GetLocalPlayerAudioListenerTransform(out Vector3 position, out Quaternion rotation) {
#if TLP_UNIT_TESTING
            _GetRemotePlayerAudioListenerTransform(LocalPlayer, out position, out rotation);
            return true;
#else
            var screenCamera = VRCCameraSettings.ScreenCamera;

            if (!Utilities.IsValid(screenCamera) || !screenCamera.Active) {
                _Error(
                        $"{nameof(_GetLocalPlayerAudioListenerTransform)}: {nameof(VRCCameraSettings.ScreenCamera)} " +
                        $"is unexpectedly invalid or not active)");
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            position = screenCamera.Position;
            rotation = screenCamera.Rotation;
            return true;
#endif
        }

        public static void _GetRemotePlayerAudioListenerTransform(
                VRCPlayerApi otherPlayer,
                out Vector3 position,
                out Quaternion rotation
        ) {
            var otherPlayerHead = otherPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            bool isOtherPlayerHumanoid = otherPlayerHead.position.sqrMagnitude > SmallNumber;
            if (isOtherPlayerHumanoid) {
                position = otherPlayerHead.position;
                rotation = otherPlayerHead.rotation;
                return;
            }

            // create a fake head position/rotation (no pitch and roll)
            var avatarRootRotation = otherPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).rotation;
            var playerUp = avatarRootRotation * Vector3.up;

            position = otherPlayer.GetPosition() + playerUp * otherPlayer.GetAvatarEyeHeightAsMeters();
            rotation = avatarRootRotation;
        }


        internal void _UpdateOtherPlayer(VRCPlayerApi otherPlayer) {
            if (!_GetLocalPlayerAudioListenerTransform(out var listenerHeadPosition, out var listenerHeadRotation)) {
                return;
            }

            _GetRemotePlayerAudioListenerTransform(otherPlayer, out var otherHeadPosition, out var otherHeadRotation);

            if (_GetMaxPriorityOverride(otherPlayer, out var playerOverride)) {
                bool localPlayerHasOverride = Utilities.IsValid(_localOverride) && _localOverride.HasStartedOk;
                if (_OtherPlayerWithOverrideCanBeHeard(
                            playerOverride,
                            _localOverride,
                            localPlayerHasOverride && _localOverride.MuteOutsiders,
                            localPlayerHasOverride && _localOverride.DisallowListeningToChannel
                    )) {
                    _ApplyAudioOverrides(
                            otherPlayer,
                            listenerHeadPosition,
                            listenerHeadRotation,
                            otherHeadPosition - listenerHeadPosition,
                            playerOverride,
                            otherHeadRotation
                    );
                    return;
                }

                _MutePlayer(otherPlayer);
                return;
            }

            if (Utilities.IsValid(_localOverride) && _localOverride.HasStartedOk && _localOverride.MuteOutsiders) {
                _MutePlayer(otherPlayer);
                return;
            }

            _ApplyGlobalAudioSettings(
                    otherPlayer,
                    listenerHeadPosition,
                    listenerHeadRotation,
                    otherHeadPosition - listenerHeadPosition,
                    otherHeadRotation,
                    _currentConfiguration,
                    LocalConfiguration.HeightToVoiceCorrelation
            );
        }

        internal bool _OtherPlayerWithOverrideCanBeHeard(
                PlayerAudioOverride otherPlayerOverride,
                PlayerAudioOverride localPlayerOverride,
                bool muteOutsiders,
                bool disallowLocalPlayerListening
        ) {
            bool playersShareChannels = otherPlayerOverride._HasOverlappingChannels(localPlayerOverride);
            if (playersShareChannels) {
                return !disallowLocalPlayerListening;
            }

            bool otherPlayerInPrivateChannel = otherPlayerOverride._IsPrivateChannel();
            if (otherPlayerInPrivateChannel && !Utilities.IsValid(localPlayerOverride)) {
                // by default players are in NoPrivacyChannel, so the local player can't hear the other player
                return false;
            }

            return localPlayerOverride._IsPrivateChannel() && !otherPlayerInPrivateChannel && !muteOutsiders;
        }

        internal void _ApplyGlobalAudioSettings(
                VRCPlayerApi otherPlayer,
                Vector3 listenerHeadPosition,
                Quaternion listenerHeadRotation,
                Vector3 toOtherPlayer,
                Quaternion otherPlayerHeadRotation,
                PlayerAudioConfigurationModel configuration,
                AnimationCurve heightToVoiceCorrelation
        ) {
            var direction = toOtherPlayer.normalized;
            float distanceBetweenPlayerHeads = toOtherPlayer.magnitude;
            float heightBasedMultiplier = heightToVoiceCorrelation.Evaluate(otherPlayer.GetAvatarEyeHeightAsMeters());
            float relativeAudioRange = _GetGlobalRelativeAudioRange(
                    listenerHeadPosition,
                    listenerHeadRotation,
                    direction,
                    distanceBetweenPlayerHeads,
                    otherPlayerHeadRotation,
                    configuration);
            _ApplyGlobalPlayerVoiceRangeReduction(
                    otherPlayer,
                    distanceBetweenPlayerHeads,
                    configuration,
                    relativeAudioRange,
                    heightBasedMultiplier);
            _ApplyGlobalAvatarAudioRangeReduction(
                    otherPlayer,
                    distanceBetweenPlayerHeads,
                    configuration,
                    relativeAudioRange,
                    heightBasedMultiplier);
        }

        private float _GetGlobalRelativeAudioRange(
                Vector3 listenerHeadPosition,
                Quaternion listenerHeadRotation,
                Vector3 direction,
                float distanceBetweenPlayerHeads,
                Quaternion otherPlayerHeadRotation,
                PlayerAudioConfigurationModel configuration
        ) {
            float rangeReducedByOcclusion = PlayerOcclusionStrategy._GetAudioClarity(
                    listenerHeadPosition,
                    direction,
                    distanceBetweenPlayerHeads,
                    1f - configuration.OcclusionFactor,
                    1f - configuration.PlayerOcclusionFactor,
                    configuration.OcclusionMask
            );

            float rangeReducedByDirection = VoiceUtils._CalculateDirectionality(
                    listenerHeadRotation,
                    otherPlayerHeadRotation,
                    direction,
                    configuration.ListenerDirectionality,
                    configuration.PlayerDirectionality
            );

            return rangeReducedByDirection * rangeReducedByOcclusion;
        }

        private void _ApplyGlobalPlayerVoiceRangeReduction(
                VRCPlayerApi otherPlayer,
                float distanceBetweenPlayerHeads,
                PlayerAudioConfigurationModel configuration,
                float relativeAudioRange,
                float heightBasedMultiplier
        ) {
            float voiceDistanceFactor = _GetAbsoluteRangeReduction(
                    distanceBetweenPlayerHeads,
                    relativeAudioRange,
                    configuration.VoiceDistanceFar * heightBasedMultiplier
            );

            _UpdateVoiceAudio(
                    otherPlayer,
                    voiceDistanceFactor,
                    configuration.EnableVoiceLowpass,
                    configuration.VoiceGain,
                    configuration.VoiceDistanceFar * heightBasedMultiplier,
                    configuration.VoiceDistanceNear * heightBasedMultiplier,
                    configuration.VoiceVolumetricRadius * heightBasedMultiplier
            );
        }

        private void _ApplyGlobalAvatarAudioRangeReduction(
                VRCPlayerApi otherPlayer,
                float distanceBetweenPlayerHeads,
                PlayerAudioConfigurationModel configuration,
                float relativeAudioRange,
                float heightBasedMultiplier
        ) {
            float avatarDistanceFactor = _GetAbsoluteRangeReduction(
                    distanceBetweenPlayerHeads,
                    relativeAudioRange,
                    configuration.AvatarFarRadius * heightBasedMultiplier
            );

            _UpdateAvatarAudio(
                    otherPlayer,
                    avatarDistanceFactor,
                    configuration.ForceAvatarSpatialAudio,
                    configuration.AllowAvatarCustomAudioCurves,
                    configuration.AvatarGain,
                    configuration.AvatarFarRadius * heightBasedMultiplier,
                    configuration.AvatarNearRadius * heightBasedMultiplier,
                    configuration.AvatarVolumetricRadius * heightBasedMultiplier
            );
        }

        private void _ApplyAudioOverrides(
                VRCPlayerApi otherPlayer,
                Vector3 listenerHeadPosition,
                Quaternion listenerHeadRotation,
                Vector3 toOtherPlayer,
                PlayerAudioOverride playerOverride,
                Quaternion otherHeadRotation
        ) {
            var direction = toOtherPlayer.normalized;
            float distanceBetweenPlayerHeads = toOtherPlayer.magnitude;
            float heightBasedMultiplier = playerOverride
                    .HeightToVoiceCorrelation
                    .Evaluate(otherPlayer.GetAvatarEyeHeightAsMeters());
            float relativeAudioRange = _GetOverrideRelativeAudioRange(
                    listenerHeadPosition,
                    listenerHeadRotation,
                    direction,
                    distanceBetweenPlayerHeads,
                    playerOverride,
                    otherHeadRotation);
            _ApplyOverrideVoiceRangeReduction(
                    otherPlayer,
                    distanceBetweenPlayerHeads,
                    playerOverride,
                    relativeAudioRange,
                    heightBasedMultiplier);
            _ApplyOverrideAvatarRangeReduction(
                    otherPlayer,
                    distanceBetweenPlayerHeads,
                    playerOverride,
                    relativeAudioRange,
                    heightBasedMultiplier);
        }

        private void _ApplyOverrideAvatarRangeReduction(
                VRCPlayerApi otherPlayer,
                float distanceBetweenPlayerHeads,
                PlayerAudioOverride playerOverride,
                float relativeAudioRange,
                float heightBasedMultiplier
        ) {
            float avatarDistanceFactor = _GetAbsoluteRangeReduction(
                    distanceBetweenPlayerHeads,
                    relativeAudioRange,
                    playerOverride.TargetAvatarFarRadius * heightBasedMultiplier
            );

            _UpdateAvatarAudio(
                    otherPlayer,
                    avatarDistanceFactor,
                    playerOverride.ForceAvatarSpatialAudio,
                    playerOverride.AllowAvatarCustomAudioCurves,
                    playerOverride.TargetAvatarGain,
                    playerOverride.TargetAvatarFarRadius * heightBasedMultiplier,
                    playerOverride.TargetAvatarNearRadius * heightBasedMultiplier,
                    playerOverride.TargetAvatarVolumetricRadius * heightBasedMultiplier
            );
        }

        private void _ApplyOverrideVoiceRangeReduction(
                VRCPlayerApi otherPlayer,
                float distanceBetweenPlayerHeads,
                PlayerAudioOverride playerOverride,
                float relativeAudioRange,
                float heightBasedMultiplier
        ) {
            float voiceDistanceFactor = _GetAbsoluteRangeReduction(
                    distanceBetweenPlayerHeads,
                    relativeAudioRange,
                    playerOverride.VoiceDistanceFar * heightBasedMultiplier
            );

            _UpdateVoiceAudio(
                    otherPlayer,
                    voiceDistanceFactor,
                    playerOverride.EnableVoiceLowpass,
                    playerOverride.VoiceGain,
                    playerOverride.VoiceDistanceFar * heightBasedMultiplier,
                    playerOverride.VoiceDistanceNear * heightBasedMultiplier,
                    playerOverride.VoiceVolumetricRadius * heightBasedMultiplier
            );
        }

        private float _GetOverrideRelativeAudioRange(
                Vector3 listenerHeadPosition,
                Quaternion listenerHeadRotation,
                Vector3 direction,
                float distanceBetweenPlayerHeads,
                PlayerAudioOverride playerOverride,
                Quaternion otherHeadRotation
        ) {
            float rangeReducedByOcclusion = PlayerOcclusionStrategy._GetAudioClarity(
                    listenerHeadPosition,
                    direction,
                    distanceBetweenPlayerHeads,
                    1f - playerOverride.OcclusionFactor,
                    1f - playerOverride.PlayerOcclusionFactor,
                    playerOverride.OcclusionMask
            );

            float rangeReducedByDirection = VoiceUtils._CalculateDirectionality(
                    listenerHeadRotation,
                    otherHeadRotation,
                    direction,
                    playerOverride.ListenerDirectionality,
                    playerOverride.PlayerDirectionality
            );

            return rangeReducedByDirection * rangeReducedByOcclusion;
        }

        private void _MutePlayer(VRCPlayerApi otherPlayer) {
            _UpdateVoiceAudio(
                    otherPlayer,
                    MinRelativeAudioRange,
                    false,
                    MinRelativeAudioRange,
                    MinRelativeAudioRange,
                    MinRelativeAudioRange,
                    MinRelativeAudioRange
            );

            _UpdateAvatarAudio(
                    otherPlayer,
                    0,
                    false,
                    false,
                    MinRelativeAudioRange,
                    MinRelativeAudioRange,
                    MinRelativeAudioRange,
                    MinRelativeAudioRange
            );
        }

        private int _GetPendingPlayerUpdates(int playerCount) {
            if (PlayerUpdateRate == 0) {
                // this will update all players every update
                return playerCount;
            }

            // make sure at least one player gets updated and no player gets updated twice
            return Mathf.Clamp(PlayerUpdateRate, 1, playerCount);
        }


        private float _GetAbsoluteRangeReduction(
                float distanceBetweenPlayerHeads,
                float relativeAudioRange,
                float maxAudibleRange
        ) {
            if (relativeAudioRange < SmallNumber) {
                return MinRelativeAudioRange;
            }

            if (_PlayerCanNotBeHeard(maxAudibleRange)
                || _PlayerIsOutOfHearingRange(distanceBetweenPlayerHeads, maxAudibleRange)) {
                return MaxRelativeAudioRange;
            }

            float remainingDistanceToFarRadius = maxAudibleRange - distanceBetweenPlayerHeads;
            float postOcclusionFarDistance =
                    remainingDistanceToFarRadius * relativeAudioRange + distanceBetweenPlayerHeads;
            return postOcclusionFarDistance / maxAudibleRange;
        }

        private static bool _PlayerCanNotBeHeard(float maxAudibleRange) {
            return maxAudibleRange == 0;
        }

        private static bool _PlayerIsOutOfHearingRange(float distanceBetweenPlayerHeads, float maxAudibleRange) {
            return maxAudibleRange < distanceBetweenPlayerHeads;
        }


        private void _UpdateVoiceAudio(
                VRCPlayerApi vrcPlayerApi,
                float clarity,
                bool enableVoiceLowpass,
                float targetVoiceGain,
                float targetVoiceDistanceFar,
                float targetVoiceDistanceNear,
                float targetVoiceVolumetricRadius
        ) {
            if (!Utilities.IsValid(vrcPlayerApi)) {
                return;
            }

            vrcPlayerApi.SetVoiceLowpass(enableVoiceLowpass);
            vrcPlayerApi.SetVoiceGain(targetVoiceGain);
            vrcPlayerApi.SetVoiceDistanceFar(targetVoiceDistanceFar * clarity);
            vrcPlayerApi.SetVoiceDistanceNear(targetVoiceDistanceNear * clarity);
            vrcPlayerApi.SetVoiceVolumetricRadius(targetVoiceVolumetricRadius * clarity);
        }

        private void _UpdateAvatarAudio(
                VRCPlayerApi vrcPlayerApi,
                float clarity,
                bool forceAvatarSpatialAudio,
                bool allowAvatarCustomAudioCurves,
                float targetAvatarGain,
                float targetAvatarFarRadius,
                float targetAvatarNearRadius,
                float targetAvatarVolumetricRadius
        ) {
            if (!Utilities.IsValid(vrcPlayerApi)) {
                return;
            }

            vrcPlayerApi.SetAvatarAudioForceSpatial(forceAvatarSpatialAudio);
            vrcPlayerApi.SetAvatarAudioCustomCurve(allowAvatarCustomAudioCurves);

            vrcPlayerApi.SetAvatarAudioGain(
                    targetAvatarGain *
                    clarity); // otherwise clarity does not seem to affect volume of avatar audio enough when large distances are used
            vrcPlayerApi.SetAvatarAudioFarRadius(targetAvatarFarRadius * clarity);
            vrcPlayerApi.SetAvatarAudioNearRadius(targetAvatarNearRadius * clarity);
            vrcPlayerApi.SetAvatarAudioVolumetricRadius(targetAvatarVolumetricRadius * clarity);
        }

        #region internal
        #region OverridePlayerSettings
        private bool _AddOverrideToNewList(PlayerAudioOverride playerAudioOverride, VRCPlayerApi playerToAffect) {
            var instance = PlayerOverrideListPool._Get();
            if (!Utilities.IsValid(instance)) {
                _Error(
                        $"Failed to retrieve {nameof(PlayerAudioOverrideList)} instance from {PlayerOverrideListPool._GetScriptPathInScene()}");
                return false;
            }

            var createdOverrideList = instance.GetComponent<PlayerAudioOverrideList>();
            if (!Utilities.IsValid(createdOverrideList)) {
                _Error(
                        $"{nameof(PlayerAudioOverrideList)} component missing object retrieved from {PlayerOverrideListPool._GetScriptPathInScene()}");
                PlayerOverrideListPool._Return(instance);
                return false;
            }

            if (!createdOverrideList._AddOverride(playerAudioOverride)) {
                _Error(
                        $"Failed to add {playerAudioOverride._GetScriptPathInScene()} to created list of {playerToAffect._ToStringSafe()}");
                PlayerOverrideListPool._Return(instance);
                return false;
            }

            PlayersToOverride.Add(playerToAffect.playerId, createdOverrideList);
            return true;
        }

        private bool _AddOverrideToExistingList(
                PlayerAudioOverride playerAudioOverride,
                VRCPlayerApi playerToAffect,
                PlayerAudioOverrideList existingList
        ) {
            if (existingList._Contains(playerAudioOverride)) {
                existingList._RemoveOverride(playerAudioOverride);
            }

            if (existingList._AddOverride(playerAudioOverride)) {
                return true;
            }

            _Error(
                    $"Failed to add {playerAudioOverride._GetScriptPathInScene()} to {existingList._GetScriptPathInScene()} of {playerToAffect._ToStringSafe()}");
            return false;
        }
        #endregion

        internal void _ResetAllPlayerVoices() {
            for (int i = 0; i < ExistingRemotePlayerIds.Count;) {
                int playerId = ExistingRemotePlayerIds[i].Int;
                var playerApi = playerId._IdToVrcPlayer();
                if (!Utilities.IsValid(playerApi)) {
                    // this is not supposed to happen as Players should be removed by OnPlayerLeft before they become invalid
                    _Warn($"Player {playerId} at position {i} has unexpectedly become invalid, deleting entry");
                    ExistingRemotePlayerIds.RemoveAt(i);
                    continue;
                }

                _UpdateVoiceAudio(playerApi, 1f, true, 15f, 25, 0, 0);
                _UpdateAvatarAudio(playerApi, 1f, false, false, 10f, 40, 0, 0);
                i++;
            }
        }

        internal PlayerAudioOverride _UpdateAudioFilters(
                PlayerAudioOverride newOverride,
                PlayerAudioOverride oldOverride
        ) {
            if (ReferenceEquals(newOverride, oldOverride)) {
                return newOverride;
            }

            if (Utilities.IsValid(newOverride)) {
                _UseReverbSettings(newOverride.OptionalReverb);
                return newOverride;
            }

            _UseReverbSettings(null);
            return null;
        }

        public override void _OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_OnEvent)} {eventName}");
#endif
            #endregion

            if (!IsControllerInitialized) {
                _Error($"{nameof(_OnEvent)}: MVC not initialized");
                return;
            }

            switch (eventName) {
                case nameof(_OnModelChanged):
                    _OnModelChanged();

                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }

        private void _OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnModelChanged));
#endif
            #endregion

            if (!Utilities.IsValid(LocalConfiguration)) {
                _Error($"{nameof(LocalConfiguration)} invalid");
                return;
            }

            if (!Utilities.IsValid(SyncedMasterConfiguration)) {
                _Error($"{nameof(SyncedMasterConfiguration)} invalid");
                return;
            }

            _currentConfiguration = !Networking.IsMaster && LocalConfiguration.AllowMasterControlLocalValues
                    ? SyncedMasterConfiguration
                    : LocalConfiguration;
        }

        private bool _AreMandatoryReferencesValid() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_AreMandatoryReferencesValid));
#endif
            #endregion

            if (!Utilities.IsValid(LocalConfiguration)) {
                _Error($"{nameof(LocalConfiguration)} invalid");
                return false;
            }

            if (Utilities.IsValid(SyncedMasterConfiguration)) {
                return true;
            }

            _Error($"{nameof(SyncedMasterConfiguration)} invalid");
            return false;
        }
        #endregion

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnOwnershipTransferred)} to '{player._DisplayNameSafe()}'");
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(OnOwnershipTransferred)}: Not initialized");
                return;
            }

            if (!IsControllerInitialized) {
                _Error($"{nameof(OnOwnershipTransferred)}: MVC not initialized");
                return;
            }

            if (!Utilities.IsValid(LocalConfiguration)) {
                _Error($"{nameof(LocalConfiguration)} is invalid");
                return;
            }

            LocalConfiguration.Dirty = true;
            LocalConfiguration._NotifyIfDirty(1);
        }

        #region Player Events
        public override void OnPlayerJoined(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnPlayerJoined)} '{player._DisplayNameSafe()}'");
#endif
            #endregion

            if (!Utilities.IsValid(player)) {
                _Error("Invalid player joined");
                return;
            }

            if (player.isLocal) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog("Local player joined and is being ignored");
#endif
                #endregion

                return;
            }

            if (ExistingRemotePlayerIds.Contains(player.playerId)) {
                _Warn($"{player._ToStringSafe()} already exists!");
                return;
            }

            ExistingRemotePlayerIds.Add(new DataToken(player.playerId));

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{player._ToStringSafe()} added to {nameof(ExistingRemotePlayerIds)}");
#endif
            #endregion
        }

        public override void OnPlayerLeft(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnPlayerLeft)} '{player._DisplayNameSafe()}'");
#endif
            #endregion

            if (player._IsLocalSafe()) {
                // don't bother doing anything
                return;
            }

            if (!HasStartedOk) {
                _Error($"{nameof(OnPlayerLeft)}: Not initialized");
                return;
            }

            if (Utilities.IsValid(player)) {
                ExistingRemotePlayerIds.RemoveAll(player.playerId);
            }

            if (!IsControllerInitialized) {
                _Error($"{nameof(OnPlayerLeft)}: MVC not initialized");
                return;
            }

            _PurgeOverridesOfPlayer(player._PlayerIdSafe());
            _PurgeOverridesOfInvalidPlayers();

            if (!Utilities.IsValid(LocalConfiguration)) {
                _Error($"{nameof(LocalConfiguration)} is invalid");
                return;
            }

            LocalConfiguration.Dirty = true;
            LocalConfiguration._NotifyIfDirty(1);
        }
        #endregion


        #region Player Checks
        /// <param name="player"></param>
        /// <returns>True when a player is not ignored and is affected only by global effects</returns>
        public bool _UsesDefaultEffects(VRCPlayerApi player) {
            if (!HasStartedOk) {
                _Error($"{nameof(_UsesDefaultEffects)}: Not initialized");
                return false;
            }

            if (IsControllerInitialized) {
                return !IgnoredPlayers._Contains(player._PlayerIdSafe()) && !_HasActiveVoiceOverrides(player);
            }

            _Error($"{nameof(_UsesDefaultEffects)}: MVC not initialized");
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true when a player is not affected global nor by override effects</returns>
        public bool _IsIgnored(VRCPlayerApi player) {
            if (!HasStartedOk) {
                _Error($"{nameof(_IsIgnored)}: Not initialized");
                return false;
            }

            if (IsControllerInitialized) {
                return IgnoredPlayers._Contains(player._PlayerIdSafe());
            }

            _Error($"{nameof(_IsIgnored)}: MVC not initialized");
            return false;
        }

        /// <param name="player"></param>
        /// <returns>true if the player is not ignored and is assigned to at least one override</returns>
        public bool _UsesVoiceOverride(VRCPlayerApi player) {
            if (!HasStartedOk) {
                _Error($"{nameof(_UsesVoiceOverride)}: Not initialized");
                return false;
            }

            if (IsControllerInitialized) {
                return !IgnoredPlayers._Contains(player._PlayerIdSafe()) && _HasActiveVoiceOverrides(player);
            }

            _Error($"{nameof(_UsesVoiceOverride)}: MVC not initialized");
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if the player is assigned to at least one override</returns>
        public bool _HasActiveVoiceOverrides(VRCPlayerApi player) {
            if (!HasStartedOk) {
                _Error($"{nameof(_HasActiveVoiceOverrides)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                _Error($"{nameof(_HasActiveVoiceOverrides)}: MVC not initialized");
                return false;
            }

            if (!Utilities.IsValid(player)) {
                _Error($"{nameof(player)} invalid");
                return false;
            }

            if (player.isLocal) {
                return LocalPlayerOverrideList._GetMaxPriority(player, out var unused);
            }

            if (!PlayersToOverride.TryGetValue(player.playerId, out var list)) {
                return false;
            }

            var overrideList = (PlayerAudioOverrideList)list.Reference;
            return Utilities.IsValid(overrideList) && overrideList._GetMaxPriority(player, out var unused2);
        }
        #endregion


        #region Public
        /// <summary>
        /// local player not included if it has an override
        /// </summary>
        /// <returns>copy of array of player ids or empty array</returns>
        public DataList _GetNonLocalPlayersWithOverrides() {
#if TLP_DEBUG
            _DebugLog(nameof(_GetNonLocalPlayersWithOverrides));
#endif
            if (!HasStartedOk) {
                _Error($"{nameof(_HasActiveVoiceOverrides)}: Not initialized");
                return new DataList();
            }


            if (IsControllerInitialized) {
                return PlayersToOverride.GetKeys();
            }

            _Error($"{nameof(_GetNonLocalPlayersWithOverrides)}: MVC not initialized");
            return new DataList();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="audioReverbFilter">Clears the reverb settings when null, otherwise copies and applies the values</param>
        internal void _UseReverbSettings(AudioReverbFilter audioReverbFilter) {
#if TLP_DEBUG
            _DebugLog(nameof(_UseReverbSettings));
#endif
            bool clearSettings = !Utilities.IsValid(audioReverbFilter);
            if (clearSettings) {
                MainAudioReverbFilter.reverbPreset = AudioReverbPreset.Off;
                return;
            }

            var newPreset = audioReverbFilter.reverbPreset;
            audioReverbFilter.enabled = true;
            audioReverbFilter.gameObject.SetActive(true);

            MainAudioReverbFilter.reverbPreset = newPreset;
            bool doesNotNeedToCopyCustomSettings = newPreset != AudioReverbPreset.User;
            if (doesNotNeedToCopyCustomSettings) {
                return;
            }

            MainAudioReverbFilter.density = audioReverbFilter.density;
            MainAudioReverbFilter.diffusion = audioReverbFilter.diffusion;
            MainAudioReverbFilter.room = audioReverbFilter.room;
            MainAudioReverbFilter.roomHF = audioReverbFilter.roomHF;
            MainAudioReverbFilter.roomLF = audioReverbFilter.roomLF;
            MainAudioReverbFilter.decayTime = audioReverbFilter.decayTime;
            MainAudioReverbFilter.dryLevel = audioReverbFilter.dryLevel;
            MainAudioReverbFilter.hfReference = audioReverbFilter.hfReference;
            MainAudioReverbFilter.lfReference = audioReverbFilter.lfReference;
            MainAudioReverbFilter.reflectionsDelay = audioReverbFilter.reflectionsDelay;
            MainAudioReverbFilter.reflectionsLevel = audioReverbFilter.reflectionsLevel;
            MainAudioReverbFilter.reverbDelay = audioReverbFilter.reverbDelay;
            MainAudioReverbFilter.reverbLevel = audioReverbFilter.reverbLevel;
            MainAudioReverbFilter.decayHFRatio = audioReverbFilter.decayHFRatio;
        }
        #endregion


        #region Overrides
        protected override bool _InitializeInternal() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_InitializeInternal));
#endif
            #endregion

            if (!Utilities.IsValid(PlayerOverrideListPool)) {
                _Error($"{nameof(PlayerOverrideListPool)} is not set");
                return false;
            }

            if (!Utilities.IsValid(DefaultConfiguration)) {
                _Error($"{nameof(DefaultConfiguration)} is not set");
                return false;
            }

            if (!Utilities.IsValid(PlayerOcclusionStrategy)) {
                _Error($"{nameof(PlayerOcclusionStrategy)} is not set");
                return false;
            }

            if (!Utilities.IsValid(IgnoredPlayers)) {
                _Error($"{nameof(IgnoredPlayers)} is not set");
                return false;
            }

            if (!Utilities.IsValid(MainAudioReverbFilter)) {
                _Error($"{nameof(MainAudioReverbFilter)} is not set");
                return false;
            }

            return base._InitializeInternal();
        }

        internal static string _ExpectedGameObjectName() {
            return $"TLP_{nameof(PlayerAudioController)}";
        }

        internal void _LogVrcDefaultAudioSettings() {
            // TODO VRChat wtf?! why are the avatar settings not exposed as well???
            var player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player)) {
                _Error($"{nameof(_LogVrcDefaultAudioSettings)}: Local player invalid");
                return;
            }

            _Info(
                    "VRChat client default player voice settings (BEFORE UdonVoiceUtils is activated):\n"
                    + $"VoiceDistanceNear: {player.GetVoiceDistanceNear()} meter\n"
                    + $"VoiceDistanceFar: {player.GetVoiceDistanceFar()} meter\n"
                    + $"VoiceVolumetricRadius: {player.GetVoiceVolumetricRadius()} meter\n"
                    + $"VoiceGain: {player.GetVoiceGain()} decibel\n"
                    + $"VoiceLowPassFiltering: {(player.GetVoiceLowpass() ? "Yes" : "No")}\n");
        }

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            string expectedName = _ExpectedGameObjectName();
            if (gameObject.name != expectedName) {
                _Warn($"Changing name of GameObject '{transform._GetPathInScene()}' to '{expectedName}'");
                gameObject.name = expectedName;
            }

            if (!_AreMandatoryReferencesValid()) {
                _ErrorAndDisableComponent($"Setup incomplete");
                return false;
            }

            if (!IsControllerInitialized && !_ValidateAndSetupMvc()) {
                _ErrorAndDisableComponent($"MVC setup failed");
                return false;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _LogVrcDefaultAudioSettings();
#endif
            #endregion


            if (_GetMaxPriorityOverride(LocalPlayer, out var localOverride)) {
                _UseReverbSettings(localOverride.OptionalReverb);
            }

            return true;
        }
        #endregion
    }
}