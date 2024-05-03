using System;
using JetBrains.Annotations;
using TLP.UdonUtils;
using TLP.UdonUtils.DesignPatterns.MVC;
using TLP.UdonUtils.Events;
using TLP.UdonUtils.Extensions;
using TLP.UdonUtils.Runtime.Pool;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerAudioController : Controller
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VoiceUtilsExecutionOrder.AudioStart;


        #region General settings
        [Header("General settings")]
        /// <summary>
        /// How long to wait after start before applying changes to all players.
        /// Prevents excessive volume on joining the world because player positions might not be up to date yet.
        /// </summary>
        [Tooltip(
                "How long to wait after start before applying changes to all players."
                + " Prevents excessive volume on joining the world because player positions "
                + "might not be up to date yet."
        )]
        [SerializeField]
        [Range(0, 10f)]
        protected float StartDelay = 10f;

        public const int ChannelNoPrivacy = -1;
        private const int MinSupportedPlayers = 2;

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
        #endregion

        #region Optional
        [Tooltip("Add a Dummy View component to the GameObject and reference it here if you don't use the Menu")]
        [SerializeField]
        internal View OptionalView;
        #endregion

        public PlayerAudioConfigurationModel LocalConfiguration;

        public PlayerAudioConfigurationModel SyncedMasterConfiguration;
        public PlayerAudioConfigurationModel DefaultConfiguration;
        private PlayerAudioConfigurationModel _currentConfiguration;

        #region State
        private bool _receivedStart;
        internal bool CanUpdate;
        private int _playerIndex;
        private VRCPlayerApi[] _players = new VRCPlayerApi[1];
        private int[] _playersToIgnore;
        internal int[] PlayersToOverride = new int[0];
        private PlayerAudioOverrideList[] _playerOverrideLists;
        private readonly RaycastHit[] _rayHits = new RaycastHit[2];
        private int _serializationRequests;
        private PlayerAudioOverride _localOverride;
        private VRCPlayerApi _localPlayer;
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

            if (_receivedStart) {
                // don't wait for all players to load as they should be all loaded already
                CanUpdate = true;
            }

            EnableCurrentReverbSettings();
        }


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

            return base.InitializeInternal();
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

            _currentConfiguration = LocalConfiguration;
            if (!Initialize(_currentConfiguration, OptionalView)) {
                ErrorAndDisableComponent($"Init of {GetUdonTypeName()} failed");
                return false;
            }

            if (!Utilities.IsValid(OptionalView) || OptionalView.Initialized) {
                ErrorAndDisableComponent($"{nameof(OptionalView)} is already initialized");
                return false;
            }

            if (!OptionalView.Initialize(this, _currentConfiguration)) {
                ErrorAndDisableComponent($"Init of {nameof(OptionalView)} failed");
                return false;
            }

            return true;
        }

        public override void Start() {
            base.Start();

            OneTimeSetup();

            EnableProcessingDelayed(StartDelay);
        }

        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            #endregion

            UseReverbSettings(null);
            ResetAllPlayerVoices();

            if (Utilities.IsValid(OptionalView)) {
                if (!OptionalView.DeInitialize()) {
                    Error($"DeInitialization of {nameof(OptionalView)} failed");
                }
            }

            if (!DeInitialize()) {
                Error($"DeInitialization of {GetUdonTypeName()} failed");
            }

            if (LocalConfiguration.Initialized && !LocalConfiguration.DeInitialize()) {
                Error($"DeInitialization of {nameof(LocalConfiguration)} failed");
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


        public override void PostLateUpdate() {
            if (!_currentConfiguration) {
                return;
            }

            if (!CanUpdate) {
                return;
            }

            if (!Utilities.IsValid(_localPlayer)) {
                _localPlayer = Networking.LocalPlayer;
                if (!Utilities.IsValid(_localPlayer)) {
                    ErrorAndDisableGameObject($"Local player is not valid");
                    return;
                }
            }

            var newLocalOverride = LocalPlayerOverrideList.GetMaxPriority(_localPlayer);
            _localOverride = UpdateAudioFilters(newLocalOverride, _localOverride);

            int playerCount = UpdatePlayerList();
            if (playerCount < MinSupportedPlayers) {
                return;
            }

            int pendingPlayerUpdates = GetPendingPlayerUpdates(playerCount);
            for (int playerUpdate = 0; playerUpdate < pendingPlayerUpdates; ++playerUpdate) {
                var otherPlayer = GetNextPlayer(playerCount);
                if (!Utilities.IsValid(otherPlayer)) {
                    // this should never be the case!!!
                    continue;
                }

                UpdateOtherPlayer(_localPlayer, otherPlayer);
            }
        }
        #endregion

        public void EnableProcessingDelayed(float delay) {
            SendCustomEventDelayedSeconds(nameof(EnableProcessing), delay);
        }

        public void EnableProcessing() {
            if (!(Utilities.IsValid(this)
                  && Utilities.IsValid(gameObject))
                && gameObject.activeInHierarchy) {
                // do nothing if the behaviour is not alive/valid/active
                return;
            }

            CanUpdate = true;
        }

        internal void UpdateOtherPlayer(VRCPlayerApi localPlayer, VRCPlayerApi otherPlayer) {
            if (!Utilities.IsValid(otherPlayer)
                || otherPlayer.playerId == localPlayer.playerId
                || PlayerIsIgnored(otherPlayer)) {
                return;
            }

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


        public static bool OtherPlayerWithOverrideCanBeHeard(
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

            float occlusionFactor = CalculateOcclusion(
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

            float occlusionFactor = CalculateOcclusion(
                    listenerHeadPosition,
                    direction,
                    distance,
                    playerOverride.OcclusionFactor,
                    playerOverride.PlayerOcclusionFactor,
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

        private bool PlayerIsIgnored(VRCPlayerApi vrcPlayerApi) {
            if (_playersToIgnore == null || !Utilities.IsValid(vrcPlayerApi)) {
                return false;
            }

            return Array.BinarySearch(_playersToIgnore, vrcPlayerApi.playerId) > -1;
        }

        private int GetPendingPlayerUpdates(int playerCount) {
            if (PlayerUpdateRate == 0) {
                // this will update all players every update
                return playerCount;
            }

            // make sure at least one player gets updated and no player gets updated twice
            return Mathf.Clamp(PlayerUpdateRate, 1, playerCount);
        }


        /// <summary>
        /// initializes all runtime variables using the default values.
        /// Has no effect if called again or if start() was already received.
        /// To reset values use ResetToDefault() instead.
        /// </summary>
        public void OneTimeSetup() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OneTimeSetup));
#endif
            #endregion


            if (_receivedStart) {
                return;
            }

            _receivedStart = true;
            _playerOverrideLists = new PlayerAudioOverrideList[0];
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="listenerHead"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <param name="occlusionFactor"></param>
        /// <param name="playerOcclusionFactor"></param>
        /// <param name="playerOcclusionMask"></param>
        /// <returns>multiplier in range 0 to 1</returns>
        internal float CalculateOcclusion(
                Vector3 listenerHead,
                Vector3 direction,
                float distance,
                float occlusionFactor,
                float playerOcclusionFactor,
                int playerOcclusionMask
        ) {
            int hits = Physics.RaycastNonAlloc(
                    listenerHead,
                    direction,
                    _rayHits,
                    distance,
                    playerOcclusionMask
            );

            if (hits == 0) {
                // nothing to do

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog("No hits");
#endif
                #endregion

                return 1f;
            }

            // if the UI layer is used for occlusion (UI layer contains the player capsules) allow at least one hit
            bool playersCanOcclude = (playerOcclusionMask | PlayerAudioConfigurationModel.UILayerMask) > 0;
            if (!playersCanOcclude) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Players can't occlude: {hits}");
#endif
                #endregion

                // result when players can't occlude other players
                return hits > 0 ? occlusionFactor : 1f;
            }

            if (hits < 2) {
                // sometimes the other player's head leaves it's own UI player capsule which causes
                // the number of hits to go down by 1
                // or there was no environment hit while the player UI capsule was hit

                // check how far away the hit is from the player and if it is above a certain threshold
                // assume an object occludes the player (threshold is 1m for now)
                // TODO find a solution that also works for taller avatars for which the radius of the capsule can exceed 1m
                float minOcclusionTriggerDistance = distance - 1f;
                bool occlusionTriggered = _rayHits[0].distance < minOcclusionTriggerDistance;
                if (!occlusionTriggered) {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    DebugLog($"occlusion not triggered");
#endif
                    #endregion

                    return 1f;
                }

                // if the transform of the hit is not null (due to filtering of player objects by UDON)
                // then the environment got hit and we use regular occlusion values
                return _rayHits[0].transform ? occlusionFactor : playerOcclusionFactor;
            }

            // more then 1 hit indicates the ray hit another player first or hit the environment
            // _rayHits contains 2 valid hits now (not ordered by distance!!!
            // see https://docs.unity3d.com/ScriptReference/Physics.RaycastNonAlloc.html)

            // if in both of the hits the transform is now null (due to filtering of player objects by UDON)
            // this indicates that another player occluded the emitting player we ray casted to.
            bool anotherPlayerOccludes = !_rayHits[0].transform && !_rayHits[1].transform;
            if (anotherPlayerOccludes) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"anotherPlayerOccludes");
#endif
                #endregion

                return playerOcclusionFactor;
            }

            // just return the occlusion factor for everything else
            return occlusionFactor;
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

            behaviour.SetProgramVariable("VoiceLowpass", enableVoiceLowpass);
            behaviour.SetProgramVariable("VoiceGain", targetVoiceGain);
            behaviour.SetProgramVariable("VoiceDistanceFar", targetVoiceDistanceFar * distanceFactor);
            behaviour.SetProgramVariable("VoiceDistanceNear", targetVoiceDistanceNear * distanceFactor);
            behaviour.SetProgramVariable("VoiceVolumetricRadius", targetVoiceVolumetricRadius * distanceFactor);
            behaviour.SendCustomEvent("VoiceValuesUpdate");
        }

        [NonSerialized]
        public DataDictionary PlayerUpdateListeners = new DataDictionary();


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

        /// <summary>
        /// updates the player array for iteration
        /// </summary>
        /// <returns>current player count which can be less then the player array length</returns>
        private int UpdatePlayerList() {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (_players.LengthSafe() != playerCount) {
                _players = new VRCPlayerApi[playerCount];
            }

            VRCPlayerApi.GetPlayers(_players);

            return playerCount;
        }


        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnOwnershipTransferred)} to '{player.DisplayNameSafe()}'");
#endif
            #endregion


            if (!Utilities.IsValid(LocalConfiguration)) {
                Error($"{nameof(LocalConfiguration)} is invalid");
                return;
            }

            LocalConfiguration.Dirty = true;
            LocalConfiguration.NotifyIfDirty(1);
        }

        public override void OnPlayerLeft(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerLeft)} '{player.DisplayNameSafe()}'");
#endif
            #endregion

            if (!Utilities.IsValid(LocalConfiguration)) {
                Error($"{nameof(LocalConfiguration)} is invalid");
                return;
            }

            LocalConfiguration.Dirty = true;
            LocalConfiguration.NotifyIfDirty(1);
        }


        /// <summary>
        /// Add a player that shall not be affected by any effect of this script. Upon adding the player
        /// all values of the player will be set to the currently defined values on this script.
        /// Occlusion and directionality effects are reverted if the player was affected.
        /// Multiple calls have no further effect.
        /// Providing an invalid player has no effect.
        /// The ignored players are internally kept in a sorted array (ascending by player id) which is cleaned up every
        /// time a player is successfully added or removed.
        /// This function is local only.
        /// </summary>
        /// <param name="playerToIgnore"></param>
        /// <returns>true on success, false if argument invalid or player doesn't exist</returns>
        public bool IgnorePlayer(VRCPlayerApi playerToIgnore) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(IgnorePlayer)} {playerToIgnore.ToStringSafe()}");
#endif
            #endregion

            // validate the player
            if (!Utilities.IsValid(playerToIgnore)) {
                Error($"{nameof(playerToIgnore)} invalid");
                return false;
            }

            var vrcPlayerApi = VRCPlayerApi.GetPlayerById(playerToIgnore.playerId);
            if (!Utilities.IsValid(vrcPlayerApi)) {
                Error($"player doesn't exist");
                return false;
            }

            bool noPlayerIgnoredYet = _playersToIgnore == null || _playersToIgnore.Length < 1;
            if (noPlayerIgnoredYet) {
                // simply add the player and return
                _playersToIgnore = new[]
                {
                        vrcPlayerApi.playerId
                };
                return true;
            }

            // make sure all contained players are still alive, otherwise remove them
            int validPlayers = 0;
            int[] stillValidIgnoredPlayers = new int[_playersToIgnore.Length];
            bool playerAdded = false;

            foreach (int playerId in _playersToIgnore) {
                if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(playerId))) {
                    // skip (=remove) the player
                    continue;
                }

                // keep all valid players
                stillValidIgnoredPlayers[validPlayers] = playerId;
                ++validPlayers;

                // keep track if the player is already in the array while validating all players in the array
                bool playerIsAlreadyIgnored = playerId == vrcPlayerApi.playerId;
                if (playerIsAlreadyIgnored) {
                    playerAdded = true;
                    continue;
                }

                // insert the new player at the current position if the insert position is found
                bool insertPositionFound = playerId < vrcPlayerApi.playerId && !playerAdded;
                if (!insertPositionFound) {
                    continue;
                }

                int[] longerStillValidIgnoredPlayers = new int[stillValidIgnoredPlayers.Length + 1];
                stillValidIgnoredPlayers.CopyTo(longerStillValidIgnoredPlayers, 0);
                stillValidIgnoredPlayers = longerStillValidIgnoredPlayers;
                stillValidIgnoredPlayers[validPlayers] = vrcPlayerApi.playerId;
                ++validPlayers;
            }

            // shrink the validated array content (happens when ignored players have left the world)
            // and store it again in the old array
            _playersToIgnore = new int[validPlayers];
            for (int i = 0; i < validPlayers; i++) {
                _playersToIgnore[i] = stillValidIgnoredPlayers[i];
            }

            return true;
        }

        /// <summary>
        /// Remove a player from the ignore list and let it be affected again by this script.
        /// The ignored players are internally kept in a sorted array (ascending by player id) which is cleaned up every
        /// time a player is removed.
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

            if (!Utilities.IsValid(ignoredPlayer)) {
                Error($"{nameof(ignoredPlayer)} invalid");
                return false;
            }

            if (_playersToIgnore == null || _playersToIgnore.Length < 2) {
                _playersToIgnore = null;
                return true;
            }

            // make sure all contained players are still alive, otherwise remove them
            int validPlayers = 0;
            int[] stillValidIgnoredPlayers = new int[_playersToIgnore.Length];
            bool wasIgnored = false;
            foreach (int playerId in _playersToIgnore) {
                if (VRCPlayerApi.GetPlayerById(playerId) == null) {
                    continue;
                }

                // keep all valid players
                stillValidIgnoredPlayers[validPlayers] = playerId;
                ++validPlayers;

                // decrement the index by one again if the player id is found
                if (playerId == ignoredPlayer.playerId) {
                    wasIgnored = true;
                    --validPlayers;
                }
            }

            // shrink the validated array content (happens when ignored players have left the world)
            // and store it again in the old array
            _playersToIgnore = new int[validPlayers];
            for (int i = 0; i < validPlayers; i++) {
                _playersToIgnore[i] = stillValidIgnoredPlayers[i];
            }

            return wasIgnored;
        }


        public bool OverridePlayerSettings(
                PlayerAudioOverride playerAudioOverride,
                VRCPlayerApi playerToAffect
        ) {
#if TLP_DEBUG
            DebugLog(nameof(OverridePlayerSettings));
#endif
            if (!Assert(
                        Utilities.IsValid(playerAudioOverride),
                        "playerAudioOverride invalid",
                        this
                )) {
                return false;
            }

            if (!Assert(
                        Utilities.IsValid(playerToAffect),
                        "playerToAffect invalid",
                        this
                )) {
                return false;
            }

            if (playerToAffect.isLocal) {
                if (!Assert(
                            Utilities.IsValid(LocalPlayerOverrideList),
                            "localPlayerOverrideList invalid",
                            this
                    )) {
                    return false;
                }

                bool playerAdded = LocalPlayerOverrideList.AddOverride(playerAudioOverride);
                if (!playerAdded) {
                    Warn(
                            $"Local player {playerToAffect.displayName} already has the override {playerAudioOverride.gameObject.name} in the priority list"
                    );
                }

                return playerAdded;
            }

            // check if the player already has an override
            int index = Array.BinarySearch(PlayersToOverride, playerToAffect.playerId);
            if (index > -1) {
                if (!Assert(
                            Utilities.IsValid(_playerOverrideLists[index]),
                            "Missing playerOverrideList for player",
                            this
                    )) {
                    return false;
                }

                // add the new override
                bool playerAdded = _playerOverrideLists[index].AddOverride(playerAudioOverride);
                if (!playerAdded) {
                    Warn(
                            $"Player {playerToAffect.displayName} already has the override {playerAudioOverride.gameObject.name} in the priority list"
                    );
                }

                return playerAdded;
            }

            int newSize = CreateOverrideSlotForPlayer(playerToAffect);

            // get the index of the added player
            int position = Array.BinarySearch(PlayersToOverride, playerToAffect.playerId);
            // create a new list of overrides
            var tempOverrides = new PlayerAudioOverrideList[newSize];

            // copy the first half up to the added player into a the new list
            if (position > 0) {
                Array.ConstrainedCopy(_playerOverrideLists, 0, tempOverrides, 0, position);
            }

            var prefabInstance = PlayerOverrideListPool.Get();
            // insert the new entry for the added player
            var playerAudioOverrideList = prefabInstance.GetComponent<PlayerAudioOverrideList>();
            if (!Assert(
                        playerAudioOverrideList.AddOverride(playerAudioOverride),
                        "Failed adding override to instantiated list prefab",
                        this
                )) {
                return false;
            }

            tempOverrides[position] = playerAudioOverrideList;

            // copy the remaining overrides for the unchanged second half of overriden players
            Array.ConstrainedCopy(
                    _playerOverrideLists,
                    position,
                    tempOverrides,
                    position + 1,
                    _playerOverrideLists.Length - position
            );

            // replace the overrides with the new list of overrides
            _playerOverrideLists = tempOverrides;

            return true;
        }

        public int CreateOverrideSlotForPlayer(VRCPlayerApi playerToAffect) {
            // add a new override list for that player
            // add the player to the list of players that have overrides
            int newSize = PlayersToOverride.Length + 1;

            int[] tempArray = new int[newSize];
            Array.ConstrainedCopy(PlayersToOverride, 0, tempArray, 0, PlayersToOverride.Length);
            PlayersToOverride = tempArray;
            PlayersToOverride[PlayersToOverride.Length - 1] = playerToAffect.playerId;

            // sort it afterwards to allow binary search to work again
            Sort(PlayersToOverride);
            return newSize;
        }

        public bool ClearPlayerOverride(PlayerAudioOverride playerAudioOverride, VRCPlayerApi playerToClear) {
#if TLP_DEBUG
            DebugLog(nameof(ClearPlayerOverride));
#endif
            if (!Assert(Utilities.IsValid(playerToClear), "Player to clear invalid", this)) {
                return false;
            }

            if (playerToClear.isLocal) {
                if (!Assert(
                            Utilities.IsValid(LocalPlayerOverrideList),
                            "localPlayerOverrideList invalid",
                            this
                    )) {
                    return false;
                }

                LocalPlayerOverrideList.RemoveOverride(playerAudioOverride);
                return true;
            }

            if (!Assert(
                        PlayersToOverride != null && PlayersToOverride.Length > 0,
                        "_playersToOverride is empty",
                        this
                )) {
                return false;
            }

            // ReSharper disable once PossibleNullReferenceException invalid as checked with Assert
            int[] temp = new int[PlayersToOverride.Length];
            PlayersToOverride.CopyTo(temp, 0);

            // remove all invalid players first
            foreach (int i in temp) {
                if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(i))) {
                    ClearSinglePlayerOverride(playerAudioOverride, i);
                }
            }

            // remove the actual player that was requested to be removed
            return ClearSinglePlayerOverride(playerAudioOverride, playerToClear.playerId);
        }

        private bool ClearSinglePlayerOverride(PlayerAudioOverride playerAudioOverride, int playerId) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"{nameof(ClearSinglePlayerOverride)} of {playerAudioOverride.name} for player {playerId.IdToVrcPlayer().ToStringSafe()}"
            );
#endif
            #endregion

            var player = VRCPlayerApi.GetPlayerById(playerId);
            if (!Utilities.IsValid(player)) {
                Error($"player with ID {playerId} is invalid");
                return false;
            }

            if (player.isLocal) {
                if (!Utilities.IsValid(LocalPlayerOverrideList)) {
                    Error($"{nameof(LocalPlayerOverrideList)} is invalid");
                    return false;
                }

                LocalPlayerOverrideList.RemoveOverride(playerAudioOverride);
                return true;
            }

            if (PlayersToOverride.Length < 1) {
                // no players are overridden
                return true;
            }

            // check if the player already has an override
            int index = Array.BinarySearch(PlayersToOverride, playerId);
            if (index < 0) {
                // player has no override
                return true;
            }

            var playerAudioOverrideList = _playerOverrideLists[index];
            int remaining = playerAudioOverrideList.RemoveOverride(playerAudioOverride);
            if (remaining > 0) {
                return true;
            }

            ReturnPlayerAudioOverrideListToPool(playerAudioOverrideList);

            PlayersToOverride[index] = int.MaxValue;

            int newSize = PlayersToOverride.Length - 1;
            Sort(PlayersToOverride);
            int[] tempArray = new int[newSize];
            Array.ConstrainedCopy(PlayersToOverride, 0, tempArray, 0, newSize);
            PlayersToOverride = tempArray;

            // create a new list of overrides
            var tempOverrides = new PlayerAudioOverrideList[newSize];

            // copy the first half up to the added player into a the new list
            Array.ConstrainedCopy(_playerOverrideLists, 0, tempOverrides, 0, index);
            // copy the remaining overrides for the unchanged second half of overriden players
            int firstIndexSecondHalf = index + 1;
            Array.ConstrainedCopy(
                    _playerOverrideLists,
                    firstIndexSecondHalf,
                    tempOverrides,
                    index,
                    _playerOverrideLists.Length - firstIndexSecondHalf
            );

            // replace the overrides with the new list of overrides
            _playerOverrideLists = tempOverrides;
            return true;
        }

        private void ReturnPlayerAudioOverrideListToPool(PlayerAudioOverrideList playerAudioOverrideList) {
            if (Assert(
                        playerAudioOverrideList.PoolableInUse,
                        $"{nameof(PlayerAudioOverrideList)} should have been retrieved from a pool",
                        playerAudioOverrideList
                )
                && Utilities.IsValid(playerAudioOverrideList.Pool)
                && Utilities.IsValid((Pool)playerAudioOverrideList.Pool)) {
                var pool = (Pool)playerAudioOverrideList.Pool;
                pool.Return(playerAudioOverrideList.gameObject);
                return;
            }

            Warn(
                    $"Can not return {nameof(PlayerAudioOverrideList)} as {nameof(Pool)} is invalid. Destroying instead."
            );
            Destroy(playerAudioOverrideList.gameObject);
        }

        #region Sorting
        private void Sort(int[] array) {
            if (array == null || array.Length < 2) {
                return;
            }

            BubbleSort(array);
        }

        private void BubbleSort(int[] array) {
            int arrayLength = array.Length;
            for (int i = 0; i < arrayLength; i++) {
                for (int j = 0; j < arrayLength - 1; j++) {
                    int next = j + 1;

                    if (array[j] > array[next]) {
                        int tmp = array[j];
                        array[j] = array[next];
                        array[next] = tmp;
                    }
                }
            }
        }
        #endregion

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>True when a player is not ignored and has no active override</returns>
        public bool UsesDefaultEffects(VRCPlayerApi player) {
            if (IsIgnored(player)) {
                return false;
            }

            return !UsesVoiceOverride(player);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true when a player is affected neither by global nor by override effects</returns>
        public bool IsIgnored(VRCPlayerApi player) {
            bool mustBeIgnored = false;

            bool playersIgnored = _playersToIgnore != null && _playersToIgnore.Length > 0;
            if (playersIgnored) {
                mustBeIgnored = Array.BinarySearch(_playersToIgnore, player.playerId) > -1;
            }

            return mustBeIgnored;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if the player is not ignored and is assigned to at least one override</returns>
        public bool UsesVoiceOverride(VRCPlayerApi player) {
            if (IsIgnored(player)) {
                return false;
            }

            return HasVoiceOverrides(player);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if the player is assigned to at least one override</returns>
        public bool HasVoiceOverrides(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(HasVoiceOverrides));
#endif
            if (!Assert(Utilities.IsValid(player), "Player invalid", this)) {
                return false;
            }

            if (player.isLocal) {
                return Assert(
                               Utilities.IsValid(LocalPlayerOverrideList),
                               "localPlayerOverrideList invalid",
                               this
                       )
                       && Utilities.IsValid(LocalPlayerOverrideList.GetMaxPriority(player));
            }

            bool noOverridesAvailable = PlayersToOverride == null || PlayersToOverride.Length == 0;
            if (noOverridesAvailable) {
                return false;
            }

            return Array.BinarySearch(PlayersToOverride, player.playerId) > -1;
        }

        public PlayerAudioOverride GetMaxPriorityOverride(VRCPlayerApi player) {
            if (!Utilities.IsValid(player)) {
                return null;
            }

            if (player.isLocal) {
                if (!Utilities.IsValid(LocalPlayerOverrideList)) {
                    Error($"{nameof(LocalPlayerOverrideList)} invalid");
                    return null;
                }

                return LocalPlayerOverrideList.GetMaxPriority(player);
            }

            bool playersWithOverride = PlayersToOverride != null && PlayersToOverride.Length > 0;
            bool playerOverridesExist = _playerOverrideLists != null && _playerOverrideLists.Length > 0;
            if (!playersWithOverride || !playerOverridesExist) {
                return null;
            }

            int index = Array.BinarySearch(PlayersToOverride, player.playerId);

            if (index > -1 && index < _playerOverrideLists.Length && Utilities.IsValid(_playerOverrideLists[index])) {
                return _playerOverrideLists[index].GetMaxPriority(player);
            }

            return null;
        }

        #region Public
        /// <summary>
        /// local player not included if it has an override
        /// </summary>
        /// <returns>copy of array of player ids or empty array</returns>
        public int[] GetNonLocalPlayersWithOverrides() {
#if TLP_DEBUG
            DebugLog(nameof(GetNonLocalPlayersWithOverrides));
#endif
            if (PlayersToOverride == null) {
                return new int[0];
            }

            int[] temp = new int[PlayersToOverride.Length];
            PlayersToOverride.CopyTo(temp, 0);
            return temp;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="audioReverbFilter">Clears the reverb settings when null, otherwise copies and applies the values</param>
        internal void UseReverbSettings(AudioReverbFilter audioReverbFilter) {
#if TLP_DEBUG
            DebugLog(nameof(UseReverbSettings));
#endif
            if (!Assert(Utilities.IsValid(MainAudioReverbFilter), "mainAudioReverbFilter invalid", this)) {
                return;
            }

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

        #region internal
        internal void ResetAllPlayerVoices() {
            if (!Utilities.IsValid(DefaultConfiguration)) {
                Error($"{nameof(DefaultConfiguration)} invalid");
                return;
            }

            int playerCount = UpdatePlayerList();
            for (int i = 0; i < playerCount; i++) {
                var playerApi = _players[i];
                if (!Utilities.IsValid(playerApi)) {
                    continue;
                }

                UpdateVoiceAudio(playerApi, 1f, true, 15f, 25, 0, 0);
            }
        }

        internal void EnableCurrentReverbSettings() {
            var betterPlayerAudioOverride = GetMaxPriorityOverride(Networking.LocalPlayer);
            if (Utilities.IsValid(betterPlayerAudioOverride)) {
                UseReverbSettings(betterPlayerAudioOverride.OptionalReverb);
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

        internal VRCPlayerApi GetNextPlayer(int playerCount) {
            _playerIndex = (_playerIndex + 1) % playerCount;
            var otherPlayer = _players[_playerIndex];
            return otherPlayer;
        }
        #endregion
    }
}