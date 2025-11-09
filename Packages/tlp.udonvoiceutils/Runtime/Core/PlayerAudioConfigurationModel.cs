using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    /// <summary>
    /// Data model of the Debug Menu. Can be synchronized with other players if set to manual sync.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(
            typeof(PlayerAudioConfigurationModel),
            ExecutionOrder,
            TlpExecutionOrder.AudioStart,
            TlpExecutionOrder.AudioEnd)]
    public class PlayerAudioConfigurationModel : Model
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioOverride.ExecutionOrder + 1;
        #endregion

        #region Constants
        public const int EnvironmentLayerMask = 1 << 11;
        public const int UILayerMask = 1 << 5;
        #endregion

        #region Networking Settings
        /// <summary>
        /// When enabled the master can change the settings of all players
        /// </summary>
        [FormerlySerializedAs("LocalOnly_AllowMasterControl")]
        [FormerlySerializedAs("AllowMasterControl")]
        [Tooltip("When enabled the master can change the settings of all players")]
        public bool AllowMasterControlLocalValues;
        #endregion

        #region Audio Configuration
        #region Occlusion settings
        [Header("Occlusion settings")]
        /// <summary>
        /// Layers which can reduce voice and avatar sound effects when they are in between the local player (listener)
        /// and the player/avatar producing the sound
        /// Default layers: 11 and 5 (Environment and UI which includes the capsule colliders of other players)
        /// </summary>
        [Tooltip(
                "Objects on these layers reduce the voice/avatar sound volume when they are in-between the local player and the player/avatar that produces the sound"
        )]
        public LayerMask OcclusionMask = EnvironmentLayerMask | UILayerMask;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 0.0 means occlusion is off. A value of 1.0 will reduce the max. audible range of the
        /// voice/player to the current distance and make him/her/them in-audible
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
                "A value of ß.0 means occlusion is off. A value of 1 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible"
        )]
        public float OcclusionFactor;

        [UdonSynced]
        private float _syncedOcclusionFactor;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// Occlusion when a player is occluded by another player.
        /// A value of 0.0 means occlusion is off. A value of 0 will reduce the max. audible range of the
        /// voice/player to the current distance and make him/her/them in-audible
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
                "Occlusion when a player is occluded by another player. A value of 0.0 means occlusion is off. A value of 1.0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible"
        )]
        public float PlayerOcclusionFactor;

        [UdonSynced]
        private float _syncedPlayerOcclusionFactor;
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
                "A value of 1.0 reduces the ranges by up to 100% when the listener is facing away from a voice/avatar and thus making them more quiet."
        )]
        public float ListenerDirectionality;

        [UdonSynced]
        private float _syncedListenerDirectionality;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is
        /// facing away from the listener.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
                "A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is facing away from the listener."
        )]
        public float PlayerDirectionality;
        [UdonSynced]
        private float _syncedPlayerDirectionality;
        #endregion

        #region Voice Settings
        [Header("Voice settings")]
        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-disable-lowpass</remarks>
        /// </summary>
        [Tooltip("When enabled the voice of a player sounds muffled when close to the max. audible range.")]
        public bool EnableVoiceLowpass = true;

        [UdonSynced]
        private bool _syncedEnableVoiceLowpass;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-distance-near</remarks>
        /// </summary>
        [Tooltip("The volume will stay at max. when the player is closer than this distance.")]
        [Range(0, 1000000)]
        public float VoiceDistanceNear;

        [UdonSynced]
        private float _syncedVoiceDistanceNear;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/playewar-audio#set-voice-distance-far</remarks>
        /// </summary>
        [Tooltip("Beyond this distance the player can't be heard.")]
        [Range(0, 1000000)]
        public float VoiceDistanceFar = 25f;

        [UdonSynced]
        private float _syncedVoiceDistanceFar;

        /// <summary>
        /// Default is 15. In my experience this may lead to clipping when being close to someone with a loud microphone.
        /// My recommendation is to use 0 instead.
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-gain</remarks>
        /// </summary>
        [Tooltip("Additional volume increase. Changing this may require re-adjusting occlusion parameters!")]
        [Range(0, 24)]
        public float VoiceGain = 15f;

        [UdonSynced]
        private float _syncedVoiceGain;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-volumetric-radius</remarks>
        /// </summary>
        [Tooltip(
                "Range in which the player voice is not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed."
        )]
        [Range(0, 1000)]
        public float VoiceVolumetricRadius;

        [UdonSynced]
        private float _syncedVoiceVolumetricRadius;
        #endregion

        #region Avatar Sounds
        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudioforcespatial</remarks>
        /// </summary>
        [Tooltip("When set overrides all avatar audio sources to be spatialized.")]
        public bool ForceAvatarSpatialAudio;

        [UdonSynced]
        private bool _syncedForceAvatarSpatialAudio;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiocustomcurve</remarks>
        /// </summary>
        [Tooltip("When set custom audio curves on avatar audio sources are used.")]
        public bool AllowAvatarCustomAudioCurves = true;

        [UdonSynced]
        private bool _syncedAllowAvatarCustomAudioCurves;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudionearradius</remarks>
        /// </summary>
        [Tooltip("Max. distance at which player audio sources start to fall of in volume.")]
        public float AvatarNearRadius = 40f;

        [UdonSynced]
        private float _syncedAvatarNearRadius;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiofarradius</remarks>
        /// </summary>
        [Tooltip("Max. allowed distance at which player audio sources can be heard.")]
        public float AvatarFarRadius = 40f;

        [UdonSynced]
        private float _syncedAvatarFarRadius;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiogain</remarks>
        /// </summary>
        [Range(0, 10)]
        [Tooltip("Volume increase in decibel.")]
        public float AvatarGain = 10f;

        [UdonSynced]
        private float _syncedAvatarGain;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiovolumetricradius</remarks>
        /// </summary>
        [Tooltip(
                "Range in which the player audio sources are not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed."
        )]
        public float AvatarVolumetricRadius;

        [UdonSynced]
        private float _syncedAvatarVolumetricRadius;

        [Header("Settings affecting Avatar and Voice")]
        /// <summary>
        /// Defines how other players avatar eye height affects their audio ranges.
        /// On the X-axis is the eye height, on the y-axis is the voice range multiplier.
        ///
        /// Leave at a constant value of 1.0 to turn off avatar height based voice range changes.
        /// </summary>
        [Tooltip(
                "Defines how other players avatar eye height affects their audio ranges.\n"
                + "On the X-axis is the eye height, on the y-axis is the voice range multiplier.\n\n"
                + "Leave at a constant value of 1.0 to turn off avatar height based voice range changing."
        )]
        public AnimationCurve HeightToVoiceCorrelation = AnimationCurve.Constant(0, 25, 1f);
        #endregion
        #endregion

        #region Udon Lifecycle
        public void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (!IsModelInitialized) {
                DebugLog($"{nameof(OnEnable)}: Skipping {nameof(NotifyIfDirty)} as not yet initialized");
                return;
            }

            Dirty = true;
            NotifyIfDirty(1);
        }
        #endregion

        #region Callbacks
        protected virtual void OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnModelChanged));
#endif
            #endregion
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (Networking.GetOwner(gameObject) == Networking.LocalPlayer) {
                CopyLocalCopyToNetworkData();
                RequestSerialization();
            }

            return base.SetupAndValidate();
        }

        public override void OnEvent(string eventName) {
            if (!HasStartedOk) {
                return;
            }

            switch (eventName) {
                case nameof(OnModelChanged):

                    #region TLP_DEBUG
#if TLP_DEBUG
                    DebugLog_OnEvent(eventName);
#endif
                    #endregion

                    OnModelChanged();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }


        public override void OnPreSerialization() {
            CopyLocalCopyToNetworkData();
            base.OnPreSerialization();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);

            if (!HasStartedOk) {
                return;
            }

            CopyNetworkDataToLocalCopy();

            if (!IsModelInitialized) {
                Warn($"Skipping {nameof(OnDeserialization)} as not yet initialized");
                return;
            }

            Dirty = true;
            NotifyIfDirty(1);
        }
        #endregion

        #region Internal
        private void CopyNetworkDataToLocalCopy() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CopyNetworkDataToLocalCopy));
#endif
            #endregion

            UpdateValueLogged(ref OcclusionFactor, nameof(OcclusionFactor), _syncedOcclusionFactor);
            UpdateValueLogged(ref PlayerOcclusionFactor, nameof(PlayerOcclusionFactor), _syncedPlayerOcclusionFactor);
            UpdateValueLogged(
                    ref ListenerDirectionality,
                    nameof(ListenerDirectionality),
                    _syncedListenerDirectionality);
            UpdateValueLogged(ref PlayerDirectionality, nameof(PlayerDirectionality), _syncedPlayerDirectionality);
            UpdateValueLogged(ref EnableVoiceLowpass, nameof(EnableVoiceLowpass), _syncedEnableVoiceLowpass);
            UpdateValueLogged(ref VoiceDistanceNear, nameof(VoiceDistanceNear), _syncedVoiceDistanceNear);
            UpdateValueLogged(ref VoiceDistanceFar, nameof(VoiceDistanceFar), _syncedVoiceDistanceFar);
            UpdateValueLogged(ref VoiceGain, nameof(VoiceGain), _syncedVoiceGain);
            UpdateValueLogged(ref VoiceVolumetricRadius, nameof(VoiceVolumetricRadius), _syncedVoiceVolumetricRadius);
            UpdateValueLogged(
                    ref ForceAvatarSpatialAudio,
                    nameof(ForceAvatarSpatialAudio),
                    _syncedForceAvatarSpatialAudio);
            UpdateValueLogged(
                    ref AllowAvatarCustomAudioCurves,
                    nameof(AllowAvatarCustomAudioCurves),
                    _syncedAllowAvatarCustomAudioCurves);
            UpdateValueLogged(ref AvatarNearRadius, nameof(AvatarNearRadius), _syncedAvatarNearRadius);
            UpdateValueLogged(ref AvatarFarRadius, nameof(AvatarFarRadius), _syncedAvatarFarRadius);
            UpdateValueLogged(ref AvatarGain, nameof(AvatarGain), _syncedAvatarGain);
            UpdateValueLogged(
                    ref AvatarVolumetricRadius,
                    nameof(AvatarVolumetricRadius),
                    _syncedAvatarVolumetricRadius);
        }

        private void CopyLocalCopyToNetworkData() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CopyLocalCopyToNetworkData));
#endif
            #endregion

            UpdateValueLogged(ref _syncedOcclusionFactor, nameof(_syncedOcclusionFactor), OcclusionFactor);
            UpdateValueLogged(
                    ref _syncedPlayerOcclusionFactor,
                    nameof(_syncedPlayerOcclusionFactor),
                    PlayerOcclusionFactor);
            UpdateValueLogged(
                    ref _syncedListenerDirectionality,
                    nameof(_syncedListenerDirectionality),
                    ListenerDirectionality);
            UpdateValueLogged(
                    ref _syncedPlayerDirectionality,
                    nameof(_syncedPlayerDirectionality),
                    PlayerDirectionality);
            UpdateValueLogged(ref _syncedEnableVoiceLowpass, nameof(_syncedEnableVoiceLowpass), EnableVoiceLowpass);
            UpdateValueLogged(ref _syncedVoiceDistanceNear, nameof(_syncedVoiceDistanceNear), VoiceDistanceNear);
            UpdateValueLogged(ref _syncedVoiceDistanceFar, nameof(_syncedVoiceDistanceFar), VoiceDistanceFar);
            UpdateValueLogged(ref _syncedVoiceGain, nameof(_syncedVoiceGain), VoiceGain);
            UpdateValueLogged(
                    ref _syncedVoiceVolumetricRadius,
                    nameof(_syncedVoiceVolumetricRadius),
                    VoiceVolumetricRadius);
            UpdateValueLogged(
                    ref _syncedForceAvatarSpatialAudio,
                    nameof(_syncedForceAvatarSpatialAudio),
                    ForceAvatarSpatialAudio);
            UpdateValueLogged(
                    ref _syncedAllowAvatarCustomAudioCurves,
                    nameof(_syncedAllowAvatarCustomAudioCurves),
                    AllowAvatarCustomAudioCurves);
            UpdateValueLogged(ref _syncedAvatarNearRadius, nameof(_syncedAvatarNearRadius), AvatarNearRadius);
            UpdateValueLogged(ref _syncedAvatarFarRadius, nameof(_syncedAvatarFarRadius), AvatarFarRadius);
            UpdateValueLogged(ref _syncedAvatarGain, nameof(_syncedAvatarGain), AvatarGain);
            UpdateValueLogged(
                    ref _syncedAvatarVolumetricRadius,
                    nameof(_syncedAvatarVolumetricRadius),
                    AvatarVolumetricRadius);
        }

        internal static void UpdateValueLogged<T>(ref T old, string variable, T replacement) {
            #region TLP_DEBUG
#if TLP_DEBUG
            TlpLogger.StaticDebugLog($"Updating {variable} from {old} to {replacement}", null);
#endif
            #endregion

            old = replacement;
        }
        #endregion
    }
}