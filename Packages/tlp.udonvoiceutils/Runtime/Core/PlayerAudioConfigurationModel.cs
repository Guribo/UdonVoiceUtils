using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.Udon.Common;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    /// <summary>
    /// Data model of the Debug Menu. Can be synchronized with other players if set to manual sync.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    public class PlayerAudioConfigurationModel : Model
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Model.ExecutionOrder;

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
        [UdonSynced]
        public float OcclusionFactor;

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
        [UdonSynced]
        public float PlayerOcclusionFactor;
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
        [UdonSynced]
        public float ListenerDirectionality;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is
        /// facing away from the listener.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
                "A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is facing away from the listener."
        )]
        [UdonSynced]
        public float PlayerDirectionality;
        #endregion

        #region Voice Settings
        [Header("Voice settings")]
        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-disable-lowpass</remarks>
        /// </summary>
        [Tooltip("When enabled the voice of a player sounds muffled when close to the max. audible range.")]
        [UdonSynced]
        public bool EnableVoiceLowpass = true;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-distance-near</remarks>
        /// </summary>
        [Tooltip("The volume will stay at max. when the player is closer than this distance.")]
        [Range(0, 1000000)]
        [UdonSynced]
        public float VoiceDistanceNear;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/playewar-audio#set-voice-distance-far</remarks>
        /// </summary>
        [Tooltip("Beyond this distance the player can't be heard.")]
        [Range(0, 1000000)]
        [UdonSynced]
        public float VoiceDistanceFar = 25f;

        /// <summary>
        /// Default is 15. In my experience this may lead to clipping when being close to someone with a loud microphone.
        /// My recommendation is to use 0 instead.
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-gain</remarks>
        /// </summary>
        [Tooltip("Additional volume increase. Changing this may require re-adjusting occlusion parameters!")]
        [Range(0, 24)]
        [UdonSynced]
        public float VoiceGain = 15f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-volumetric-radius</remarks>
        /// </summary>
        [Tooltip(
                "Range in which the player voice is not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed."
        )]
        [Range(0, 1000)]
        [UdonSynced]
        public float VoiceVolumetricRadius;
        #endregion

        #region Avatar Sounds
        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudioforcespatial</remarks>
        /// </summary>
        [Tooltip("When set overrides all avatar audio sources to be spatialized.")]
        [UdonSynced]
        public bool ForceAvatarSpatialAudio;


        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiocustomcurve</remarks>
        /// </summary>
        [Tooltip("When set custom audio curves on avatar audio sources are used.")]
        [UdonSynced]
        public bool AllowAvatarCustomAudioCurves = true;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudionearradius</remarks>
        /// </summary>
        [Tooltip("Max. distance at which player audio sources start to fall of in volume.")]
        [UdonSynced]
        public float AvatarNearRadius = 40f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiofarradius</remarks>
        /// </summary>
        [Tooltip("Max. allowed distance at which player audio sources can be heard.")]
        [UdonSynced]
        public float AvatarFarRadius = 40f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiogain</remarks>
        /// </summary>
        [Range(0, 10)]
        [Tooltip("Volume increase in decibel.")]
        [UdonSynced]
        public float AvatarGain = 10f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiovolumetricradius</remarks>
        /// </summary>
        [Tooltip(
                "Range in which the player audio sources are not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed."
        )]
        [UdonSynced]
        public float AvatarVolumetricRadius;

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

            if (!Initialized) {
                Warn($"Skipping {nameof(OnEnable)} as not yet initialized");
                return;
            }

            Dirty = true;
            NotifyIfDirty(1);
        }
        #endregion

        #region Overrides
        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);

            if (!Initialized) {
                Warn($"Skipping {nameof(OnDeserialization)} as not yet initialized");
                return;
            }

            Dirty = true;
            NotifyIfDirty(1);
        }
        #endregion
    }
}