using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterPlayerAudio : UdonSharpBehaviour
    {
        [Header("General Settings")] [SerializeField]
        private UdonBehaviour uiController;

        /// <summary>
        /// The name of the function in the UI controller script that should be called when the master control
        /// is enabled and the master changed any value
        /// </summary>
        [Tooltip(
            "The name of the function in the UI controller script that should be called when the master control is enabled and the master changed any value")]
        [SerializeField]
        private string updateUiEventName = "UpdateUi";

        /// <summary>
        /// Default layer: 11 (Environment)
        /// </summary>
        public LayerMask occlusionMask = 1 << 11;

        #region default values for resetting

        /// <summary>
        /// When enabled the master can change the settings of all players
        /// </summary>
        [Tooltip("When enabled the master can change the settings of all players")]
        public bool defaultAllowMasterControl = false;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the
        /// voice/player to the current distance and make him/her/them in-audible
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float defaultOcclusionFactor = 0.5f;

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
        public float defaultPlayerDirectionality = 0.5f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-disable-lowpass</remarks>
        /// </summary>
        [Header("Voice Settings")] public bool defaultEnableVoiceLowpass = true;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-distance-near</remarks>
        /// </summary>
        [Range(0, 1000000)] public float defaultVoiceDistanceNear = 0f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-distance-far</remarks>
        /// </summary>
        [Range(0, 1000000)] public float defaultVoiceDistanceFar = 100f;

        /// <summary>
        /// Default is 15. In my experience this may lead to clipping when being close to someone with a loud microphone.
        /// My recommendation is to use 0 instead.
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-gain</remarks>
        /// </summary>
        [Range(0, 24)] public float defaultVoiceGain = 15f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-volumetric-radius</remarks>
        /// </summary>
        [Range(0, 1000)] public float defaultVoiceVolumetricRadius = 0f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudioforcespatial</remarks>
        /// </summary>
        [Header("Avatar Settings")] public bool defaultForceAvatarSpatialAudio = false;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiocustomcurve</remarks>
        /// </summary>
        public bool defaultAllowAvatarCustomAudioCurves = true;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudionearradius</remarks>
        /// </summary>
        public float defaultAvatarNearRadius = 1f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiofarradius</remarks>
        /// </summary>
        public float defaultAvatarFarRadius = 100f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiogain</remarks>
        /// </summary>
        [Range(0, 10)] public float defaultAvatarGain = 10f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiovolumetricradius</remarks>
        /// </summary>
        public float defaultAvatarVolumetricRadius = 0f;

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

        #region Synched values

        /// <summary>
        /// <inheritdoc cref="defaultOcclusionFactor"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterOcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="defaultListenerDirectionality"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterListenerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultPlayerDirectionality"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterPlayerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultEnableVoiceLowpass"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public bool masterEnableVoiceLowpass;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceNear"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterTargetVoiceDistanceNear;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceFar"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterTargetVoiceDistanceFar;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceGain"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterTargetVoiceGain;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceVolumetricRadius"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterTargetVoiceVolumetricRadius;

        /// <summary>
        /// <inheritdoc cref="defaultForceAvatarSpatialAudio"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public bool masterForceAvatarSpatialAudio;

        /// <summary>
        /// <inheritdoc cref="defaultAllowAvatarCustomAudioCurves"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public bool masterAllowAvatarCustomAudioCurves;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarNearRadius"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterTargetAvatarNearRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarFarRadius"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterTargetAvatarFarRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarGain"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterTargetAvatarGain;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarVolumetricRadius"/>
        /// </summary>
        [UdonSynced][HideInInspector]  public float masterTargetAvatarVolumetricRadius;

        #endregion

        private bool _initialized;
        private int _playerIndex = 0;
        private int _playerCount;
        private VRCPlayerApi[] _players = new VRCPlayerApi[1];
        private readonly RaycastHit[] _rayHits = new RaycastHit[1];

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            UpdatePlayerList();
            if (_playerCount < 2) return;
            _playerIndex = (_playerIndex + 1) % _playerCount;

            var vrcPlayerApi = _players[_playerIndex];
            if (vrcPlayerApi == null) return;

            // skip local player
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null || vrcPlayerApi.playerId == localPlayer.playerId) return;

            var listenerHead = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var otherPlayerHead = vrcPlayerApi.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            var listenerToPlayer = (otherPlayerHead.position - listenerHead.position);
            var direction = listenerToPlayer.normalized;
            var distance = listenerToPlayer.magnitude;

            var occlusionFactor = CalculateOcclusion(listenerHead.position, direction, distance, OcclusionFactor);
            var directionality = CalculateDirectionality(listenerHead.rotation, otherPlayerHead.rotation, direction);

            var distanceReduction = directionality * occlusionFactor;
            var voiceDistanceFactor =
                CalculateRangeReduction(distance, distanceReduction, TargetVoiceDistanceFar);
            UpdateVoiceAudio(vrcPlayerApi, voiceDistanceFactor);

            var avatarDistanceFactor = CalculateRangeReduction(distance, distanceReduction, TargetAvatarFarRadius);
            UpdateAvatarAudio(vrcPlayerApi, avatarDistanceFactor);
        }

        #endregion

        /// <summary>
        /// initializes all runtime variables using the default values.
        /// Has no effect if called again or if start() was already received.
        /// To reset values use ResetToDefault() instead.
        /// </summary>
        public void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                ResetToDefault();
            }
        }

        public void ResetToDefault()
        {
            OcclusionFactor = defaultOcclusionFactor;
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
            Vector3 directionToPlayer)
        {
            var listenerForward = listenerHeadRotation * Vector3.forward;
            var playerBackward = playerHeadRotation * Vector3.back;


            var dotListener = 0.5f * (Vector3.Dot(listenerForward, directionToPlayer) + 1f);
            var dotSource = 0.5f * (Vector3.Dot(playerBackward, directionToPlayer) + 1f);

            return Mathf.Clamp01(dotListener + (1 - ListenerDirectionality)) *
                   Mathf.Clamp01(dotSource + (1 - PlayerDirectionality));
        }

        private float CalculateOcclusion(Vector3 listenerHead, Vector3 direction, float distance, float occlusionFactor)
        {
            if (Mathf.Abs(occlusionFactor - 1f) < 0.01f)
            {
                // don't waste time ray casting when it doesn't have any effect
                return 1f;
            }

            var hits = Physics.RaycastNonAlloc(listenerHead,
                direction,
                _rayHits,
                distance,
                occlusionMask);

            return hits > 0 ? OcclusionFactor : 1f;
        }

        private void UpdateVoiceAudio(VRCPlayerApi vrcPlayerApi, float distanceFactor)
        {
            vrcPlayerApi.SetVoiceLowpass(EnableVoiceLowpass);

            vrcPlayerApi.SetVoiceGain(TargetVoiceGain * distanceFactor);
            vrcPlayerApi.SetVoiceDistanceFar(TargetVoiceDistanceFar * distanceFactor);
            vrcPlayerApi.SetVoiceDistanceNear(TargetVoiceDistanceNear * distanceFactor);
            vrcPlayerApi.SetVoiceVolumetricRadius(TargetVoiceVolumetricRadius);
        }

        private void UpdateAvatarAudio(VRCPlayerApi vrcPlayerApi, float occlusion)
        {
            vrcPlayerApi.SetAvatarAudioForceSpatial(ForceAvatarSpatialAudio);
            vrcPlayerApi.SetAvatarAudioCustomCurve(AllowAvatarCustomAudioCurves);

            vrcPlayerApi.SetAvatarAudioGain(TargetAvatarGain * occlusion);
            vrcPlayerApi.SetAvatarAudioFarRadius(TargetAvatarFarRadius * occlusion);
            vrcPlayerApi.SetAvatarAudioNearRadius(TargetAvatarNearRadius * occlusion);
            vrcPlayerApi.SetAvatarAudioVolumetricRadius(TargetAvatarVolumetricRadius);
        }

        private void UpdatePlayerList()
        {
            _playerCount = VRCPlayerApi.GetPlayerCount();
            if (_players == null || _players.Length < _playerCount)
            {
                _players = new VRCPlayerApi[_playerCount];
            }

            VRCPlayerApi.GetPlayers(_players);
        }

        public bool IsOwner()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                return localPlayer.isMaster;
            }

            return true;
        }

        public override void OnDeserialization()
        {
            UseMasterValues();
        }

        public override void OnPreSerialization()
        {
            if (IsOwner()){
                masterOcclusionFactor = OcclusionFactor;
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
            }
        }

        public void SetUseMasterControls(bool use)
        {
            if (use && !_allowMasterControl)
            {
                _allowMasterControl = true;
                UseMasterValues();
            }
            else
            {
                _allowMasterControl = use;
            }
        }

        public bool AllowMasterTakeControl()
        {
            return _allowMasterControl;
        }

        private void UseMasterValues()
        {
            if (_allowMasterControl && uiController)
            {
                OcclusionFactor = masterOcclusionFactor;
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

                uiController.SendCustomEvent(updateUiEventName);
            }
        }
    }
}