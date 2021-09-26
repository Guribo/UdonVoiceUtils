using System;
using Guribo.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace Guribo.UdonBetterAudio.Runtime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(10010)]
    public class BetterPlayerAudio : UdonSharpBehaviour
    {
        #region General settings

        [Header("General settings")]
        /// <summary>
        /// Whether ownership can be changed by any player at any time with Networking.SetOwner(...)
        /// </summary>
        [Tooltip("Whether ownership can be changed by any player at any time with Networking.SetOwner(...)")]
        [SerializeField] protected bool allowOwnershipTransfer;

        /// <summary>
        /// When enabled the master can change the settings of all players
        /// </summary>
        [Tooltip("When enabled the master can change the settings of all players")]
        public bool defaultAllowMasterControl;

        /// <summary>
        /// How long to wait after start before applying changes to all players.
        /// Prevents excessive volume on joining the world because player positions might not be up to date yet.
        /// </summary>
        [Tooltip(
            "How long to wait after start before applying changes to all players. Prevents excessive volume on joining the world because player positions might not be up to date yet.")]
        [SerializeField]
        [Range(0, 10f)]
        protected float startDelay = 10f;

        /// <summary>
        /// How many player updates should be performed every second (at low framerate).
        /// Independent of the value set here it will always update at least 1 player for each rendered frame.
        /// 
        /// Example:
        /// With 60 players in the world, playerUpdateRate = 20 and 10 fps it will take 60/20 = 3 seconds until every
        /// player got updated. For each rendered frame 2 players are updated. At 60 fps it will take 1 second.
        /// 
        /// If set to -1 it will update ALL PLAYERS EVERY FRAME
        /// Don't use this option in worlds with up to 80 people as this can have a serious performance impact!
        /// </summary>
        [Tooltip(
            "How many player updates should be performed every second (at low framerate). Example: with 60 players in the world, playerUpdateRate = 20 and 20 fps it will take 60/20 = 3 seconds until every player got updated. At 60 fps it will take 1 second.")]
        [SerializeField]
        protected int playerUpdateRate = 1;

        #endregion


        #region Occlusion settings

        #region Constants

        private const int EnvironmentLayerMask = 1 << 11;
        private const int UILayerMask = 1 << 5;

        #endregion

        [Header("Occlusion settings")]
        /// <summary>
        /// Layers which can reduce voice and avatar sound effects when they are in between the local player (listener)
        /// and the player/avatar producing the sound
        /// Default layers: 11 and 5 (Environment and UI which includes the capsule colliders of other players)
        /// </summary>
        [Tooltip(
            "Objects on these layers reduce the voice/avatar sound volume when they are in-between the local player and the player/avatar that produces the sound")]
        public LayerMask occlusionMask = EnvironmentLayerMask | UILayerMask;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the
        /// voice/player to the current distance and make him/her/them in-audible
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float defaultOcclusionFactor = 0.7f;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// Occlusion when a player is occluded by another player.
        /// A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the
        /// voice/player to the current distance and make him/her/them in-audible
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Occlusion when a player is occluded by another player. A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float defaultPlayerOcclusionFactor = 0.85f;

        #endregion

        #region Directionality settings

        [Header("Directionality settings")]
        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 1.0 reduces the ranges by up to 100% when the listener is facing away from a voice/avatar
        /// and thus making them more quiet.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when the listener is facing away from a voice/avatar and thus making them more quiet.")]
        public float defaultListenerDirectionality = 0.5f;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is
        /// facing away from the listener.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is facing away from the listener.")]
        public float defaultPlayerDirectionality = 0.3f;

        #endregion


        #region Voice settings

        [Header("Voice settings")]
        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-disable-lowpass</remarks>
        /// </summary>
        [Tooltip("When enabled the voice of a player sounds muffled when close to the max. audible range.")]
        public bool defaultEnableVoiceLowpass = true;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-distance-near</remarks>
        /// </summary>
        [Tooltip("The volume will stay at max. when the player is closer than this distance.")]
        [Range(0, 1000000)] public float defaultVoiceDistanceNear;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/playewar-audio#set-voice-distance-far</remarks>
        /// </summary>
        [Tooltip("Beyond this distance the player can't be heard.")]
        [Range(0, 1000000)] public float defaultVoiceDistanceFar = 25f;

        /// <summary>
        /// Default is 15. In my experience this may lead to clipping when being close to someone with a loud microphone.
        /// My recommendation is to use 0 instead.
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-gain</remarks>
        /// </summary>
        [Tooltip("Additional volume increase. Changing this may require re-adjusting occlusion parameters!")]
        [Range(0, 24)] public float defaultVoiceGain = 15f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-volumetric-radius</remarks>
        /// </summary>
        [Tooltip(
            "Range in which the player voice is not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed.")]
        [Range(0, 1000)] public float defaultVoiceVolumetricRadius;

        #endregion

        #region Avatar settings

        [Header("Avatar settings")]
        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudioforcespatial</remarks>
        /// </summary>
        [Tooltip("When set overrides all avatar audio sources to be spatialized.")]
        public bool defaultForceAvatarSpatialAudio;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiocustomcurve</remarks>
        /// </summary>
        [Tooltip("When set custom audio curves on avatar audio sources are used.")]
        public bool defaultAllowAvatarCustomAudioCurves = true;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudionearradius</remarks>
        /// </summary>
        [Tooltip("Max. distance at which player audio sources start to fall of in volume.")]
        public float defaultAvatarNearRadius = 40f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiofarradius</remarks>
        /// </summary>
        [Tooltip("Max. allowed distance at which player audio sources can be heard.")]
        public float defaultAvatarFarRadius = 40f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiogain</remarks>
        /// </summary>
        [Range(0, 10)]
        [Tooltip("Volume increase in decibel.")]
        public float defaultAvatarGain = 10f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiovolumetricradius</remarks>
        /// </summary>
        [Tooltip(
            "Range in which the player audio sources are not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed.")]
        public float defaultAvatarVolumetricRadius;

        #endregion

        #region Mandatory references

        [Header("Mandatory references")]
        public BetterPlayerAudioOverrideList cloneablePlayerList;
        public BetterPlayerAudioOverrideList localPlayerOverrideList;
        
        [SerializeField] 
        internal AudioReverbFilter mainAudioReverbFilter;

        #region Libraries

        public UdonDebug udonDebug;

        #endregion

        #endregion

        #region UI control

        [Header("UI control")]
        [SerializeField] private UdonBehaviour optionalUiController;
        /// <summary>
        /// The name of the function in the UI controller script that should be called when the master control
        /// is enabled and the master changed any value
        /// </summary>
        [Tooltip(
            "The name of the function in the UI controller script that should be called when the master control is enabled and the master changed any value")]
        [SerializeField]
        private string updateUiEventName = "UpdateUi";

        #endregion


        #region currently used values

        /// <summary>
        /// <inheritdoc cref="defaultAllowMasterControl"/>
        /// </summary>
        private bool _allowMasterControl;

        /// <summary>
        /// <inheritdoc cref="defaultOcclusionFactor"/>
        /// </summary>
        [NonSerialized] public float OcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="defaultPlayerOcclusionFactor"/>
        /// </summary>
        [NonSerialized] public float PlayerOcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="defaultListenerDirectionality"/>
        /// </summary>
        [NonSerialized] public float ListenerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultPlayerDirectionality"/>
        /// </summary>
        [NonSerialized] public float PlayerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultEnableVoiceLowpass"/>
        /// </summary>
        [NonSerialized] public bool EnableVoiceLowpass;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceNear"/>
        /// </summary>
        [NonSerialized] public float TargetVoiceDistanceNear;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceFar"/>
        /// </summary>
        [NonSerialized] public float TargetVoiceDistanceFar;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceGain"/>
        /// </summary>
        [NonSerialized] public float TargetVoiceGain;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceVolumetricRadius"/>
        /// </summary>
        [NonSerialized] public float TargetVoiceVolumetricRadius;

        /// <summary>
        /// <inheritdoc cref="defaultForceAvatarSpatialAudio"/>
        /// </summary>
        [NonSerialized] public bool ForceAvatarSpatialAudio;

        /// <summary>
        /// <inheritdoc cref="defaultAllowAvatarCustomAudioCurves"/>
        /// </summary>
        [NonSerialized] public bool AllowAvatarCustomAudioCurves;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarNearRadius"/>
        /// </summary>
        [NonSerialized] public float TargetAvatarNearRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarFarRadius"/>
        /// </summary>
        [NonSerialized] public float TargetAvatarFarRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarGain"/>
        /// </summary>
        [NonSerialized] public float TargetAvatarGain;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarVolumetricRadius"/>
        /// </summary>
        [NonSerialized] public float TargetAvatarVolumetricRadius;

        #endregion

        #region Manually Synced values

        /// <summary>
        /// <inheritdoc cref="defaultOcclusionFactor"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterOcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="defaultPlayerOcclusionFactor"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterPlayerOcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="defaultListenerDirectionality"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterListenerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultPlayerDirectionality"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterPlayerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultEnableVoiceLowpass"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public bool masterEnableVoiceLowpass;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceNear"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetVoiceDistanceNear;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceFar"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetVoiceDistanceFar;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceGain"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetVoiceGain;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceVolumetricRadius"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetVoiceVolumetricRadius;

        /// <summary>
        /// <inheritdoc cref="defaultForceAvatarSpatialAudio"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public bool masterForceAvatarSpatialAudio;

        /// <summary>
        /// <inheritdoc cref="defaultAllowAvatarCustomAudioCurves"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public bool masterAllowAvatarCustomAudioCurves;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarNearRadius"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetAvatarNearRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarFarRadius"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetAvatarFarRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarGain"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetAvatarGain;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarVolumetricRadius"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetAvatarVolumetricRadius;

        #endregion

        #region State

        internal bool ReceivedStart;
        internal bool CanUpdate;
        private bool _isReallyOwner;
        private int _playerIndex;
        private VRCPlayerApi[] _players = new VRCPlayerApi[1];
        private int[] _playersToIgnore;
        internal int[] PlayersToOverride = new int[0];
        private BetterPlayerAudioOverrideList[] _playerOverrideLists;
        private readonly RaycastHit[] _rayHits = new RaycastHit[2];
        private int _serializationRequests;
        internal BetterPlayerAudioOverride LocalOverride;

        internal int UiLayerMask;

        #endregion


        #region Unity Lifecycle

        public void OnEnable()
        {
            UiLayerMask = LayerMask.NameToLayer("UI");
            
            if (ReceivedStart)
            {
                // don't wait for all players to load as they should be all loaded already
                CanUpdate = true;
            }

            EnableCurrentReverbSettings();
        }

        public void OnDisable()
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] OnDisable", this);

            UseReverbSettings(null);
            ResetAllPlayerVoices();
        }

        public void OnDestroy()
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] OnDestroy", this);
            UseReverbSettings(null);
            ResetAllPlayerVoices();
        }

        internal void ResetAllPlayerVoices()
        {
            var playerCount = UpdatePlayerList();
            for (var i = 0; i < playerCount; i++)
            {
                var playerApi = _players[i];
                if (!Utilities.IsValid(playerApi))
                {
                    continue;
                }

                UpdateVoiceAudio(playerApi, 1f, true, 15f, 25, 0, 0);
            }
        }

        internal void EnableCurrentReverbSettings()
        {
            var betterPlayerAudioOverride = GetMaxPriorityOverride(Networking.LocalPlayer);
            if (Utilities.IsValid(betterPlayerAudioOverride))
            {
                UseReverbSettings(betterPlayerAudioOverride.optionalReverb);
            }
        }

        public void Start()
        {
            _playerOverrideLists = new BetterPlayerAudioOverrideList[0];
            Initialize();

            EnableProcessingDelayed(startDelay);
        }

        public override void PostLateUpdate()
        {
            if (!CanUpdate)
            {
                return;
            }

            // skip local player
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer))
            {
                return;
            }
            
            var newLocalOverride = localPlayerOverrideList.GetMaxPriority();
            LocalOverride = UpdateAudioFilters(newLocalOverride, LocalOverride);

            var playerCount = UpdatePlayerList();
            if (playerCount < 2)
            {
                return;
            }

            var pendingPlayerUpdates = GetPendingPlayerUpdates(playerCount);
            for (var playerUpdate = 0; playerUpdate < pendingPlayerUpdates; ++playerUpdate)
            {
                var otherPlayer = GetNextPlayer(playerCount);
                if (!Utilities.IsValid(otherPlayer))
                {
                    // this should never be the case!!!
                    continue;
                }

                UpdateOtherPlayer(localPlayer, otherPlayer);
            }
        }

        internal VRCPlayerApi GetNextPlayer(int playerCount)
        {
            _playerIndex = (_playerIndex + 1) % playerCount;
            var otherPlayer = _players[_playerIndex];
            return otherPlayer;
        }

        #endregion
        
        public void EnableProcessingDelayed(float delay)
        {
            SendCustomEventDelayedSeconds(nameof(EnableProcessing), 10f);
        }

        public void EnableProcessing()
        {
            if (!(Utilities.IsValid(this)
                  && Utilities.IsValid(gameObject))
                && gameObject.activeInHierarchy)
            {
                // do nothing if the behaviour is not alive/valid/active
                return;
            }

            CanUpdate = true;
        }

        internal void UpdateOtherPlayer(VRCPlayerApi localPlayer, VRCPlayerApi otherPlayer)
        {
            if (!Utilities.IsValid(otherPlayer)
                || otherPlayer.playerId == localPlayer.playerId
                || PlayerIsIgnored(otherPlayer))
            {
                return;
            }

            var playerOverride = GetMaxPriorityOverride(otherPlayer);
            
            var privacyChannelId = -1;
            var muteOutsiders = false;
            var disallowListeningToChannel = false;

            if (Utilities.IsValid(LocalOverride))
            {
                privacyChannelId = LocalOverride.privacyChannelId;
                muteOutsiders = LocalOverride.muteOutsiders;
                disallowListeningToChannel = LocalOverride.disallowListeningToChannel;
            }
            
            var listenerHead = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var otherPlayerHead = otherPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            Vector3 listenerHeadPosition;
            Quaternion listenerHeadRotation;
            Vector3 otherHeadPosition;
            Quaternion otherHeadRotation;

            var validHeadTrackingData = listenerHead.position.sqrMagnitude > 0.001f;
            if (validHeadTrackingData)
            {
                listenerHeadPosition = listenerHead.position;
                listenerHeadRotation = listenerHead.rotation;
            }
            else
            {
                var playerPosition = localPlayer.GetPosition();
                listenerHeadRotation = localPlayer.GetRotation();
                var playerUp = listenerHeadRotation * Vector3.up;

                // assume the max player height is 10m
                var ray = new Ray(playerPosition + playerUp * 10f, -playerUp);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, 10f, UiLayerMask, QueryTriggerInteraction.Ignore))
                {
                    // and assume that the player head position is at 80% of the capsule height
                    listenerHeadPosition = Vector3.Lerp(playerPosition, hitInfo.point, 0.8f);
                }
                else
                {
                    const float assumedAvgAvatarHeadHeight = 1.25f;
                    listenerHeadPosition = playerPosition + playerUp * assumedAvgAvatarHeadHeight;
                }

                // use a fallback value if the UI capsule was not hit
            }

            // create a fake head position/rotation (no pitch and roll)
            var validHeadTrackingData1 = otherPlayerHead.position.sqrMagnitude > 0.001f;
            if (validHeadTrackingData1)
            {
                otherHeadPosition = otherPlayerHead.position;
                otherHeadRotation = otherPlayerHead.rotation;
            }
            else
            {
                var playerPosition1 = otherPlayer.GetPosition();
                otherHeadRotation = otherPlayer.GetRotation();
                var playerUp1 = otherHeadRotation * Vector3.up;

                // assume the max player height is 10m
                var ray1 = new Ray(playerPosition1 + playerUp1 * 10f, -playerUp1);
                RaycastHit hitInfo1;
                if (Physics.Raycast(ray1, out hitInfo1, 10f, UiLayerMask, QueryTriggerInteraction.Ignore))
                {
                    // and assume that the player head position is at 80% of the capsule height
                    otherHeadPosition = Vector3.Lerp(playerPosition1, hitInfo1.point, 0.8f);
                }
                else
                {
                    const float assumedAvgAvatarHeadHeight1 = 1.25f;
                    otherHeadPosition = playerPosition1 + playerUp1 * assumedAvgAvatarHeadHeight1;
                }

                // use a fallback value if the UI capsule was not hit
            }

            // create a fake head position/rotation (no pitch and roll)

            var listenerToPlayer = otherHeadPosition - listenerHeadPosition;
            var direction = listenerToPlayer.normalized;
            var distance = listenerToPlayer.magnitude;

            var localPlayerInPrivateChannel = privacyChannelId != -1;
            if (Utilities.IsValid(playerOverride))
            {
                if (OtherPlayerWithOverrideCanBeHeard(playerOverride, localPlayerInPrivateChannel, privacyChannelId,
                    muteOutsiders, disallowListeningToChannel))
                {
                    ApplyAudioOverrides(otherPlayer,
                        listenerHeadPosition,
                        listenerHeadRotation,
                        direction,
                        distance,
                        playerOverride,
                        otherHeadRotation);
                    return;
                }

                MutePlayer(otherPlayer);
                return;
            }

            if (localPlayerInPrivateChannel && muteOutsiders)
            {
                MutePlayer(otherPlayer);
                return;
            }

            ApplyGlobalAudioSettings(otherPlayer,
                listenerHeadPosition,
                listenerHeadRotation,
                direction,
                distance,
                otherHeadRotation);
        }


        public bool OtherPlayerWithOverrideCanBeHeard(BetterPlayerAudioOverride playerOverride,
            bool localPlayerInPrivateChannel, int currentPrivacyChannel, bool muteOutsiders,
            bool disallowLocalPlayerListening)
        {
            // ReSharper disable once PossibleNullReferenceException (invalid warning because of IsValid check)
            var playerInSamePrivateChannel = playerOverride.privacyChannelId == currentPrivacyChannel;
            var playerInSamePrivateChannelAllowedToBeHeard =
                playerInSamePrivateChannel && !disallowLocalPlayerListening;
            var otherPlayerNotInAnyPrivateChannel = playerOverride.privacyChannelId == -1;
            var isOutsiderAndCanBeHeard = localPlayerInPrivateChannel
                                          && otherPlayerNotInAnyPrivateChannel
                                          && !muteOutsiders;

            return playerInSamePrivateChannelAllowedToBeHeard || isOutsiderAndCanBeHeard;
        }

        private void ApplyGlobalAudioSettings(VRCPlayerApi otherPlayer,
           Vector3 listenerHeadPosition,
           Quaternion listenerHeadRotation,
            Vector3 direction,
            float distance,
           Quaternion otherPlayerHeadRotation)
        {
            var occlusionFactor = CalculateOcclusion(listenerHeadPosition,
                direction,
                distance,
                OcclusionFactor,
                PlayerOcclusionFactor,
                occlusionMask);

            var directionality = CalculateDirectionality(listenerHeadRotation,
                otherPlayerHeadRotation,
                direction,
                ListenerDirectionality,
                PlayerDirectionality);

            var distanceReduction = directionality * occlusionFactor;

            var voiceDistanceFactor = CalculateRangeReduction(distance,
                distanceReduction,
                TargetVoiceDistanceFar);

            UpdateVoiceAudio(otherPlayer,
                voiceDistanceFactor,
                EnableVoiceLowpass,
                TargetVoiceGain,
                TargetVoiceDistanceFar,
                TargetVoiceDistanceNear,
                TargetVoiceVolumetricRadius);

            var avatarDistanceFactor = CalculateRangeReduction(distance,
                distanceReduction,
                TargetAvatarFarRadius);

            UpdateAvatarAudio(otherPlayer,
                avatarDistanceFactor,
                ForceAvatarSpatialAudio,
                AllowAvatarCustomAudioCurves,
                TargetAvatarGain,
                TargetAvatarFarRadius,
                TargetAvatarNearRadius,
                TargetAvatarVolumetricRadius);
        }

        private void ApplyAudioOverrides(VRCPlayerApi otherPlayer,
            Vector3 listenerHeadPosition,
            Quaternion listenerHeadRotation,
            Vector3 direction,
            float distance,
            BetterPlayerAudioOverride playerOverride,
            Quaternion otherHeadRotation)
        {
            var occlusionFactor = CalculateOcclusion(listenerHeadPosition,
                direction,
                distance,
                playerOverride.occlusionFactor,
                playerOverride.playerOcclusionFactor,
                playerOverride.occlusionMask);

            var directionality = CalculateDirectionality(listenerHeadRotation,
                otherHeadRotation,
                direction,
                playerOverride.listenerDirectionality,
                playerOverride.playerDirectionality);

            var distanceReduction = directionality * occlusionFactor;

            var voiceDistanceFactor = CalculateRangeReduction(distance,
                distanceReduction,
                playerOverride.voiceDistanceFar);

            UpdateVoiceAudio(otherPlayer,
                voiceDistanceFactor,
                playerOverride.enableVoiceLowpass,
                playerOverride.voiceGain,
                playerOverride.voiceDistanceFar,
                playerOverride.voiceDistanceNear,
                playerOverride.voiceVolumetricRadius);

            var avatarDistanceFactor = CalculateRangeReduction(distance,
                distanceReduction,
                playerOverride.targetAvatarFarRadius);

            UpdateAvatarAudio(otherPlayer,
                avatarDistanceFactor,
                playerOverride.forceAvatarSpatialAudio,
                playerOverride.allowAvatarCustomAudioCurves,
                playerOverride.targetAvatarGain,
                playerOverride.targetAvatarFarRadius,
                playerOverride.targetAvatarNearRadius,
                playerOverride.targetAvatarVolumetricRadius);
        }

        private void MutePlayer(VRCPlayerApi otherPlayer)
        {
            UpdateVoiceAudio(otherPlayer,
                0f,
                false,
                0f,
                0f,
                0f,
                0f);

            UpdateAvatarAudio(otherPlayer,
                0,
                false,
                false,
                0f,
                0f,
                0f,
                0f);
        }

        private bool PlayerIsIgnored(VRCPlayerApi vrcPlayerApi)
        {
            if (_playersToIgnore == null || !Utilities.IsValid(vrcPlayerApi)) return false;

            return Array.BinarySearch(_playersToIgnore, vrcPlayerApi.playerId) > -1;
        }

        private int GetPendingPlayerUpdates(int playerCount)
        {
            if (playerUpdateRate == -1)
            {
                // this will update all players every update
                return playerCount;
            }

            // calculate how many players need to be updated during this update to perform the requested updates
            // every second
            var pendingUpdates = Mathf.FloorToInt(playerUpdateRate * Time.deltaTime);

            // make sure at least one player gets updated and no player gets updated twice
            return Mathf.Clamp(pendingUpdates, 1, playerCount);
        }


        /// <summary>
        /// initializes all runtime variables using the default values.
        /// Has no effect if called again or if start() was already received.
        /// To reset values use ResetToDefault() instead.
        /// </summary>
        public void Initialize()
        {
            if (!ReceivedStart)
            {
                ReceivedStart = true;
                ResetToDefault();
            }
        }

        public void ResetToDefault()
        {
            OcclusionFactor = defaultOcclusionFactor;
            PlayerOcclusionFactor = defaultPlayerOcclusionFactor;
            ListenerDirectionality = defaultListenerDirectionality;
            PlayerDirectionality = defaultPlayerDirectionality;
            EnableVoiceLowpass = defaultEnableVoiceLowpass;
            TargetVoiceDistanceNear = defaultVoiceDistanceNear;
            TargetVoiceDistanceFar = defaultVoiceDistanceFar;
            TargetVoiceGain = defaultVoiceGain;
            TargetVoiceVolumetricRadius = defaultVoiceVolumetricRadius;
            ForceAvatarSpatialAudio = defaultForceAvatarSpatialAudio;
            AllowAvatarCustomAudioCurves = defaultAllowAvatarCustomAudioCurves;
            TargetAvatarNearRadius = defaultAvatarNearRadius;
            TargetAvatarFarRadius = defaultAvatarFarRadius;
            TargetAvatarGain = defaultAvatarGain;
            TargetAvatarVolumetricRadius = defaultAvatarVolumetricRadius;

            TryRequestSerialization();
        }

        /// <summary>
        /// Notifies the component that changes to variables have been made.
        /// Triggers synchronization of master values if the calling player is the owner.
        /// </summary>
        public void SetDirty()
        {
            TryRequestSerialization();
        }

        private void TryRequestSerialization()
        {
            if (Networking.IsOwner(gameObject))
            {
                _serializationRequests++;
                Debug.Log($"[<color=#008000>BetterAudio</color>] " +
                          $"TryRequestSerialization: Requesting serialization (queue length = {_serializationRequests})");
                RequestSerialization();
            }
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (!result.success)
            {
                Debug.LogWarning(
                    $"[<color=#008000>BetterAudio</color>] OnPostSerialization: Serialization failed, trying again",
                    this);
                RequestSerialization();
                return;
            }

            Debug.Log($"[<color=#008000>BetterAudio</color>] OnPostSerialization: Serialized {result.byteCount} bytes");
            if (Networking.IsOwner(gameObject))
            {
                _serializationRequests--;
                if (_serializationRequests > 0)
                {
                    RequestSerialization();
                }
            }
            else
            {
                _serializationRequests = 0;
            }

            Debug.Log($"[<color=#008000>BetterAudio</color>] " +
                      $"OnPostSerialization: (queue length = {_serializationRequests})");
        }

        private float CalculateRangeReduction(float distance, float distanceReduction, float maxAudibleRange)
        {
            if (maxAudibleRange <= 0f || Mathf.Abs(distanceReduction - 1f) < 0.01f)
            {
                return 1f;
            }

            var remainingDistanceToFarRadius = maxAudibleRange - distance;
            var playerCouldBeAudible = remainingDistanceToFarRadius > 0f;

            var occlusion = 1f;
            if (playerCouldBeAudible)
            {
                var postOcclusionFarDistance = remainingDistanceToFarRadius * distanceReduction + distance;
                occlusion = postOcclusionFarDistance / maxAudibleRange;
            }

            return occlusion;
        }

        private float CalculateDirectionality(Quaternion listenerHeadRotation, Quaternion playerHeadRotation,
            Vector3 directionToPlayer,
            float listenerDirectionality,
            float playerDirectionality)
        {
            var listenerForward = listenerHeadRotation * Vector3.forward;
            var playerBackward = playerHeadRotation * Vector3.back;

            var dotListener = 0.5f * (Vector3.Dot(listenerForward, directionToPlayer) + 1f);
            var dotSource = 0.5f * (Vector3.Dot(playerBackward, directionToPlayer) + 1f);

            return Mathf.Clamp01(dotListener + (1 - listenerDirectionality)) *
                   Mathf.Clamp01(dotSource + (1 - playerDirectionality));
        }

        private float CalculateOcclusion(Vector3 listenerHead,
            Vector3 direction,
            float distance,
            float occlusionFactor,
            float playerOcclusionFactor,
            int playerOcclusionMask)
        {
            var occlusionIsOff = Mathf.Abs(occlusionFactor - 1f) < 0.01f
                                 && Mathf.Abs(playerOcclusionFactor - 1f) < 0.01f;
            if (occlusionIsOff)
            {
                // don't waste time ray casting when it doesn't have any effect
                return 1f;
            }

            var hits = Physics.RaycastNonAlloc(listenerHead,
                direction,
                _rayHits,
                distance,
                playerOcclusionMask);


            if (hits == 0)
            {
                // nothing to do
                return 1f;
            }

            // if the UI layer is used for occlusion (UI layer contains the player capsules) allow at least one hit
            var playersCanOcclude = (playerOcclusionMask | UILayerMask) > 0;
            if (!playersCanOcclude)
            {
                // result when players can't occlude other players
                return hits > 0 ? occlusionFactor : 1f;
            }

            if (hits < 2)
            {
                // sometimes the other player's head leaves it's own UI player capsule which causes
                // the number of hits to go down by 1
                // or there was no environment hit while the player UI capsule was hit

                // check how far away the hit is from the player and if it is above a certain threshold
                // assume an object occludes the player (threshold is 1m for now)
                // TODO find a solution that also works for taller avatars for which the radius of the capsule can exceed 1m
                var minOcclusionTriggerDistance = distance - 1f;
                var occlusionTriggered = _rayHits[0].distance < minOcclusionTriggerDistance;
                if (!occlusionTriggered)
                {
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
            var anotherPlayerOccludes = !_rayHits[0].transform && !_rayHits[1].transform;
            if (anotherPlayerOccludes)
            {
                return playerOcclusionFactor;
            }

            // just return the occlusion factor for everything else
            return occlusionFactor;
        }

        private void UpdateVoiceAudio(VRCPlayerApi vrcPlayerApi,
            float distanceFactor,
            bool enableVoiceLowpass,
            float targetVoiceGain,
            float targetVoiceDistanceFar,
            float targetVoiceDistanceNear,
            float targetVoiceVolumetricRadius)
        {
            vrcPlayerApi.SetVoiceLowpass(enableVoiceLowpass);

            vrcPlayerApi.SetVoiceGain(targetVoiceGain * distanceFactor);
            vrcPlayerApi.SetVoiceDistanceFar(targetVoiceDistanceFar * distanceFactor);
            vrcPlayerApi.SetVoiceDistanceNear(targetVoiceDistanceNear * distanceFactor);
            vrcPlayerApi.SetVoiceVolumetricRadius(targetVoiceVolumetricRadius);
        }


        private void UpdateAvatarAudio(VRCPlayerApi vrcPlayerApi, float occlusion,
            bool forceAvatarSpatialAudio,
            bool allowAvatarCustomAudioCurves,
            float targetAvatarGain,
            float targetAvatarFarRadius,
            float targetAvatarNearRadius,
            float targetAvatarVolumetricRadius)
        {
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
        private int UpdatePlayerList()
        {
            var playerCount = VRCPlayerApi.GetPlayerCount();
            if (_players == null || _players.Length < playerCount)
            {
                _players = new VRCPlayerApi[playerCount];
            }

            VRCPlayerApi.GetPlayers(_players);
            return playerCount;
        }

        public bool IsOwner()
        {
            return _isReallyOwner;
        }

        public override void OnDeserialization()
        {
            if (_isReallyOwner)
            {
                _isReallyOwner = false;
            }

            TryUseMasterValues();
        }

        public override void OnPreSerialization()
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] " + $"OnPreSerialization called");
            if (Networking.IsOwner(gameObject))
            {
                _isReallyOwner = true;

                masterOcclusionFactor = OcclusionFactor;
                masterPlayerOcclusionFactor = PlayerOcclusionFactor;
                masterListenerDirectionality = ListenerDirectionality;
                masterPlayerDirectionality = PlayerDirectionality;
                masterEnableVoiceLowpass = EnableVoiceLowpass;
                masterTargetVoiceDistanceNear = TargetVoiceDistanceNear;
                masterTargetVoiceDistanceFar = TargetVoiceDistanceFar;
                masterTargetVoiceGain = TargetVoiceGain;
                masterTargetVoiceVolumetricRadius = TargetVoiceVolumetricRadius;
                masterForceAvatarSpatialAudio = ForceAvatarSpatialAudio;
                masterAllowAvatarCustomAudioCurves = AllowAvatarCustomAudioCurves;
                masterTargetAvatarNearRadius = TargetAvatarNearRadius;
                masterTargetAvatarFarRadius = TargetAvatarFarRadius;
                masterTargetAvatarGain = TargetAvatarGain;
                masterTargetAvatarVolumetricRadius = TargetAvatarVolumetricRadius;

                Debug.Log($"OnPreSerialization: New synchronized Values in BetterPlayerAudio "
                          + $"{masterOcclusionFactor}, "
                          + $"{masterPlayerOcclusionFactor}, "
                          + $"{masterListenerDirectionality}, "
                          + $"{masterPlayerDirectionality}, "
                          + $"{masterEnableVoiceLowpass}, "
                          + $"{masterTargetVoiceDistanceNear}, "
                          + $"{masterTargetVoiceDistanceFar}, "
                          + $"{masterTargetVoiceGain}, "
                          + $"{masterTargetVoiceVolumetricRadius}, "
                          + $"{masterForceAvatarSpatialAudio}, "
                          + $"{masterAllowAvatarCustomAudioCurves}, "
                          + $"{masterTargetAvatarNearRadius}, "
                          + $"{masterTargetAvatarFarRadius}, "
                          + $"{masterTargetAvatarGain}, "
                          + $"{masterTargetAvatarVolumetricRadius}");

                Debug.Log($"[<color=#008000>BetterAudio</color>] " +
                          $"OnPreSerialization: serialized data (queue length = {_serializationRequests})");
            }
            else
            {
                Debug.LogWarning("[<color=#008000>BetterAudio</color>] " +
                                 "Is not really owner but tries to serialize data");
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            _allowMasterControl = false;
            if (optionalUiController)
            {
                optionalUiController.SendCustomEvent(updateUiEventName);
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject))
            {
                _allowMasterControl = false;
            }

            if (optionalUiController)
            {
                optionalUiController.SendCustomEvent(updateUiEventName);
            }
        }

        public void SetUseMasterControls(bool useMasterValues)
        {
            if (useMasterValues && !_allowMasterControl)
            {
                _allowMasterControl = true;
                udonDebug.Assert(TryUseMasterValues(), "Failed using master values", this);
            }
            else
            {
                _allowMasterControl = useMasterValues;
            }
        }

        public bool AllowMasterTakeControl()
        {
            return _allowMasterControl;
        }

        private bool TryUseMasterValues()
        {
            if (_allowMasterControl && optionalUiController)
            {
                OcclusionFactor = masterOcclusionFactor;
                PlayerOcclusionFactor = masterPlayerOcclusionFactor;
                ListenerDirectionality = masterListenerDirectionality;
                PlayerDirectionality = masterPlayerDirectionality;
                EnableVoiceLowpass = masterEnableVoiceLowpass;
                TargetVoiceDistanceNear = masterTargetVoiceDistanceNear;
                TargetVoiceDistanceFar = masterTargetVoiceDistanceFar;
                TargetVoiceGain = masterTargetVoiceGain;
                TargetVoiceVolumetricRadius = masterTargetVoiceVolumetricRadius;
                ForceAvatarSpatialAudio = masterForceAvatarSpatialAudio;
                AllowAvatarCustomAudioCurves = masterAllowAvatarCustomAudioCurves;
                TargetAvatarNearRadius = masterTargetAvatarNearRadius;
                TargetAvatarFarRadius = masterTargetAvatarFarRadius;
                TargetAvatarGain = masterTargetAvatarGain;
                TargetAvatarVolumetricRadius = masterTargetAvatarVolumetricRadius;

                optionalUiController.SendCustomEvent(updateUiEventName);
                return true;
            }

            return false;
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
        public void IgnorePlayer(VRCPlayerApi playerToIgnore)
        {
            // validate the player
            if (!Utilities.IsValid(playerToIgnore))
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] " +
                               "BetterPlayerAudio.IgnorePlayer: invalid argument");
                return;
            }

            var vrcPlayerApi = VRCPlayerApi.GetPlayerById(playerToIgnore.playerId);
            if (!Utilities.IsValid(vrcPlayerApi))
            {
                Debug.LogError(
                    $"[<color=#008000>BetterAudio</color>] BetterPlayerAudio.IgnorePlayer: player {playerToIgnore} doesn't exist");
                return;
            }

            var noPlayerIgnoredYet = _playersToIgnore == null || _playersToIgnore.Length < 1;
            if (noPlayerIgnoredYet)
            {
                // simply add the player and return
                _playersToIgnore = new[]
                {
                    vrcPlayerApi.playerId
                };
                return;
            }

            // make sure all contained players are still alive, otherwise remove them
            var validPlayers = 0;
            var stillValidIgnoredPlayers = new int[_playersToIgnore.Length];
            var playerAdded = false;

            foreach (var playerId in _playersToIgnore)
            {
                if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(playerId)))
                {
                    // skip (=remove) the player
                    continue;
                }

                // keep all valid players
                stillValidIgnoredPlayers[validPlayers] = playerId;
                ++validPlayers;

                // keep track if the player is already in the array while validating all players in the array
                var playerIsAlreadyIgnored = playerId == vrcPlayerApi.playerId;
                if (playerIsAlreadyIgnored)
                {
                    playerAdded = true;
                    continue;
                }

                // insert the new player at the current position if the insert position is found
                var insertPositionFound = playerId < vrcPlayerApi.playerId && !playerAdded;
                if (!insertPositionFound)
                {
                    continue;
                }

                var longerStillValidIgnoredPlayers = new int[stillValidIgnoredPlayers.Length + 1];
                stillValidIgnoredPlayers.CopyTo(longerStillValidIgnoredPlayers, 0);
                stillValidIgnoredPlayers = longerStillValidIgnoredPlayers;
                stillValidIgnoredPlayers[validPlayers] = vrcPlayerApi.playerId;
                ++validPlayers;
            }

            // shrink the validated array content (happens when ignored players have left the world)
            // and store it again in the old array
            _playersToIgnore = new int[validPlayers];
            for (var i = 0; i < validPlayers; i++)
            {
                _playersToIgnore[i] = stillValidIgnoredPlayers[i];
            }
        }

        /// <summary>
        /// Remove a player from the ignore list and let it be affected again by this script.
        /// The ignored players are internally kept in a sorted array (ascending by player id) which is cleaned up every
        /// time a player is removed.
        /// This function is local only.
        /// </summary>
        /// <param name="ignoredPlayer"></param>
        public void UnIgnorePlayer(VRCPlayerApi ignoredPlayer)
        {
            // validate the player
            if (ignoredPlayer == null)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] " +
                               "BetterPlayerAudio.UnIgnorePlayer: invalid argument");
                return;
            }

            var vrcPlayerApi = VRCPlayerApi.GetPlayerById(ignoredPlayer.playerId);
            if (vrcPlayerApi == null)
            {
                Debug.LogError($"[<color=#008000>BetterAudio</color>] " +
                               $"BetterPlayerAudio.UnIgnorePlayer: player {ignoredPlayer} doesn't exist");
                return;
            }

            if (_playersToIgnore == null || _playersToIgnore.Length < 2)
            {
                _playersToIgnore = null;
                return;
            }

            // make sure all contained players are still alive, otherwise remove them
            var validPlayers = 0;
            var stillValidIgnoredPlayers = new int[_playersToIgnore.Length];

            foreach (var playerId in _playersToIgnore)
            {
                if (VRCPlayerApi.GetPlayerById(playerId) == null)
                {
                    continue;
                }

                // keep all valid players
                stillValidIgnoredPlayers[validPlayers] = playerId;
                ++validPlayers;

                // decrement the index by one again if the player id is found
                if (playerId == vrcPlayerApi.playerId)
                {
                    --validPlayers;
                }
            }

            // shrink the validated array content (happens when ignored players have left the world)
            // and store it again in the old array
            _playersToIgnore = new int[validPlayers];
            for (var i = 0; i < validPlayers; i++)
            {
                _playersToIgnore[i] = stillValidIgnoredPlayers[i];
            }
        }


        public bool OverridePlayerSettings(BetterPlayerAudioOverride betterPlayerAudioOverride,
            VRCPlayerApi playerToAffect)
        {
            if (!udonDebug.Assert(Utilities.IsValid(betterPlayerAudioOverride),
                "betterPlayerAudioOverride invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(Utilities.IsValid(playerToAffect),
                "playerToAffect invalid", this))
            {
                return false;
            }

            if (playerToAffect.isLocal)
            {
                if (!udonDebug.Assert(Utilities.IsValid(localPlayerOverrideList),
                    "localPlayerOverrideList invalid", this))
                {
                    return false;
                }

                var playerAdded = localPlayerOverrideList.AddOverride(betterPlayerAudioOverride);
                if (!playerAdded)
                {
                    Debug.LogWarning(
                        $"Local player {playerToAffect.displayName} already has the override {betterPlayerAudioOverride.gameObject.name} in the priority list");
                }

                return playerAdded;
            }

            // check if the player already has an override
            var index = Array.BinarySearch(PlayersToOverride, playerToAffect.playerId);
            if (index > -1)
            {
                if (!udonDebug.Assert(Utilities.IsValid(_playerOverrideLists[index]),
                    "Missing playerOverrideList for player", this))
                {
                    return false;
                }

                // add the new override
                var playerAdded = _playerOverrideLists[index].AddOverride(betterPlayerAudioOverride);
                if (!playerAdded)
                {
                    Debug.LogWarning(
                        $"Player {playerToAffect.displayName} already has the override {betterPlayerAudioOverride.gameObject.name} in the priority list");
                }

                return playerAdded;
            }

            var newSize = CreateOverrideSlotForPlayer(playerToAffect);

            // get the index of the added player
            var position = Array.BinarySearch(PlayersToOverride, playerToAffect.playerId);
            // create a new list of overrides
            var tempOverrides = new BetterPlayerAudioOverrideList[newSize];

            // copy the first half up to the added player into a the new list
            if (position > 0)
            {
                Array.ConstrainedCopy(_playerOverrideLists, 0, tempOverrides, 0, position);
            }

            var prefabInstance = VRCInstantiate(cloneablePlayerList.gameObject);
            // insert the new entry for the added player
            var betterPlayerAudioOverrideList = prefabInstance.GetComponent<BetterPlayerAudioOverrideList>();
            if (!udonDebug.Assert(betterPlayerAudioOverrideList.AddOverride(betterPlayerAudioOverride),
                "Failed adding override to instantiated list prefab", this))
            {
                return false;
            }

            tempOverrides[position] = betterPlayerAudioOverrideList;

            // copy the remaining overrides for the unchanged second half of overriden players
            Array.ConstrainedCopy(_playerOverrideLists,
                position,
                tempOverrides,
                position + 1,
                _playerOverrideLists.Length - position);

            // replace the overrides with the new list of overrides
            _playerOverrideLists = tempOverrides;

            return true;
        }

        public int CreateOverrideSlotForPlayer(VRCPlayerApi playerToAffect)
        {
            // add a new override list for that player
            // add the player to the list of players that have overrides
            var newSize = PlayersToOverride.Length + 1;

            var tempArray = new int[newSize];
            Array.ConstrainedCopy(PlayersToOverride, 0, tempArray, 0, PlayersToOverride.Length);
            PlayersToOverride = tempArray;
            PlayersToOverride[PlayersToOverride.Length - 1] = playerToAffect.playerId;

            // sort it afterwards to allow binary search to work again
            Sort(PlayersToOverride);
            return newSize;
        }

        public bool ClearPlayerOverride(BetterPlayerAudioOverride betterPlayerAudioOverride, VRCPlayerApi playerToClear)
        {
            if (!udonDebug.Assert(Utilities.IsValid(playerToClear), "Player to clear invalid", this))
            {
                return false;
            }

            if (playerToClear.isLocal)
            {
                if (!udonDebug.Assert(Utilities.IsValid(localPlayerOverrideList), "localPlayerOverrideList invalid",
                    this))
                {
                    return false;
                }

                localPlayerOverrideList.RemoveOverride(betterPlayerAudioOverride);
                return true;
            }

            if (!udonDebug.Assert(PlayersToOverride != null && PlayersToOverride.Length > 0,
                "_playersToOverride is empty", this))
            {
                return false;
            }

            // ReSharper disable once PossibleNullReferenceException invalid as checked with udonDebug.Assert
            var temp = new int[PlayersToOverride.Length];
            PlayersToOverride.CopyTo(temp, 0);

            // remove all invalid players first
            foreach (var i in temp)
            {
                if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(i)))
                {
                    ClearSinglePlayerOverride(betterPlayerAudioOverride, i);
                }
            }

            // remove the actual player that was requested to be removed
            return ClearSinglePlayerOverride(betterPlayerAudioOverride, playerToClear.playerId);
        }

        private bool ClearSinglePlayerOverride(BetterPlayerAudioOverride betterPlayerAudioOverride, int playerId)
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] " +
                      $"ClearSinglePlayerOverride: clearing override settings for player {playerId}");

            var player = VRCPlayerApi.GetPlayerById(playerId);
            if (!udonDebug.Assert(Utilities.IsValid(player), "Player is invalid", this))
            {
                return false;
            }

            if (player.isLocal)
            {
                if (!udonDebug.Assert(Utilities.IsValid(localPlayerOverrideList), "localPlayerOverrideList invalid",
                    this))
                {
                    return false;
                }

                localPlayerOverrideList.RemoveOverride(betterPlayerAudioOverride);
                return true;
            }

            if (!udonDebug.Assert(PlayersToOverride.Length > 0, "_playersToOverride empty",
                this))
            {
                return false;
            }

            // check if the player already has an override
            var index = Array.BinarySearch(PlayersToOverride, playerId);
            if (!udonDebug.Assert(index > -1, "player has no override", this))
            {
                return false;
            }

            var remaining = _playerOverrideLists[index].RemoveOverride(betterPlayerAudioOverride);

            if (remaining > 0)
            {
                return true;
            }

#if UNITY_INCLUDE_TESTS
            DestroyImmediate(_playerOverrideLists[index]);            
#else
            Destroy(_playerOverrideLists[index]);
#endif

            PlayersToOverride[index] = int.MaxValue;

            var newSize = PlayersToOverride.Length - 1;
            Sort(PlayersToOverride);
            var tempArray = new int[newSize];
            Array.ConstrainedCopy(PlayersToOverride, 0, tempArray, 0, newSize);
            PlayersToOverride = tempArray;


            // create a new list of overrides
            var tempOverrides = new BetterPlayerAudioOverrideList[newSize];

            // copy the first half up to the added player into a the new list
            Array.ConstrainedCopy(_playerOverrideLists, 0, tempOverrides, 0, index);
            // copy the remaining overrides for the unchanged second half of overriden players
            var firstIndexSecondHalf = index + 1;
            Array.ConstrainedCopy(_playerOverrideLists,
                firstIndexSecondHalf,
                tempOverrides,
                index,
                _playerOverrideLists.Length - firstIndexSecondHalf);

            // replace the overrides with the new list of overrides
            _playerOverrideLists = tempOverrides;

            Debug.Log($"[<color=#008000>BetterAudio</color>] " + $"ClearSinglePlayerOverride: cleared");
            return true;
        }

        #region Sorting

        private void Sort(int[] array)
        {
            if (array == null || array.Length < 2)
            {
                return;
            }

            BubbleSort(array);
        }

        private void BubbleSort(int[] array)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
            {
                for (var j = 0; j < arrayLength - 1; j++)
                {
                    var next = j + 1;

                    if (array[j] > array[next])
                    {
                        var tmp = array[j];
                        array[j] = array[next];
                        array[next] = tmp;
                    }
                }
            }
        }

        #endregion

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return allowOwnershipTransfer && Utilities.IsValid(requestingPlayer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns>True when a player is not ignored and has no active override</returns>
        public bool UsesDefaultEffects(VRCPlayerApi player)
        {
            if (IsIgnored(player))
            {
                return false;
            }

            return !UsesVoiceOverride(player);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true when a player is affected neither by global nor by override effects</returns>
        public bool IsIgnored(VRCPlayerApi player)
        {
            var mustBeIgnored = false;

            var playersIgnored = _playersToIgnore != null && _playersToIgnore.Length > 0;
            if (playersIgnored)
            {
                mustBeIgnored = Array.BinarySearch(_playersToIgnore, player.playerId) > -1;
            }

            return mustBeIgnored;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if the player is not ignored and is assigned to at least one override</returns>
        public bool UsesVoiceOverride(VRCPlayerApi player)
        {
            if (IsIgnored(player))
            {
                return false;
            }

            return HasVoiceOverrides(player);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if the player is assigned to at least one override</returns>
        public bool HasVoiceOverrides(VRCPlayerApi player)
        {
            if (!udonDebug.Assert(Utilities.IsValid(player), "Player invalid", this))
            {
                return false;
            }

            if (player.isLocal)
            {
                return udonDebug.Assert(Utilities.IsValid(localPlayerOverrideList), "localPlayerOverrideList invalid",
                           this)
                       && Utilities.IsValid(localPlayerOverrideList.GetMaxPriority());
            }

            var noOverridesAvailable = PlayersToOverride == null || PlayersToOverride.Length == 0;
            if (noOverridesAvailable)
            {
                return false;
            }

            return Array.BinarySearch(PlayersToOverride, player.playerId) > -1;
        }

        public BetterPlayerAudioOverride GetMaxPriorityOverride(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
            {
                return null;
            }

            if (player.isLocal)
            {
                if (!Utilities.IsValid(localPlayerOverrideList))
                {
                    return null;
                }

                return localPlayerOverrideList.GetMaxPriority();
            }

            var playersWithOverride = PlayersToOverride != null && PlayersToOverride.Length > 0;
            var playerOverridesExist = _playerOverrideLists != null && _playerOverrideLists.Length > 0;
            if (!playersWithOverride || !playerOverridesExist)
            {
                return null;
            }

            var index = Array.BinarySearch(PlayersToOverride, player.playerId);

            if (index > -1 && index < _playerOverrideLists.Length && Utilities.IsValid(_playerOverrideLists[index]))
            {
                return _playerOverrideLists[index].GetMaxPriority();;
            }

            return null;
        }

        #region Public

        /// <summary>
        /// local player not included if it has an override
        /// </summary>
        /// <returns>copy of array of player ids or empty array</returns>
        public int[] GetNonLocalPlayersWithOverrides()
        {
            if (PlayersToOverride == null)
            {
                return new int[0];
            }

            var temp = new int[PlayersToOverride.Length];
            PlayersToOverride.CopyTo(temp, 0);
            return temp;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="audioReverbFilter">Clears the reverb settings when null, otherwise copies and applies the values</param>
        internal void UseReverbSettings(AudioReverbFilter audioReverbFilter)
        {
            if (!udonDebug.Assert(Utilities.IsValid(mainAudioReverbFilter), "mainAudioReverbFilter invalid", this))
            {
                return;
            }

            var clearSettings = !Utilities.IsValid(audioReverbFilter);
            if (clearSettings)
            {
                mainAudioReverbFilter.reverbPreset = AudioReverbPreset.Off;
                return;
            }
            
            var newPreset = audioReverbFilter.reverbPreset;
            audioReverbFilter.enabled = true;
            audioReverbFilter.gameObject.SetActive(true);

            mainAudioReverbFilter.reverbPreset = newPreset;
            var doesNotNeedToCopyCustomSettings = newPreset != AudioReverbPreset.User;
            if (doesNotNeedToCopyCustomSettings)
            {
                return;
            }
            
            mainAudioReverbFilter.density = audioReverbFilter.density;
            mainAudioReverbFilter.diffusion = audioReverbFilter.diffusion;
            mainAudioReverbFilter.room = audioReverbFilter.room;
            mainAudioReverbFilter.roomHF = audioReverbFilter.roomHF;
            mainAudioReverbFilter.roomLF = audioReverbFilter.roomLF;
            mainAudioReverbFilter.decayTime = audioReverbFilter.decayTime;
            mainAudioReverbFilter.dryLevel = audioReverbFilter.dryLevel;
            mainAudioReverbFilter.hfReference = audioReverbFilter.hfReference;
            mainAudioReverbFilter.lfReference = audioReverbFilter.lfReference;
            mainAudioReverbFilter.reflectionsDelay = audioReverbFilter.reflectionsDelay;
            mainAudioReverbFilter.reflectionsLevel = audioReverbFilter.reflectionsLevel;
            mainAudioReverbFilter.reverbDelay = audioReverbFilter.reverbDelay;
            mainAudioReverbFilter.reverbLevel = audioReverbFilter.reverbLevel;
            mainAudioReverbFilter.decayHFRatio = audioReverbFilter.decayHFRatio;
        }

        #endregion

        #region internal

        internal BetterPlayerAudioOverride UpdateAudioFilters(BetterPlayerAudioOverride newOverride, BetterPlayerAudioOverride oldOverride)
        {
            if (newOverride == oldOverride)
            {
                return newOverride;
            }

            if (Utilities.IsValid(newOverride))
            {
                UseReverbSettings(newOverride.optionalReverb);
                return newOverride;
            }
            
            UseReverbSettings(null);
            return null;
        }

        #endregion
    }
}