using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Pool;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerOcclusion;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core
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
        private PlayerAudioConfigurationModel _currentConfiguration;

        #region State
        internal readonly DataList ExistingRemotePlayerIds = new DataList();


        /// <summary>
        /// Map of playerIDs to PlayerAudioOverrideList
        /// </summary>
        internal readonly DataDictionary PlayersToOverride = new DataDictionary();

        private bool _receivedStart;
        private int _playerIndex;

        private PlayerAudioOverride _localOverride;
        internal readonly DataDictionary PlayerUpdateListeners = new DataDictionary();
        #endregion

        #region Unity Lifecycle
        public void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (!HasStartedOk) {
                Error($"{nameof(OnEnable)}: Not initialized");
            }
        }

        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            #endregion

            UseReverbSettings(null);
            ResetAllPlayerVoices();

            if (!HasStartedOk) {
                Warn($"{nameof(OnDisable)}: Not initialized, nothing to do");
                HasReceivedStart = false; // allow re-start
                return;
            }

            HasStartedOk = false;

            if (Menu.HasStartedOk
                && Menu.IsViewInitialized
                && !Menu.DeInitialize()) {
                Error($"{nameof(OnDisable)}: {nameof(DeInitialize)} of {nameof(Menu)} failed");
            }

            if (!DeInitialize()) {
                Error($"{nameof(OnDisable)} {nameof(DeInitialize)} of {GetUdonTypeName()} failed");
            }

            if (LocalConfiguration.HasStartedOk
                && LocalConfiguration.IsModelInitialized
                && !LocalConfiguration.DeInitialize()) {
                Error($"DeInitialization of {nameof(LocalConfiguration)} failed");
            }
        }

        public override void PostLateUpdate() {
            if (!HasStartedOk) {
                #region TLP_DEBUG
#if TLP_DEBUG
                Warn($"{nameof(PostLateUpdate)}: Not initialized");
#endif
                #endregion

                return;
            }

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                Error($"Local player is not valid");
                return;
            }

            var newLocalOverride = LocalPlayerOverrideList.GetMaxPriority(localPlayer);
            _localOverride = UpdateAudioFilters(newLocalOverride, _localOverride);

            for (int playerUpdate = 0;
                 playerUpdate < GetPendingPlayerUpdates(ExistingRemotePlayerIds.Count);
                 ++playerUpdate) {
                UpdateNextPlayer(localPlayer, ref playerUpdate);
            }
        }

        protected override void OnDestroy() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDestroy));
#endif
            #endregion

            UseReverbSettings(null);
            ResetAllPlayerVoices();
        }
        #endregion


        #region Public API
        public void AddPlayerUpdateListener(TlpBaseBehaviour listener, int playerId) {
            if (!HasStartedOk) {
                Error($"{nameof(AddPlayerUpdateListener)}: Not initialized");
                return;
            }

            PlayerUpdateListeners[playerId] = listener;
        }

        public bool RemovePlayerUpdateListener(int playerId) {
            if (!HasStartedOk) {
                Error($"{nameof(RemovePlayerUpdateListener)}: Not initialized");
                return false;
            }

            return PlayerUpdateListeners.Remove(playerId);
        }

        /// <summary>
        /// Add a player that shall not be affected by any effect of this script. Upon adding the player
        /// all values of the player will be set to the currently defined values on this script.
        /// Occlusion and directionality effects are reverted if the player was affected.
        /// This function is local only.
        /// </summary>
        /// <param name="playerToIgnore"></param>
        /// <returns>true on success, false if argument invalid or player doesn't exist or is already ignored</returns>
        public bool IgnorePlayer(VRCPlayerApi playerToIgnore) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(IgnorePlayer)} {playerToIgnore.ToStringSafe()}");
#endif
            #endregion

            if (!HasStartedOk) {
                Error($"{nameof(IgnorePlayer)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                Error($"{nameof(IgnorePlayer)}: MVC not initialized");
                return false;
            }

            return IgnoredPlayers.Add(playerToIgnore);
        }

        /// <summary>
        /// Remove a player from the ignore list and let it be affected again by this script.
        /// This function is local only.
        /// </summary>
        /// <param name="ignoredPlayer"></param>
        /// <returns>false if player was invalid or not ignored, true if the player was removed from the ignore list</returns>
        public bool UnIgnorePlayer(VRCPlayerApi ignoredPlayer) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(UnIgnorePlayer)} {ignoredPlayer.ToStringSafe()}");
#endif
            #endregion

            if (!HasStartedOk) {
                Error($"{nameof(UnIgnorePlayer)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                Error($"{nameof(UnIgnorePlayer)}: MVC not initialized");
                return false;
            }

            return IgnoredPlayers.Remove(ignoredPlayer);
        }

        public bool OverridePlayerSettings(
                PlayerAudioOverride playerAudioOverride,
                VRCPlayerApi playerToAffect
        ) {
#if TLP_DEBUG
            DebugLog(nameof(OverridePlayerSettings));
#endif
            if (!HasStartedOk) {
                Error($"{nameof(OverridePlayerSettings)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                Error($"{nameof(OverridePlayerSettings)}: MVC not initialized");
                return false;
            }

            if (!Utilities.IsValid(playerAudioOverride)) {
                Error($"{nameof(playerAudioOverride)} invalid");
                return false;
            }

            if (!Utilities.IsValid(playerToAffect)) {
                Error($"{nameof(playerToAffect)} invalid");
                return false;
            }

            if (playerToAffect.isLocal) {
                return LocalPlayerOverrideList.AddOverride(playerAudioOverride);
            }

            // check if the player already has an override
            if (PlayersToOverride.TryGetValue(playerToAffect.playerId, out var value)) {
                var existingList = (PlayerAudioOverrideList)value.Reference;
                if (Utilities.IsValid(existingList)) {
                    return AddOverrideToExistingList(playerAudioOverride, playerToAffect, existingList);
                }
            }

            return AddOverrideToNewList(playerAudioOverride, playerToAffect);
        }

        public bool ClearPlayerOverride(PlayerAudioOverride playerAudioOverride, VRCPlayerApi playerToClear) {
#if TLP_DEBUG
            DebugLog(nameof(ClearPlayerOverride));
#endif
            if (!HasStartedOk) {
                Error($"{nameof(ClearPlayerOverride)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                Error($"{nameof(ClearPlayerOverride)}: MVC not initialized");
                return false;
            }

            if (!Utilities.IsValid(playerToClear)) {
                Error($"{nameof(playerToClear)} invalid");
                return false;
            }

            if (!Utilities.IsValid(playerAudioOverride)) {
                Error($"{nameof(playerAudioOverride)} invalid");
                return false;
            }

            if (playerToClear.isLocal) {
                if (LocalPlayerOverrideList.Contains(playerAudioOverride)) {
                    LocalPlayerOverrideList.RemoveOverride(playerAudioOverride);
                    return true;
                }

                DebugLog(
                        $"{playerToClear.ToStringSafe()} didn't have the local override {playerAudioOverride.GetScriptPathInScene()}");
                return false;
            }

            if (!PlayersToOverride.TryGetValue(playerToClear.playerId, out var list)) {
                Warn(
                        $"{playerToClear.ToStringSafe()} doesn't have any overrides");
                return false;
            }

            var overrideList = (PlayerAudioOverrideList)list.Reference;
            if (Utilities.IsValid(overrideList) && overrideList.Contains(playerAudioOverride)) {
                overrideList.RemoveOverride(playerAudioOverride);
                return true;
            }

            Warn(
                    $"{playerToClear.ToStringSafe()} didn't have the override {playerAudioOverride.GetScriptPathInScene()}");
            return false;
        }

        /// <param name="player"></param>
        /// <returns>null on error or if no overrides present</returns>
        public PlayerAudioOverride GetMaxPriorityOverride(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(GetMaxPriorityOverride)} {player.ToStringSafe()}");
#endif
            #endregion

            if (!IsReceivingStart && !HasStartedOk) {
                Error($"{nameof(GetMaxPriorityOverride)}: Not initialized");
                return null;
            }

            if (!IsControllerInitialized) {
                Error($"{nameof(GetMaxPriorityOverride)}: MVC not initialized");
                return null;
            }

            if (!Utilities.IsValid(player)) {
                Error($"{nameof(player)} invalid");
                return null;
            }

            if (player.isLocal) {
                return LocalPlayerOverrideList.GetMaxPriority(player);
            }

            if (!PlayersToOverride.TryGetValue(player.playerId, out var list)) {
                return null;
            }

            var overrideList = (PlayerAudioOverrideList)list.Reference;
            return Utilities.IsValid(overrideList) ? overrideList.GetMaxPriority(player) : null;
        }
        #endregion

        #region OnPlayerLeft
        private void PurgeOverridesOfPlayer(int playerId) {
            if (!PlayersToOverride.TryGetValue(playerId, out var list)) {
                return;
            }

            var overrideList = (PlayerAudioOverrideList)list.Reference;
            if (Utilities.IsValid(overrideList)) {
                PlayerOverrideListPool.Return(overrideList.gameObject);
            }

            PlayersToOverride.Remove(playerId);
        }

        private void PurgeOverridesOfInvalidPlayers() {
            var playerIds = PlayersToOverride.GetKeys();
            for (int i = 0; i < playerIds.Count; i++) {
                var entry = playerIds[i];
                int playerId = entry.Int;
                if (!playerId.IsValidPlayer(out var unused)) {
                    PurgeOverridesOfPlayer(playerId);
                }
            }
        }
        #endregion

        private void UpdateNextPlayer(VRCPlayerApi localPlayer, ref int playerUpdate) {
            _playerIndex.MoveIndexRightLooping(ExistingRemotePlayerIds.Count);
            int playerId = ExistingRemotePlayerIds[_playerIndex].Int;

            if (!playerId.IsValidPlayer(out var player)) {
                // this is not supposed to happen as Players should be removed by OnPlayerLeft before they become invalid
                Error(
                        $"{nameof(PostLateUpdate)}: Player {playerId} at position " +
                        $"{_playerIndex} has unexpectedly become invalid, deleting entry");

                // remove invalid entry
                ExistingRemotePlayerIds.RemoveAt(_playerIndex);

                // ensure that we don't skip an entry in the next iteration after deleting the invalid one
                _playerIndex.MoveIndexLeftLooping(ExistingRemotePlayerIds.Count);
                --playerUpdate;
                return;
            }

            if (IgnoredPlayers.Contains(playerId)) {
                return;
            }

            UpdateOtherPlayer(localPlayer, player);
        }

        private bool ValidateAndSetupMvc() {
            if (!LocalConfiguration.IsModelInitialized &&
                !LocalConfiguration.Initialize(LocalConfiguration.gameObject.GetComponent<UdonEvent>())) {
                ErrorAndDisableComponent($"Failed to initialize {nameof(LocalConfiguration)}");
                return false;
            }

            if (SyncedMasterConfiguration.transform.childCount != 1) {
                ErrorAndDisableComponent(
                        $"Expected the {nameof(SyncedMasterConfiguration)} to have one child with the {nameof(UdonEvent)}"
                );
                return false;
            }

            var child = SyncedMasterConfiguration.transform.GetChild(0);
            if (!child) {
                ErrorAndDisableComponent(
                        $"Expected the {nameof(SyncedMasterConfiguration)} to have one child with the {nameof(UdonEvent)}"
                );
                return false;
            }

            var masterConfigChangeEvent = child.gameObject.GetComponent<UdonEvent>();
            if (!masterConfigChangeEvent) {
                ErrorAndDisableComponent(
                        $"Expected the {nameof(SyncedMasterConfiguration)} to have one child with the {nameof(UdonEvent)}"
                );
                return false;
            }

            if (!SyncedMasterConfiguration.IsModelInitialized &&
                !SyncedMasterConfiguration.Initialize(masterConfigChangeEvent)) {
                ErrorAndDisableComponent($"Failed to initialize {nameof(SyncedMasterConfiguration)}");
                return false;
            }

            if (!LocalConfiguration.ChangeEvent.AddListenerVerified(this, nameof(OnModelChanged))) {
                ErrorAndDisableComponent("Failed to listen to local configuration");
                return false;
            }

            if (!SyncedMasterConfiguration.ChangeEvent.AddListenerVerified(this, nameof(OnModelChanged))) {
                ErrorAndDisableComponent("Failed to listen to master configuration");
                return false;
            }

            if (!Utilities.IsValid(Menu)) {
                Menu = GetComponent<DummyView>();
            }

            _currentConfiguration = LocalConfiguration;
            if (!Initialize(_currentConfiguration, Menu)) {
                ErrorAndDisableComponent($"Init of {GetUdonTypeName()} failed");
                return false;
            }

            if (!Utilities.IsValid(Menu) || Menu.IsViewInitialized) {
                ErrorAndDisableComponent($"{nameof(Menu)} is already initialized");
                return false;
            }

            if (!Menu.Initialize(this, _currentConfiguration)) {
                ErrorAndDisableComponent($"Init of {nameof(Menu)} failed");
                return false;
            }

            return true;
        }

        internal void UpdateOtherPlayer(VRCPlayerApi localPlayer, VRCPlayerApi otherPlayer) {
            var playerOverride = GetMaxPriorityOverride(otherPlayer);

            DataList localPrivacyChannels = null;
            bool muteOutsiders = false;
            bool disallowListeningToChannel = false;

            if (Utilities.IsValid(_localOverride)) {
                localPrivacyChannels = _localOverride.PrivacyChannelIdsList;
                muteOutsiders = _localOverride.MuteOutsiders;
                disallowListeningToChannel = _localOverride.DisallowListeningToChannel;
            }

            Vector3 listenerHeadPosition;
            Quaternion listenerHeadRotation;
            var listenerHead = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            bool isLocalPlayerHumanoid = listenerHead.position.sqrMagnitude > 0.001f;
            if (isLocalPlayerHumanoid) {
                listenerHeadPosition = listenerHead.position;
                listenerHeadRotation = listenerHead.rotation;
            }
            else
            {
                // create a fake head position/rotation (no pitch and roll)
                var avatarRootRotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).rotation;
                var playerUp = avatarRootRotation * Vector3.up;

                listenerHeadRotation = avatarRootRotation;
                listenerHeadPosition = localPlayer.GetPosition() + playerUp * localPlayer.GetAvatarEyeHeightAsMeters();
            }

            Vector3 otherHeadPosition;
            Quaternion otherHeadRotation;
            var otherPlayerHead = otherPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            bool isOtherPlayerHumanoid = otherPlayerHead.position.sqrMagnitude > 0.001f;
            if (isOtherPlayerHumanoid) {
                otherHeadPosition = otherPlayerHead.position;
                otherHeadRotation = otherPlayerHead.rotation;
            }
            else
            {
                // create a fake head position/rotation (no pitch and roll)
                var avatarRootRotation = otherPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).rotation;
                var playerUp = avatarRootRotation * Vector3.up;

                otherHeadRotation = avatarRootRotation;
                otherHeadPosition = otherPlayer.GetPosition() + playerUp * otherPlayer.GetAvatarEyeHeightAsMeters();
            }

            var listenerToPlayer = otherHeadPosition - listenerHeadPosition;
            var direction = listenerToPlayer.normalized;
            float distanceBetweenPlayerHeads = listenerToPlayer.magnitude;

            bool localPlayerInPrivateChannel = HasPrivacyChannels(localPrivacyChannels);
            if (Utilities.IsValid(playerOverride)) {
                if (OtherPlayerWithOverrideCanBeHeard(
                            playerOverride,
                            localPlayerInPrivateChannel,
                            localPrivacyChannels,
                            muteOutsiders,
                            disallowListeningToChannel
                    )) {
                    ApplyAudioOverrides(
                            otherPlayer,
                            listenerHeadPosition,
                            listenerHeadRotation,
                            direction,
                            distanceBetweenPlayerHeads,
                            playerOverride,
                            otherHeadRotation
                    );
                    NotifyVoiceUpdateListeners(otherPlayer);
                    return;
                }

                MutePlayer(otherPlayer);
                return;
            }

            if (muteOutsiders) {
                MutePlayer(otherPlayer);
                return;
            }

            ApplyGlobalAudioSettings(
                    otherPlayer,
                    listenerHeadPosition,
                    listenerHeadRotation,
                    direction,
                    distanceBetweenPlayerHeads,
                    otherHeadRotation,
                    _currentConfiguration,
                    LocalConfiguration.HeightToVoiceCorrelation
            );
            NotifyVoiceUpdateListeners(otherPlayer);
        }

        internal static bool OtherPlayerWithOverrideCanBeHeard(
                PlayerAudioOverride playerOverride,
                bool localPlayerInPrivateChannel,
                DataList localPrivacyChannels,
                bool muteOutsiders,
                bool disallowLocalPlayerListening
        ) {
            // Check if players share any privacy channels
            bool playersSharePrivateChannels = SharesAnyPrivacyChannels(localPrivacyChannels, playerOverride.PrivacyChannelIdsList);
            bool playerInSamePrivateChannelAllowedToBeHeard = playersSharePrivateChannels && !disallowLocalPlayerListening;

            // Check if other player is not in any private channel
            bool otherPlayerNotInAnyPrivateChannel = !HasPrivacyChannels(playerOverride.PrivacyChannelIdsList);
            bool isOutsiderAndCanBeHeard = localPlayerInPrivateChannel
                                           && otherPlayerNotInAnyPrivateChannel
                                           && !muteOutsiders;

            return playerInSamePrivateChannelAllowedToBeHeard || isOutsiderAndCanBeHeard;
        }

        internal void ApplyGlobalAudioSettings(
                VRCPlayerApi otherPlayer,
                Vector3 listenerHeadPosition,
                Quaternion listenerHeadRotation,
                Vector3 direction,
                float distanceBetweenPlayerHeads,
                Quaternion otherPlayerHeadRotation,
                PlayerAudioConfigurationModel configuration,
                AnimationCurve heightToVoiceCorrelation
        ) {
            float heightBasedMultiplier = heightToVoiceCorrelation.Evaluate(otherPlayer.GetAvatarEyeHeightAsMeters());
            float relativeAudioRange = GetGlobalRelativeAudioRange(
                    listenerHeadPosition,
                    listenerHeadRotation,
                    direction,
                    distanceBetweenPlayerHeads,
                    otherPlayerHeadRotation,
                    configuration);
            ApplyGlobalPlayerVoiceRangeReduction(
                    otherPlayer,
                    distanceBetweenPlayerHeads,
                    configuration,
                    relativeAudioRange,
                    heightBasedMultiplier);
            ApplyGlobalAvatarAudioRangeReduction(
                    otherPlayer,
                    distanceBetweenPlayerHeads,
                    configuration,
                    relativeAudioRange,
                    heightBasedMultiplier);
        }

        private float GetGlobalRelativeAudioRange(
                Vector3 listenerHeadPosition,
                Quaternion listenerHeadRotation,
                Vector3 direction,
                float distanceBetweenPlayerHeads,
                Quaternion otherPlayerHeadRotation,
                PlayerAudioConfigurationModel configuration
        ) {
            float rangeReducedByOcclusion = PlayerOcclusionStrategy.GetRemainingAudioRange(
                    listenerHeadPosition,
                    direction,
                    distanceBetweenPlayerHeads,
                    1f - configuration.OcclusionFactor,
                    1f - configuration.PlayerOcclusionFactor,
                    configuration.OcclusionMask
            );

            float rangeReducedByDirection = CalculateDirectionality(
                    listenerHeadRotation,
                    otherPlayerHeadRotation,
                    direction,
                    configuration.ListenerDirectionality,
                    configuration.PlayerDirectionality
            );

            return rangeReducedByDirection * rangeReducedByOcclusion;
        }

        private void ApplyGlobalPlayerVoiceRangeReduction(
                VRCPlayerApi otherPlayer,
                float distanceBetweenPlayerHeads,
                PlayerAudioConfigurationModel configuration,
                float relativeAudioRange,
                float heightBasedMultiplier
        ) {
            float voiceDistanceFactor = GetAbsoluteRangeReduction(
                    distanceBetweenPlayerHeads,
                    relativeAudioRange,
                    configuration.VoiceDistanceFar * heightBasedMultiplier
            );

            UpdateVoiceAudio(
                    otherPlayer,
                    voiceDistanceFactor,
                    configuration.EnableVoiceLowpass,
                    configuration.VoiceGain,
                    configuration.VoiceDistanceFar * heightBasedMultiplier,
                    configuration.VoiceDistanceNear * heightBasedMultiplier,
                    configuration.VoiceVolumetricRadius * heightBasedMultiplier
            );
        }

        private void ApplyGlobalAvatarAudioRangeReduction(
                VRCPlayerApi otherPlayer,
                float distanceBetweenPlayerHeads,
                PlayerAudioConfigurationModel configuration,
                float relativeAudioRange,
                float heightBasedMultiplier
        ) {
            float avatarDistanceFactor = GetAbsoluteRangeReduction(
                    distanceBetweenPlayerHeads,
                    relativeAudioRange,
                    configuration.AvatarFarRadius * heightBasedMultiplier
            );

            UpdateAvatarAudio(
                    otherPlayer,
                    avatarDistanceFactor * heightBasedMultiplier,
                    configuration.ForceAvatarSpatialAudio,
                    configuration.AllowAvatarCustomAudioCurves,
                    configuration.AvatarGain,
                    configuration.AvatarFarRadius * heightBasedMultiplier,
                    configuration.AvatarNearRadius * heightBasedMultiplier,
                    configuration.AvatarVolumetricRadius * heightBasedMultiplier
            );
        }

        private void ApplyAudioOverrides(
                VRCPlayerApi otherPlayer,
                Vector3 listenerHeadPosition,
                Quaternion listenerHeadRotation,
                Vector3 direction,
                float distanceBetweenPlayerHeads,
                PlayerAudioOverride playerOverride,
                Quaternion otherHeadRotation
        ) {
            float heightBasedMultiplier = playerOverride
                    .HeightToVoiceCorrelation
                    .Evaluate(otherPlayer.GetAvatarEyeHeightAsMeters());
            float relativeAudioRange = GetOverrideRelativeAudioRange(
                    listenerHeadPosition,
                    listenerHeadRotation,
                    direction,
                    distanceBetweenPlayerHeads,
                    playerOverride,
                    otherHeadRotation);
            ApplyOverrideVoiceRangeReduction(
                    otherPlayer,
                    distanceBetweenPlayerHeads,
                    playerOverride,
                    relativeAudioRange,
                    heightBasedMultiplier);
            ApplyOverrideAvatarRangeReduction(
                    otherPlayer,
                    distanceBetweenPlayerHeads,
                    playerOverride,
                    relativeAudioRange,
                    heightBasedMultiplier);
        }

        private void ApplyOverrideAvatarRangeReduction(
                VRCPlayerApi otherPlayer,
                float distanceBetweenPlayerHeads,
                PlayerAudioOverride playerOverride,
                float relativeAudioRange,
                float heightBasedMultiplier
        ) {
            float avatarDistanceFactor = GetAbsoluteRangeReduction(
                    distanceBetweenPlayerHeads,
                    relativeAudioRange,
                    playerOverride.TargetAvatarFarRadius * heightBasedMultiplier
            );

            UpdateAvatarAudio(
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

        private void ApplyOverrideVoiceRangeReduction(
                VRCPlayerApi otherPlayer,
                float distanceBetweenPlayerHeads,
                PlayerAudioOverride playerOverride,
                float relativeAudioRange,
                float heightBasedMultiplier
        ) {
            float voiceDistanceFactor = GetAbsoluteRangeReduction(
                    distanceBetweenPlayerHeads,
                    relativeAudioRange,
                    playerOverride.VoiceDistanceFar * heightBasedMultiplier
            );

            UpdateVoiceAudio(
                    otherPlayer,
                    voiceDistanceFactor,
                    playerOverride.EnableVoiceLowpass,
                    playerOverride.VoiceGain,
                    playerOverride.VoiceDistanceFar * heightBasedMultiplier,
                    playerOverride.VoiceDistanceNear * heightBasedMultiplier,
                    playerOverride.VoiceVolumetricRadius * heightBasedMultiplier
            );
        }

        private float GetOverrideRelativeAudioRange(
                Vector3 listenerHeadPosition,
                Quaternion listenerHeadRotation,
                Vector3 direction,
                float distanceBetweenPlayerHeads,
                PlayerAudioOverride playerOverride,
                Quaternion otherHeadRotation
        ) {
            float rangeReducedByOcclusion = PlayerOcclusionStrategy.GetRemainingAudioRange(
                    listenerHeadPosition,
                    direction,
                    distanceBetweenPlayerHeads,
                    1f - playerOverride.OcclusionFactor,
                    1f - playerOverride.PlayerOcclusionFactor,
                    playerOverride.OcclusionMask
            );

            float rangeReducedByDirection = CalculateDirectionality(
                    listenerHeadRotation,
                    otherHeadRotation,
                    direction,
                    playerOverride.ListenerDirectionality,
                    playerOverride.PlayerDirectionality
            );

            return rangeReducedByDirection * rangeReducedByOcclusion;
        }

        private void MutePlayer(VRCPlayerApi otherPlayer) {
            UpdateVoiceAudio(
                    otherPlayer,
                    MinRelativeAudioRange,
                    false,
                    MinRelativeAudioRange,
                    MinRelativeAudioRange,
                    MinRelativeAudioRange,
                    MinRelativeAudioRange
            );

            UpdateAvatarAudio(
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

        private int GetPendingPlayerUpdates(int playerCount) {
            if (PlayerUpdateRate == 0) {
                // this will update all players every update
                return playerCount;
            }

            // make sure at least one player gets updated and no player gets updated twice
            return Mathf.Clamp(PlayerUpdateRate, 1, playerCount);
        }


        private float GetAbsoluteRangeReduction(
                float distanceBetweenPlayerHeads,
                float relativeAudioRange,
                float maxAudibleRange
        ) {
            if (relativeAudioRange < SmallNumber) {
                return MinRelativeAudioRange;
            }

            if (PlayerCanNotBeHeard(maxAudibleRange)
                || PlayerIsOutOfHearingRange(distanceBetweenPlayerHeads, maxAudibleRange)) {
                return MaxRelativeAudioRange;
            }

            float remainingDistanceToFarRadius = maxAudibleRange - distanceBetweenPlayerHeads;
            float postOcclusionFarDistance =
                    remainingDistanceToFarRadius * relativeAudioRange + distanceBetweenPlayerHeads;
            return postOcclusionFarDistance / maxAudibleRange;
        }

        private static bool PlayerCanNotBeHeard(float maxAudibleRange) {
            return maxAudibleRange == 0;
        }

        private static bool PlayerIsOutOfHearingRange(float distanceBetweenPlayerHeads, float maxAudibleRange) {
            return maxAudibleRange < distanceBetweenPlayerHeads;
        }

        private float CalculateDirectionality(
                Quaternion listenerHeadRotation,
                Quaternion playerHeadRotation,
                Vector3 directionToPlayer,
                float listenerDirectionality,
                float playerDirectionality
        ) {
            var listenerForward = listenerHeadRotation * Vector3.forward;
            var playerBackward = playerHeadRotation * Vector3.back;

            float dotListener = 0.5f * (Vector3.Dot(listenerForward, directionToPlayer) + 1f);
            float dotSource = 0.5f * (Vector3.Dot(playerBackward, directionToPlayer) + 1f);

            return Mathf.Clamp01(dotListener + (1f - listenerDirectionality)) *
                   Mathf.Clamp01(dotSource + (1f - playerDirectionality));
        }


        private void UpdateVoiceAudio(
                VRCPlayerApi vrcPlayerApi,
                float distanceFactor,
                bool enableVoiceLowpass,
                float targetVoiceGain,
                float targetVoiceDistanceFar,
                float targetVoiceDistanceNear,
                float targetVoiceVolumetricRadius
        ) {
            if (!Utilities.IsValid(vrcPlayerApi)) {
                Error($"player invalid");
                return;
            }

            vrcPlayerApi.SetVoiceLowpass(enableVoiceLowpass);
            vrcPlayerApi.SetVoiceGain(targetVoiceGain);
            vrcPlayerApi.SetVoiceDistanceFar(targetVoiceDistanceFar * distanceFactor);
            vrcPlayerApi.SetVoiceDistanceNear(targetVoiceDistanceNear * distanceFactor);
            vrcPlayerApi.SetVoiceVolumetricRadius(targetVoiceVolumetricRadius * distanceFactor);
        }

        private void NotifyVoiceUpdateListeners(VRCPlayerApi otherPlayer) {
            if (!PlayerUpdateListeners.TryGetValue(otherPlayer.playerId, out var value)) {
                // nothing to do as no-one is listening for changes of the player
                return;
            }

            var behaviour = (TlpBaseBehaviour)value.Reference;
            if (!Utilities.IsValid(behaviour)) {
                Error($"Listener of {otherPlayer.ToStringSafe()} is no {nameof(TlpBaseBehaviour)}: Removing.");
                PlayerUpdateListeners.Remove(otherPlayer.playerId);
                return;
            }

            behaviour.OnEvent("VoiceValuesUpdate");
        }

        private void UpdateAvatarAudio(
                VRCPlayerApi vrcPlayerApi,
                float occlusion,
                bool forceAvatarSpatialAudio,
                bool allowAvatarCustomAudioCurves,
                float targetAvatarGain,
                float targetAvatarFarRadius,
                float targetAvatarNearRadius,
                float targetAvatarVolumetricRadius
        ) {
            vrcPlayerApi.SetAvatarAudioForceSpatial(forceAvatarSpatialAudio);
            vrcPlayerApi.SetAvatarAudioCustomCurve(allowAvatarCustomAudioCurves);

            vrcPlayerApi.SetAvatarAudioGain(targetAvatarGain * occlusion);
            vrcPlayerApi.SetAvatarAudioFarRadius(targetAvatarFarRadius * occlusion);
            vrcPlayerApi.SetAvatarAudioNearRadius(targetAvatarNearRadius * occlusion);
            vrcPlayerApi.SetAvatarAudioVolumetricRadius(targetAvatarVolumetricRadius);
        }

        #region internal
        #region OverridePlayerSettings
        private bool AddOverrideToNewList(PlayerAudioOverride playerAudioOverride, VRCPlayerApi playerToAffect) {
            var instance = PlayerOverrideListPool.Get();
            if (!Utilities.IsValid(instance)) {
                Error(
                        $"Failed to retrieve {nameof(PlayerAudioOverrideList)} instance from {PlayerOverrideListPool.GetScriptPathInScene()}");
                return false;
            }

            var createdOverrideList = instance.GetComponent<PlayerAudioOverrideList>();
            if (!Utilities.IsValid(createdOverrideList)) {
                Error(
                        $"{nameof(PlayerAudioOverrideList)} component missing object retrieved from {PlayerOverrideListPool.GetScriptPathInScene()}");
                PlayerOverrideListPool.Return(instance);
                return false;
            }

            if (!createdOverrideList.AddOverride(playerAudioOverride)) {
                Error(
                        $"Failed to add {playerAudioOverride.GetScriptPathInScene()} to created list of {playerToAffect.ToStringSafe()}");
                PlayerOverrideListPool.Return(instance);
                return false;
            }

            PlayersToOverride.Add(playerToAffect.playerId, createdOverrideList);
            return true;
        }

        private bool AddOverrideToExistingList(
                PlayerAudioOverride playerAudioOverride,
                VRCPlayerApi playerToAffect,
                PlayerAudioOverrideList existingList
        ) {
            if (existingList.Contains(playerAudioOverride)) {
                existingList.RemoveOverride(playerAudioOverride);
            }

            if (existingList.AddOverride(playerAudioOverride)) {
                return true;
            }

            Error(
                    $"Failed to add {playerAudioOverride.GetScriptPathInScene()} to {existingList.GetScriptPathInScene()} of {playerToAffect.ToStringSafe()}");
            return false;
        }
        #endregion

        internal void ResetAllPlayerVoices() {
            for (int i = 0; i < ExistingRemotePlayerIds.Count;) {
                int playerId = ExistingRemotePlayerIds[i].Int;
                var playerApi = playerId.IdToVrcPlayer();
                if (!Utilities.IsValid(playerApi)) {
                    // this is not supposed to happen as Players should be removed by OnPlayerLeft before they become invalid
                    Error($"Player {playerId} at position {i} has unexpectedly become invalid, deleting entry");
                    ExistingRemotePlayerIds.RemoveAt(i);
                    continue;
                }

                UpdateVoiceAudio(playerApi, 1f, true, 15f, 25, 0, 0);
                i++;
            }
        }

        internal void EnableCurrentReverbSettings() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(EnableCurrentReverbSettings));
#endif
            #endregion

            var localOverride = GetMaxPriorityOverride(Networking.LocalPlayer);
            if (Utilities.IsValid(localOverride)) {
                UseReverbSettings(localOverride.OptionalReverb);
            }
        }

        internal PlayerAudioOverride UpdateAudioFilters(
                PlayerAudioOverride newOverride,
                PlayerAudioOverride oldOverride
        ) {
            if (ReferenceEquals(newOverride, oldOverride)) {
                return newOverride;
            }

            if (Utilities.IsValid(newOverride)) {
                UseReverbSettings(newOverride.OptionalReverb);
                return newOverride;
            }

            UseReverbSettings(null);
            return null;
        }

        public override void OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnEvent)} {eventName}");
#endif
            #endregion

            if (!IsControllerInitialized) {
                Error($"{nameof(OnEvent)}: MVC not initialized");
                return;
            }

            switch (eventName) {
                case nameof(OnModelChanged):
                    OnModelChanged();

                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }

        private void OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnModelChanged));
#endif
            #endregion

            if (!Utilities.IsValid(LocalConfiguration)) {
                Error($"{nameof(LocalConfiguration)} invalid");
                return;
            }

            if (!Utilities.IsValid(SyncedMasterConfiguration)) {
                Error($"{nameof(SyncedMasterConfiguration)} invalid");
                return;
            }

            _currentConfiguration = !Networking.IsMaster && LocalConfiguration.AllowMasterControlLocalValues
                    ? SyncedMasterConfiguration
                    : LocalConfiguration;
        }

        private bool AreMandatoryReferencesValid() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(AreMandatoryReferencesValid));
#endif
            #endregion

            if (!Utilities.IsValid(LocalConfiguration)) {
                Error($"{nameof(LocalConfiguration)} invalid");
                return false;
            }

            if (Utilities.IsValid(SyncedMasterConfiguration)) {
                return true;
            }

            Error($"{nameof(SyncedMasterConfiguration)} invalid");
            return false;
        }
        #endregion

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnOwnershipTransferred)} to '{player.DisplayNameSafe()}'");
#endif
            #endregion

            if (!HasStartedOk) {
                Error($"{nameof(OnOwnershipTransferred)}: Not initialized");
                return;
            }

            if (!IsControllerInitialized) {
                Error($"{nameof(OnOwnershipTransferred)}: MVC not initialized");
                return;
            }

            if (!Utilities.IsValid(LocalConfiguration)) {
                Error($"{nameof(LocalConfiguration)} is invalid");
                return;
            }

            LocalConfiguration.Dirty = true;
            LocalConfiguration.NotifyIfDirty(1);
        }

        #region Player Events
        public override void OnPlayerJoined(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerJoined)} '{player.DisplayNameSafe()}'");
#endif
            #endregion

            if (!Utilities.IsValid(player)) {
                Error("Invalid player joined");
                return;
            }

            if (player.isLocal) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog("Local player joined and is being ignored");
#endif
                #endregion

                return;
            }

            if (ExistingRemotePlayerIds.Contains(player.playerId)) {
                Warn($"{player.ToStringSafe()} already exists!");
                return;
            }

            ExistingRemotePlayerIds.Add(new DataToken(player.playerId));

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{player.ToStringSafe()} added to {nameof(ExistingRemotePlayerIds)}");
#endif
            #endregion
        }

        public override void OnPlayerLeft(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerLeft)} '{player.DisplayNameSafe()}'");
#endif
            #endregion

            if (player.IsLocalSafe()) {
                // don't bother doing anything
                return;
            }

            if (!HasStartedOk) {
                Error($"{nameof(OnPlayerLeft)}: Not initialized");
                return;
            }

            if (Utilities.IsValid(player)) {
                ExistingRemotePlayerIds.RemoveAll(player.playerId);
            }

            if (!IsControllerInitialized) {
                Error($"{nameof(OnPlayerLeft)}: MVC not initialized");
                return;
            }

            PurgeOverridesOfPlayer(player.PlayerIdSafe());
            PurgeOverridesOfInvalidPlayers();

            if (!Utilities.IsValid(LocalConfiguration)) {
                Error($"{nameof(LocalConfiguration)} is invalid");
                return;
            }

            LocalConfiguration.Dirty = true;
            LocalConfiguration.NotifyIfDirty(1);
        }
        #endregion


        #region Player Checks
        /// <param name="player"></param>
        /// <returns>True when a player is not ignored and is affected only by global effects</returns>
        public bool UsesDefaultEffects(VRCPlayerApi player) {
            if (!HasStartedOk) {
                Error($"{nameof(UsesDefaultEffects)}: Not initialized");
                return false;
            }

            if (IsControllerInitialized) {
                return !IgnoredPlayers.Contains(player.PlayerIdSafe()) && !HasActiveVoiceOverrides(player);
            }

            Error($"{nameof(UsesDefaultEffects)}: MVC not initialized");
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true when a player is not affected global nor by override effects</returns>
        public bool IsIgnored(VRCPlayerApi player) {
            if (!HasStartedOk) {
                Error($"{nameof(IsIgnored)}: Not initialized");
                return false;
            }

            if (IsControllerInitialized) {
                return IgnoredPlayers.Contains(player.PlayerIdSafe());
            }

            Error($"{nameof(IsIgnored)}: MVC not initialized");
            return false;
        }

        /// <param name="player"></param>
        /// <returns>true if the player is not ignored and is assigned to at least one override</returns>
        public bool UsesVoiceOverride(VRCPlayerApi player) {
            if (!HasStartedOk) {
                Error($"{nameof(UsesVoiceOverride)}: Not initialized");
                return false;
            }

            if (IsControllerInitialized) {
                return !IgnoredPlayers.Contains(player.PlayerIdSafe()) && HasActiveVoiceOverrides(player);
            }

            Error($"{nameof(UsesVoiceOverride)}: MVC not initialized");
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if the player is assigned to at least one override</returns>
        public bool HasActiveVoiceOverrides(VRCPlayerApi player) {
            if (!HasStartedOk) {
                Error($"{nameof(HasActiveVoiceOverrides)}: Not initialized");
                return false;
            }

            if (!IsControllerInitialized) {
                Error($"{nameof(HasActiveVoiceOverrides)}: MVC not initialized");
                return false;
            }

            if (!Utilities.IsValid(player)) {
                Error($"{nameof(player)} invalid");
                return false;
            }

            if (player.isLocal) {
                return Utilities.IsValid(LocalPlayerOverrideList.GetMaxPriority(player));
            }

            if (!PlayersToOverride.TryGetValue(player.playerId, out var list)) {
                return false;
            }

            var overrideList = (PlayerAudioOverrideList)list.Reference;
            return Utilities.IsValid(overrideList) && Utilities.IsValid(overrideList.GetMaxPriority(player));
        }
        #endregion


        #region Public
        /// <summary>
        /// local player not included if it has an override
        /// </summary>
        /// <returns>copy of array of player ids or empty array</returns>
        public DataList GetNonLocalPlayersWithOverrides() {
#if TLP_DEBUG
            DebugLog(nameof(GetNonLocalPlayersWithOverrides));
#endif
            if (!HasStartedOk) {
                Error($"{nameof(HasActiveVoiceOverrides)}: Not initialized");
                return new DataList();
            }


            if (IsControllerInitialized) {
                return PlayersToOverride.GetKeys();
            }

            Error($"{nameof(GetNonLocalPlayersWithOverrides)}: MVC not initialized");
            return new DataList();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="audioReverbFilter">Clears the reverb settings when null, otherwise copies and applies the values</param>
        internal void UseReverbSettings(AudioReverbFilter audioReverbFilter) {
#if TLP_DEBUG
            DebugLog(nameof(UseReverbSettings));
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
        protected override bool InitializeInternal() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InitializeInternal));
#endif
            #endregion

            if (!Utilities.IsValid(PlayerOverrideListPool)) {
                Error($"{nameof(PlayerOverrideListPool)} is not set");
                return false;
            }

            if (!Utilities.IsValid(DefaultConfiguration)) {
                Error($"{nameof(DefaultConfiguration)} is not set");
                return false;
            }

            if (!Utilities.IsValid(PlayerOcclusionStrategy)) {
                Error($"{nameof(PlayerOcclusionStrategy)} is not set");
                return false;
            }

            if (!Utilities.IsValid(IgnoredPlayers)) {
                Error($"{nameof(IgnoredPlayers)} is not set");
                return false;
            }

            if (!Utilities.IsValid(MainAudioReverbFilter)) {
                Error($"{nameof(MainAudioReverbFilter)} is not set");
                return false;
            }

            return base.InitializeInternal();
        }

        internal static string ExpectedGameObjectName() {
            return $"TLP_{nameof(PlayerAudioController)}";
        }

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            string expectedName = ExpectedGameObjectName();
            if (gameObject.name != expectedName) {
                Warn($"Changing name of GameObject '{transform.GetPathInScene()}' to '{expectedName}'");
                gameObject.name = expectedName;
            }

            if (!AreMandatoryReferencesValid()) {
                ErrorAndDisableComponent($"Setup incomplete");
                return false;
            }

            if (!IsControllerInitialized && !ValidateAndSetupMvc()) {
                ErrorAndDisableComponent($"MVC setup failed");
                return false;
            }

            EnableCurrentReverbSettings();
            return true;
        }
        #endregion
        
        #region Privacy Channel Helper Methods
        /// <summary>
        /// Check if a DataList contains any privacy channels
        /// </summary>
        /// <param name="privacyChannels">The DataList to check</param>
        /// <returns>true if the list has privacy channels</returns>
        private static bool HasPrivacyChannels(DataList privacyChannels) {
            return privacyChannels != null && privacyChannels.Count > 0;
        }

        /// <summary>
        /// Check if two DataLists share any privacy channel IDs
        /// </summary>
        /// <param name="channelsA">First list of privacy channels</param>
        /// <param name="channelsB">Second list of privacy channels</param>
        /// <returns>true if they share any channel IDs</returns>
        private static bool SharesAnyPrivacyChannels(DataList channelsA, DataList channelsB) {
            if (!HasPrivacyChannels(channelsA) || !HasPrivacyChannels(channelsB)) 
                return false;
            
            for (int i = 0; i < channelsA.Count; i++) {
                int channelA = channelsA[i].Int;
                for (int j = 0; j < channelsB.Count; j++) {
                    if (channelsB[j].Int == channelA)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a privacy channel list contains a specific channel ID
        /// </summary>
        /// <param name="privacyChannels">The list to check</param>
        /// <param name="channelId">The channel ID to look for</param>
        /// <returns>true if the channel ID is found</returns>
        private static bool ContainsPrivacyChannel(DataList privacyChannels, int channelId) {
            if (!HasPrivacyChannels(privacyChannels)) 
                return false;
            
            for (int i = 0; i < privacyChannels.Count; i++) {
                if (privacyChannels[i].Int == channelId)
                    return true;
            }
            return false;
        }
        #endregion
    }
}