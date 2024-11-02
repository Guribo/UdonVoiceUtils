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
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerAudioController), ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlayerAudioController : Controller
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerOcclusionStrategy.ExecutionOrder + 10;

        #region General settings
        [Header("General settings")]
        public const int ChannelNoPrivacy = -1;

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

            if (!AreMandatoryReferencesValid()) {
                ErrorAndDisableComponent($"Setup incomplete");
                return;
            }

            if (!ValidateAndSetupMvc()) {
                ErrorAndDisableComponent($"MVC setup failed");
                return;
            }

            EnableCurrentReverbSettings();
        }

        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (!Initialized) {
                Warn("Not initialized, nothing to do");
                return;
            }

            UseReverbSettings(null);
            ResetAllPlayerVoices();

            if (!Menu.DeInitialize()) {
                Error($"DeInitialization of {nameof(Menu)} failed");
            }

            if (!DeInitialize()) {
                Error($"DeInitialization of {GetUdonTypeName()} failed");
            }

            if (LocalConfiguration.Initialized && !LocalConfiguration.DeInitialize()) {
                Error($"DeInitialization of {nameof(LocalConfiguration)} failed");
            }
        }

        public override void PostLateUpdate() {
            if (!Initialized) {
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
            PlayerUpdateListeners[playerId] = listener;
        }

        public bool RemovePlayerUpdateListener(int playerId) {
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

            if (!Initialized) {
                Error("Not initialized");
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

            if (!Initialized) {
                Error("Not initialized");
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
            if (!Initialized) {
                Error("Not initialized");
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
            if (!Initialized) {
                Error("Not initialized");
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

                Warn(
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

            if (!Initialized) {
                Error("Not initialized");
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
            if (!LocalConfiguration.Initialized &&
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

            if (!SyncedMasterConfiguration.Initialized &&
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

            if (!Utilities.IsValid(Menu) || Menu.Initialized) {
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

            int privacyChannelId = ChannelNoPrivacy;
            bool muteOutsiders = false;
            bool disallowListeningToChannel = false;

            if (Utilities.IsValid(_localOverride)) {
                privacyChannelId = _localOverride.PrivacyChannelId;
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
            } else {
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
            } else {
                // create a fake head position/rotation (no pitch and roll)
                var avatarRootRotation = otherPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).rotation;
                var playerUp = avatarRootRotation * Vector3.up;

                otherHeadRotation = avatarRootRotation;
                otherHeadPosition = otherPlayer.GetPosition() + playerUp * otherPlayer.GetAvatarEyeHeightAsMeters();
            }

            var listenerToPlayer = otherHeadPosition - listenerHeadPosition;
            var direction = listenerToPlayer.normalized;
            float distance = listenerToPlayer.magnitude;

            bool localPlayerInPrivateChannel = privacyChannelId != ChannelNoPrivacy;
            if (Utilities.IsValid(playerOverride)) {
                if (OtherPlayerWithOverrideCanBeHeard(
                            playerOverride,
                            localPlayerInPrivateChannel,
                            privacyChannelId,
                            muteOutsiders,
                            disallowListeningToChannel
                    )) {
                    ApplyAudioOverrides(
                            otherPlayer,
                            listenerHeadPosition,
                            listenerHeadRotation,
                            direction,
                            distance,
                            playerOverride,
                            otherHeadRotation
                    );
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
                    distance,
                    otherHeadRotation,
                    _currentConfiguration,
                    LocalConfiguration.HeightToVoiceCorrelation
            );
        }


        internal static bool OtherPlayerWithOverrideCanBeHeard(
                PlayerAudioOverride playerOverride,
                bool localPlayerInPrivateChannel,
                int currentPrivacyChannel,
                bool muteOutsiders,
                bool disallowLocalPlayerListening
        ) {
            // ReSharper disable once PossibleNullReferenceException (invalid warning because of IsValid check)
            bool playerInSamePrivateChannel = playerOverride.PrivacyChannelId == currentPrivacyChannel;
            bool playerInSamePrivateChannelAllowedToBeHeard =
                    playerInSamePrivateChannel && !disallowLocalPlayerListening;
            bool otherPlayerNotInAnyPrivateChannel = playerOverride.PrivacyChannelId == ChannelNoPrivacy;
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
                float distance,
                Quaternion otherPlayerHeadRotation,
                PlayerAudioConfigurationModel configuration,
                AnimationCurve heightToVoiceCorrelation
        ) {
            float avatarEyeHeightAsMeters = otherPlayer.GetAvatarEyeHeightAsMeters();

            float heightBasedMultiplier = heightToVoiceCorrelation.Evaluate(avatarEyeHeightAsMeters);

            float occlusionFactor = PlayerOcclusionStrategy.CalculateOcclusion(
                    listenerHeadPosition,
                    direction,
                    distance,
                    1f - configuration.OcclusionFactor,
                    1f - configuration.PlayerOcclusionFactor,
                    configuration.OcclusionMask
            );

            float directionality = CalculateDirectionality(
                    listenerHeadRotation,
                    otherPlayerHeadRotation,
                    direction,
                    configuration.ListenerDirectionality,
                    configuration.PlayerDirectionality
            );

            float distanceReduction = directionality * occlusionFactor;

            float voiceDistanceFactor = CalculateRangeReduction(
                    distance,
                    distanceReduction,
                    configuration.VoiceDistanceFar
            );

            UpdateVoiceAudio(
                    otherPlayer,
                    voiceDistanceFactor * heightBasedMultiplier,
                    configuration.EnableVoiceLowpass,
                    configuration.VoiceGain,
                    configuration.VoiceDistanceFar,
                    configuration.VoiceDistanceNear,
                    configuration.VoiceVolumetricRadius
            );

            float avatarDistanceFactor = CalculateRangeReduction(
                    distance,
                    distanceReduction,
                    configuration.AvatarFarRadius
            );

            UpdateAvatarAudio(
                    otherPlayer,
                    avatarDistanceFactor * heightBasedMultiplier,
                    configuration.ForceAvatarSpatialAudio,
                    configuration.AllowAvatarCustomAudioCurves,
                    configuration.AvatarGain,
                    configuration.AvatarFarRadius,
                    configuration.AvatarNearRadius,
                    configuration.AvatarVolumetricRadius
            );
        }

        private void ApplyAudioOverrides(
                VRCPlayerApi otherPlayer,
                Vector3 listenerHeadPosition,
                Quaternion listenerHeadRotation,
                Vector3 direction,
                float distance,
                PlayerAudioOverride playerOverride,
                Quaternion otherHeadRotation
        ) {
            float heightBasedMultiplier = playerOverride
                    .HeightToVoiceCorrelation
                    .Evaluate(otherPlayer.GetAvatarEyeHeightAsMeters());

            float occlusionFactor = PlayerOcclusionStrategy.CalculateOcclusion(
                    listenerHeadPosition,
                    direction,
                    distance,
                    1f - playerOverride.OcclusionFactor,
                    1f - playerOverride.PlayerOcclusionFactor,
                    playerOverride.OcclusionMask
            );

            float directionality = CalculateDirectionality(
                    listenerHeadRotation,
                    otherHeadRotation,
                    direction,
                    playerOverride.ListenerDirectionality,
                    playerOverride.PlayerDirectionality
            );

            float distanceReduction = directionality * occlusionFactor;

            float voiceDistanceFactor = CalculateRangeReduction(
                    distance,
                    distanceReduction,
                    playerOverride.VoiceDistanceFar
            );

            UpdateVoiceAudio(
                    otherPlayer,
                    voiceDistanceFactor * heightBasedMultiplier,
                    playerOverride.EnableVoiceLowpass,
                    playerOverride.VoiceGain,
                    playerOverride.VoiceDistanceFar,
                    playerOverride.VoiceDistanceNear,
                    playerOverride.VoiceVolumetricRadius
            );

            float avatarDistanceFactor = CalculateRangeReduction(
                    distance,
                    distanceReduction,
                    playerOverride.TargetAvatarFarRadius
            );

            UpdateAvatarAudio(
                    otherPlayer,
                    avatarDistanceFactor * heightBasedMultiplier,
                    playerOverride.ForceAvatarSpatialAudio,
                    playerOverride.AllowAvatarCustomAudioCurves,
                    playerOverride.TargetAvatarGain,
                    playerOverride.TargetAvatarFarRadius,
                    playerOverride.TargetAvatarNearRadius,
                    playerOverride.TargetAvatarVolumetricRadius
            );
        }

        private void MutePlayer(VRCPlayerApi otherPlayer) {
            UpdateVoiceAudio(
                    otherPlayer,
                    0f,
                    false,
                    0f,
                    0f,
                    0f,
                    0f
            );

            UpdateAvatarAudio(
                    otherPlayer,
                    0,
                    false,
                    false,
                    0f,
                    0f,
                    0f,
                    0f
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


        private float CalculateRangeReduction(float distance, float distanceReduction, float maxAudibleRange) {
            if (maxAudibleRange <= 0f || Mathf.Abs(distanceReduction - 1f) < 0.01f) {
                return 1f;
            }

            float remainingDistanceToFarRadius = maxAudibleRange - distance;
            bool playerCouldBeAudible = remainingDistanceToFarRadius > 0f;

            float occlusion = 1f;
            if (playerCouldBeAudible) {
                float postOcclusionFarDistance = remainingDistanceToFarRadius * distanceReduction + distance;
                occlusion = postOcclusionFarDistance / maxAudibleRange;
            }

            return occlusion;
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

            if (!PlayerUpdateListeners.TryGetValue(vrcPlayerApi.playerId, out var value)) {
                // nothing to do as no-one is listening for changes of the player
                return;
            }

            var behaviour = (TlpBaseBehaviour)value.Reference;
            if (!Utilities.IsValid(behaviour)) {
                Error($"Listener of {vrcPlayerApi.ToStringSafe()} is no {nameof(TlpBaseBehaviour)}: Removing.");
                PlayerUpdateListeners.Remove(vrcPlayerApi.playerId);
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

            if (!Initialized) {
                Error("Not initialized");
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

            if (!Initialized) {
                Error("Not initialized");
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


            if (Utilities.IsValid(player)) {
                ExistingRemotePlayerIds.RemoveAll(player.playerId);
            }

            if (!Initialized) {
                Error("Not initialized");
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
            if (Initialized) {
                return !IgnoredPlayers.Contains(player.PlayerIdSafe()) && !HasActiveVoiceOverrides(player);
            }

            Error("Not initialized");
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true when a player is not affected global nor by override effects</returns>
        public bool IsIgnored(VRCPlayerApi player) {
            if (Initialized) {
                return IgnoredPlayers.Contains(player.PlayerIdSafe());
            }

            Error("Not initialized");
            return false;
        }

        /// <param name="player"></param>
        /// <returns>true if the player is not ignored and is assigned to at least one override</returns>
        public bool UsesVoiceOverride(VRCPlayerApi player) {
            if (Initialized) {
                return !IgnoredPlayers.Contains(player.PlayerIdSafe()) && HasActiveVoiceOverrides(player);
            }

            Error("Not initialized");
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if the player is assigned to at least one override</returns>
        public bool HasActiveVoiceOverrides(VRCPlayerApi player) {
            if (!Initialized) {
                Error("Not initialized");
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
            if (Initialized) {
                return PlayersToOverride.GetKeys();
            }

            Error("Not initialized");
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

            if (Initialized) {
                return true;
            }

            Error("MVC setup is incomplete");
            return false;
        }
        #endregion
    }
}