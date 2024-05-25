using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Player;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    /// <summary>
    /// This override contains values that can be used to override the default audio settings in
    /// <see cref="PlayerAudioController"/> for a group of players.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerAudioOverride : TlpBaseBehaviour
    {
        #region Settings
        [Header("General settings")]
        /// <summary>
        /// Determines whether it 
        /// Overrides with equal or higher values can override other overrides with lower priority.
        /// When removed from a player and it was currently the highest priority override it will
        /// fall back to the next lower override available.
        /// </summary>
        [FormerlySerializedAs("priority")]
        public int Priority;
        #endregion

        #region Occlusion settings
        #region Constants
        private const int EnvironmentLayerMask = 1 << 11;
        private const int UILayerMask = 1 << 5;
        #endregion


        [Header("Occlusion settings")]
        [Tooltip(
                "Objects on these layers reduce the voice/avatar sound volume when they are in-between " +
                "the local player and the player/avatar that produces the sound"
        )]
        [FormerlySerializedAs("occlusionMask")]
        /// <summary>
        /// <inheritdoc cref=""/>
        /// </summary>
        public LayerMask OcclusionMask = EnvironmentLayerMask | UILayerMask;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultOcclusionFactor"/>
        /// </summary>
        [FormerlySerializedAs("occlusionFactor")]
        [Range(0, 1)]
        [Tooltip(
                "A value of 0.0 means occlusion is off. A value of 1.0 will reduce the max. audible range of the " +
                "voice/player to the current distance and make him/her/them in-audible"
        )]
        public float OcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultPlayerOcclusionFactor"/>
        /// </summary>
        [FormerlySerializedAs("playerOcclusionFactor")]
        [Range(0, 1)]
        [Tooltip(
                "Occlusion when a player is occluded by another player. A value of 0.0 means occlusion is off." +
                " A value of 1.0 will reduce the max. audible range of the voice/player to the current distance and" +
                " make him/her/them in-audible"
        )]
        public float PlayerOcclusionFactor;
        #endregion

        #region Directionality settings
        [FormerlySerializedAs("listenerDirectionality")]
        [Header("Directionality settings")]
        [Range(0, 1)]
        [Tooltip(
                "A value of 1.0 reduces the ranges by up to 100% when the listener is facing away from a " +
                "voice/avatar and thus making them more quiet."
        )]
        public float ListenerDirectionality;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultPlayerDirectionality"/>
        /// </summary>
        [FormerlySerializedAs("playerDirectionality")]
        [Range(0, 1)]
        [Tooltip(
                "A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar " +
                "sounds but is facing away from the listener."
        )]
        public float PlayerDirectionality;
        #endregion

        #region Reverb settings
        [FormerlySerializedAs("optionalReverb")]
        [Header("Reverb settings")]
        public AudioReverbFilter OptionalReverb;
        #endregion

        #region Voice settings
        [FormerlySerializedAs("enableVoiceLowpass")]
        [Header("Voice settings")]
        [Tooltip("When enabled the voice of a player sounds muffled when close to the max. audible range.")]
        public bool EnableVoiceLowpass = true;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultVoiceDistanceNear"/>
        /// </summary>
        [FormerlySerializedAs("voiceDistanceNear")]
        [Tooltip("The volume will stay at max. when the player is closer than this distance.")]
        [Range(0, 1000000)]
        public float VoiceDistanceNear;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultVoiceDistanceFar"/>
        /// </summary>
        [FormerlySerializedAs("voiceDistanceFar")]
        [Tooltip("Beyond this distance the player can't be heard.")]
        [Range(0, 1000000)]
        public float VoiceDistanceFar = 25f;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultVoiceGain"/>
        /// </summary>
        [FormerlySerializedAs("voiceGain")]
        [Tooltip("Additional volume increase. Changing this may require re-adjusting occlusion parameters!")]
        [Range(0, 24)]
        public float VoiceGain = 15f;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultVoiceVolumetricRadius"/>
        /// </summary>
        [FormerlySerializedAs("voiceVolumetricRadius")]
        [Tooltip(
                "Range in which the player voice is not spatialized. Increases experienced volume by a lot! " +
                "May require extensive tweaking of gain/range parameters when being changed."
        )]
        [Range(0, 1000)]
        public float VoiceVolumetricRadius;
        #endregion

        #region Avatar settings
        [FormerlySerializedAs("forceAvatarSpatialAudio")]
        [Header("Avatar settings")]
        [Tooltip("When set overrides all avatar audio sources to be spatialized.")]
        public bool ForceAvatarSpatialAudio;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultAllowAvatarCustomAudioCurves"/>
        /// </summary>
        [FormerlySerializedAs("allowAvatarCustomAudioCurves")]
        [Tooltip("When set custom audio curves on avatar audio sources are used.")]
        public bool AllowAvatarCustomAudioCurves = true;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultAvatarNearRadius"/>
        /// </summary>
        [FormerlySerializedAs("targetAvatarNearRadius")]
        [Tooltip("Max. distance at which player audio sources start to fall of in volume.")]
        public float TargetAvatarNearRadius;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultAvatarFarRadius"/>
        /// </summary>
        [FormerlySerializedAs("targetAvatarFarRadius")]
        [Tooltip("Max. allowed distance at which player audio sources can be heard.")]
        public float TargetAvatarFarRadius = 40f;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultAvatarGain"/>
        /// </summary>
        [FormerlySerializedAs("targetAvatarGain")]
        [Range(0, 10)]
        [Tooltip("Volume increase in decibel.")]
        public float TargetAvatarGain = 10f;

        /// <summary>
        /// <inheritdoc cref="PlayerAudioController.defaultAvatarVolumetricRadius"/>
        /// </summary>
        [FormerlySerializedAs("targetAvatarVolumetricRadius")]
        [Tooltip(
                "Range in which the player audio sources are not spatialized. " +
                "Increases experienced volume by a lot! " +
                "May require extensive tweaking of gain/range parameters when being changed."
        )]
        public float TargetAvatarVolumetricRadius;
        #endregion

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

        #region Privacy Settings
        /// <summary>
        /// Players affected by different overrides with the same privacy channel id can hear each other and can't be
        /// heard by non-affected players.
        /// If set to -1 the feature is turned off for this override component and only players affected by this
        /// override can hear each other.
        /// </summary>
        [FormerlySerializedAs("privacyChannelId")]
        [Header("Privacy settings")]
        [Tooltip(
                "Players affected by different overrides with the same privacy channel id can hear each other and " +
                "can't be heard by non-affected players. If set to -1 the feature is turned off and all players affected " +
                "by this override can be heard."
        )]
        public int PrivacyChannelId = PlayerAudioController.ChannelNoPrivacy;


        /// <summary>
        /// If set to true affected players also can't hear non-affected players.
        /// Only in effect in combination with a privacy channel not equal to -1.
        /// </summary>
        [FormerlySerializedAs("muteOutsiders")]
        [Tooltip(
                "If set to true affected players can't hear non-affected players."
        )]
        public bool MuteOutsiders = true;

        /// <summary>
        /// prevents the local player from listening to any other player in the same channel, can be used to talk to
        /// players in a private room without being able to hear what is going on inside
        /// </summary>
        [FormerlySerializedAs("disallowListeningToChannel")]
        [Tooltip(
                "Prevents the local player from listening to any other player in the same channel, can be used to " +
                "talk to players in a private room without being able to hear what is going on inside"
        )]
        public bool DisallowListeningToChannel;
        #endregion

        #region Optional listeners
        [Header("Events")]
        [Tooltip(
                "Raised when the local player has been added (does not mean the override is active " +
                "e.g. when another override with higher priority is already active)"
        )]
        public UdonEvent LocalPlayerAdded;

        [Tooltip(
                "Raised when the local player has been removed (does not mean the override was active " +
                "e.g. when another override with higher priority was already active)"
        )]
        public UdonEvent LocalPlayerRemoved;
        #endregion

        #region Mandatory references
        [FormerlySerializedAs("playerAudioController")]
        [FormerlySerializedAs("betterPlayerAudio")]
        [Header("Mandatory references")]
        public PlayerAudioController PlayerAudioController;

        [FormerlySerializedAs("playerList")]
        public PlayerList PlayerList;
        #endregion

        #region State
        public bool Initialized { get; internal set; }
        #endregion

        #region Udon Lifecycle
        internal void OnEnable() {
            EnsureInitialized();

            ApplyToAffectedPlayers();
        }


        internal void OnDisable() {
            if (!Assert(PlayerList, "playerList invalid", this)) {
                return;
            }

            PlayerList.DiscardInvalid();
            if (PlayerList.Players == null) {
                return;
            }

            foreach (int playerId in PlayerList.Players) {
                var playerToRemove = VRCPlayerApi.GetPlayerById(playerId);
                if (!Utilities.IsValid(playerToRemove)) {
                    continue;
                }

                if (!PlayerAudioController.ClearPlayerOverride(this, playerToRemove)) {
                    PlayerList.DiscardInvalid();
                }

                if (playerToRemove.isLocal && Utilities.IsValid(LocalPlayerRemoved)) {
                    // DeactivateReverb();
                    LocalPlayerRemoved.Raise(this);
                }
            }
        }
        #endregion

        #region Public
        /// <summary>
        /// Add a single player to the list of players that should make use of the here defined override values.
        /// When adding multiple players use <see cref="ApplyToAffectedPlayers"/> instead.
        /// Methods can be expensive when call with a lot of players! Avoid using in Update in every frame!
        /// </summary>
        /// <param name="playerToAffect"></param>
        /// <returns>true if the player was added/was already added before</returns>
        public bool AddPlayer(VRCPlayerApi playerToAffect) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(AddPlayer));
#endif
            #endregion

            EnsureInitialized();

            if (!Utilities.IsValid(PlayerList)) {
                Error($"{nameof(PlayerList)} invalid");
                return false;
            }

            if (!Assert(Utilities.IsValid(playerToAffect), "Player to affect invalid", this)) {
                return false;
            }

            if (!PlayerList.AddPlayer(playerToAffect)) {
                Warn($"{playerToAffect.ToStringSafe()} is already affected");
                return true;
            }

            if (!IsActiveAndEnabled()) {
                Warn($"Override {gameObject.name} is not enabled for {playerToAffect.displayName}");
                return true;
            }

            if (!Assert(Utilities.IsValid(PlayerAudioController), $"{nameof(PlayerAudioController)} invalid", this)) {
                return false;
            }


            if (!PlayerAudioController.OverridePlayerSettings(this, playerToAffect)) {
                return false;
            }

            int _ = PlayerList.DiscardInvalid();

            if (playerToAffect.isLocal && Utilities.IsValid(LocalPlayerAdded)) {
                // ActivateReverb();
                LocalPlayerAdded.Raise(this);
            }

#if TLP_DEBUG
            Assert(PlayerList.Contains(playerToAffect), "Player not found in PlayerList", this);
#endif

            return true;
        }


        /// <summary>
        /// Remove player from the list of players that should make use of the here defined override values.
        /// </summary>
        /// <param name="playerToRemove"></param>
        /// <returns>true if the player was removed/not affected yet</returns>
        public bool RemovePlayer(VRCPlayerApi playerToRemove) {
#if TLP_DEBUG
            DebugLog(nameof(RemovePlayer));
#endif
            if (!Assert(PlayerList, "playerList invalid", this)) {
                return false;
            }

            if (!Assert(Utilities.IsValid(playerToRemove), "Player to remove invalid", this)) {
                return false;
            }

            if (!Assert(PlayerList.RemovePlayer(playerToRemove), "player not affected", this)) {
                return false;
            }

            if (!IsActiveAndEnabled()) {
                Warn($"Override {gameObject.name} is not enabled for {playerToRemove.displayName}");
                return true;
            }

            if (!Assert(Utilities.IsValid(PlayerAudioController), "PlayerAudio invalid", this)) {
                return false;
            }


            bool success = true;

            // make the controller apply default settings to the player again
            if (!PlayerAudioController.ClearPlayerOverride(this, playerToRemove)) {
                int oldLength = PlayerList.Players.Length;
                success = oldLength > PlayerList.DiscardInvalid();
            }

            if (playerToRemove.isLocal && Utilities.IsValid(LocalPlayerRemoved)) {
                // DeactivateReverb();
                LocalPlayerRemoved.Raise(this);
            }

            return success;
        }

        /// <summary>
        /// Check whether the player given as playerId should make use of the here defined override values
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns>true if the player should be affected</returns>
        public bool IsAffected(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(IsAffected));
#endif
            if (!Assert(PlayerList, "playerList invalid", this)) {
                return false;
            }

            return PlayerList.Contains(player);
        }

        public void Refresh() {
#if TLP_DEBUG
            DebugLog(nameof(Refresh));
#endif
            if (!Assert(Utilities.IsValid(PlayerAudioController), $"{nameof(PlayerAudioController)} invalid", this)) {
                return;
            }

            if (!Assert(Utilities.IsValid(PlayerList), "playerList invalid", this)) {
                return;
            }

            PlayerList.DiscardInvalid();

            int[] nonLocalPlayersWithOverrides = PlayerAudioController.GetNonLocalPlayersWithOverrides();
            foreach (int nonLocalPlayersWithOverride in nonLocalPlayersWithOverrides) {
                var vrcPlayerApi = VRCPlayerApi.GetPlayerById(nonLocalPlayersWithOverride);
                if (!PlayerList.Contains(vrcPlayerApi)) {
                    PlayerAudioController.ClearPlayerOverride(this, vrcPlayerApi);
                }
            }

            bool containsLocal = false;
            var localPlayer = Networking.LocalPlayer;

            if (!Assert(Utilities.IsValid(localPlayer), "Local player invalid", this)) {
                return;
            }

            int localPlayerId = localPlayer.playerId;

            foreach (int playerListPlayer in PlayerList.Players) {
                if (playerListPlayer == localPlayerId) {
                    containsLocal = true;
                }

                PlayerAudioController.OverridePlayerSettings(this, VRCPlayerApi.GetPlayerById(playerListPlayer));
            }

            // also remove from local player
            if (!containsLocal) {
                PlayerAudioController.ClearPlayerOverride(this, localPlayer);
            }
        }

        public bool Clear() {
            if (!Assert(Utilities.IsValid(PlayerAudioController), "playerAudio invalid", this)) {
                return false;
            }

            if (!Assert(Utilities.IsValid(PlayerList), "playerList invalid", this)) {
                return false;
            }

            PlayerList.DiscardInvalid();

            if (IsActiveAndEnabled()) {
                ClearPlayerOverrides();
            }

            PlayerList.Clear();
            return true;
        }

        public PlayerBlackList OptionalPlayerBlackList;


        public bool IsPlayerBlackListed(VRCPlayerApi player) {
            return Utilities.IsValid(OptionalPlayerBlackList) &&
                   OptionalPlayerBlackList.IsBlackListed(player.displayName);
        }

        internal void ClearPlayerOverrides() {
            if (PlayerList.Players != null) {
                foreach (int affectedPlayer in PlayerList.Players) {
                    PlayerAudioController.ClearPlayerOverride(this, VRCPlayerApi.GetPlayerById(affectedPlayer));
                }
            }
        }
        #endregion

        #region internal
        internal bool IsActiveAndEnabled() {
#if UNITY_INCLUDE_TESTS
            return Utilities.IsValid(this) && enabled;
#else
            return Utilities.IsValid(this) && enabled && gameObject.activeInHierarchy;
#endif
        }

        internal void EnsureInitialized() {
            if (Initialized) {
                return;
            }

            Initialized = true;

            if (Utilities.IsValid(OptionalReverb)) {
                OptionalReverb.gameObject.SetActive(false);
            }
        }

        internal void ApplyToAffectedPlayers() {
            if (!Assert(PlayerList, "playerList invalid", this)) {
                return;
            }

            PlayerList.DiscardInvalid();
            if (PlayerList.Players == null) {
                return;
            }

            foreach (int playerId in PlayerList.Players) {
                ApplyToPlayer(playerId);
            }
        }

        internal void ApplyToPlayer(int playerId) {
            if (!Utilities.IsValid(PlayerAudioController)) {
                Error($"{nameof(PlayerAudioController)} invalid");
                return;
            }

            var playerToAffect = VRCPlayerApi.GetPlayerById(playerId);
            if (!Utilities.IsValid(playerToAffect)) {
                return;
            }

            if (!Assert(
                        PlayerAudioController.OverridePlayerSettings(this, playerToAffect),
                        $"Overriding player {playerToAffect.displayName} failed.",
                        this
                )) {
                return;
            }

            if (playerToAffect.isLocal && Utilities.IsValid(LocalPlayerAdded)) {
                // ActivateReverb();
                LocalPlayerAdded.Raise(this);
            }
        }
        #endregion
    }
}