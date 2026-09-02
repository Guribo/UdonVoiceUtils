using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Player;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;
#if UNITY_EDITOR
using UnityEditor;
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Core.PlayerAudio
{
    [CustomEditor(typeof(PlayerAudioOverride))]
    public class PlayerAudioOverrideEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Overrides default audio settings for a group of players. " +
                   "Configure voice and avatar audio properties like distance, " +
                   "gain, occlusion, and privacy channels. " +
                   "Higher priority overrides take precedence over lower ones. " +
                   "If the priority is equal the last added override will take precedence.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio
{
    /// <summary>
    /// This override contains values that can be used to override the default audio settings in
    /// <see cref="PlayerAudioController"/> for a group of players.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(
            typeof(PlayerAudioOverride),
            ExecutionOrder,
            TlpExecutionOrder.AudioStart,
            TlpExecutionOrder.AudioEnd)]
    public class PlayerAudioOverride : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioController.ExecutionOrder + 1;
        #endregion

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
        public float OcclusionFactor = 0.6f;

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
        public float PlayerOcclusionFactor = 0.3f;
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
        public bool AllowAvatarCustomAudioCurves;

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
                + "Leave at a constant value of 1.0 to turn off avatar height based voice range changing.\n\n"
                + "VRChat limits: player-controlled 0.2–5m, Udon 0.01–10000m."
        )]
        public AnimationCurve HeightToVoiceCorrelation = AnimationCurve.Constant(0, 25, 1f);

        #region Privacy Settings
        /// <summary>
        /// Players affected by different overrides with the same privacy channel id can hear each other and can't be
        /// heard by non-affected players.
        /// If set to -1 the feature is turned off for this override component and only players affected by this
        /// override can hear each other.
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("privacyChannelId")]
        [Header("Privacy Settings")]
        [Tooltip(
                "Players affected by different overrides with the same privacy channel id can hear each other and " +
                "can't be heard by non-affected players. If set to -1 the feature is turned off and all players affected " +
                "by this override can be heard."
        )]
        internal int PrivacyChannelId = PlayerAudioController.ChannelNoPrivacy;

        [SerializeField]
        [Tooltip(
                "Combined with PrivacyChannelId. " +
                "AudioOverrides containing at least one same channel id are considered in the same channel. " +
                "Allows creating AudioOverrides that can 'listen' to multiple channels at once.")]
        internal int[] AdditionalPrivacyChannelIds;

        /// <summary>
        /// If set to true affected players also can't hear non-affected players.
        /// Only in effect in combination with a privacy channel not equal to -1.
        /// </summary>
        [FormerlySerializedAs("muteOutsiders")]
        [Tooltip(
                "If set to true affected players can't hear non-affected players."
        )]
        public bool MuteOutsiders = true;

        [FormerlySerializedAs("disallowListeningToChannel")]
        [Tooltip(
                "Prevents the local player from listening to any other player in the same channel, can be used to " +
                "talk to players in a private room without being able to hear what is going on inside. " +
                "Only in effect if the local player is currently added to this override."
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

        #region Mandatory References
        [FormerlySerializedAs("PlayerList")]
        [Header("Mandatory references")]
        [FormerlySerializedAs("playerList")]
        public PlayerSet PlayerSet;
        #endregion

        #region Optional References
        [Header("Optional")]
        public PlayerBlackList OptionalPlayerBlackList;
        #endregion

        #region Properties
        public PlayerAudioController PlayerAudioController { internal set; get; }
        #endregion

        #region State
        internal readonly DataDictionary LocallyAddedPlayers = new DataDictionary();

        /// <summary>
        /// keys: ChannelIds [int]
        /// values: unused [empty DataToken]
        /// </summary>
        public readonly DataDictionary PrivacyChannelIds = new DataDictionary();
        #endregion

        #region Udon Lifecycle
        internal void OnEnable() {
            if (!HasStartedOk) {
                _Error($"{nameof(OnEnable)}: Not initialized");
                return;
            }

            _ApplyToAffectedPlayers();
        }


        internal void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (!HasStartedOk) {
                _Warn($"Was not initialized, skipping removal from {nameof(PlayerAudioController)}");
                return;
            }

            for (int i = 0; i < PlayerSet.WorkingValues._LengthSafe(); i++) {
                var playerToRemove = PlayerSet.WorkingValues[i]._IdToVrcPlayer();
                if (!Utilities.IsValid(playerToRemove)) {
                    continue;
                }

                if (!PlayerAudioController._ClearPlayerOverride(this, playerToRemove)) {
                    _Warn($"Failed to clear override for {playerToRemove.displayName}");
                    continue;
                }

                if (playerToRemove.isLocal && Utilities.IsValid(LocalPlayerRemoved)) {
                    // DeactivateReverb();
                    LocalPlayerRemoved._Raise(this);
                }
            }
        }
        #endregion

        #region Public
        /// <summary>
        /// Add a single player to the list of players that should make use of the here defined override values.
        /// When adding multiple players use <see cref="_ApplyToAffectedPlayers"/> instead.
        /// Methods can be expensive when call with a lot of players! Avoid using in Update in every frame!
        /// </summary>
        /// <param name="playerToAffect"></param>
        /// <returns>true if the player was added/was already added before</returns>
        public bool _AddPlayer(VRCPlayerApi playerToAffect) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_AddPlayer));
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(_AddPlayer)}: Not initialized");
                return false;
            }

            if (!Utilities.IsValid(playerToAffect)) {
                _Error($"{nameof(_AddPlayer)}: {nameof(playerToAffect)} invalid");
                return false;
            }

            if (!LocallyAddedPlayers.ContainsKey(playerToAffect.playerId)) {
                LocallyAddedPlayers.Add(playerToAffect.playerId, new DataToken());
            }

            var playerListResult = PlayerSet._AddPlayer(playerToAffect);
            switch (playerListResult) {
                case PlayerListResult.Success:
                    break;
                case PlayerListResult.AlreadyPresent:
                    _Warn($"{playerToAffect._ToStringSafe()} is already affected");
                    return true;
                default:
                    _Error($"Unexpected result from {nameof(PlayerSet._AddPlayer)}: {playerListResult}");
                    return false;
            }

            if (!isActiveAndEnabled) {
                _Warn($"Override {gameObject.name} is not enabled for {playerToAffect.displayName}");
                return true;
            }

            if (playerToAffect.isLocal && Utilities.IsValid(LocalPlayerAdded)) {
                // ActivateReverb();
                LocalPlayerAdded._Raise(this);
            }

            if (!PlayerAudioController._OverridePlayerSettings(this, playerToAffect)) {
                _Error($"Overriding settings of {playerToAffect._ToStringSafe()} failed");
                return false;
            }

            return true;
        }


        /// <summary>
        /// Remove player from the list of players that should make use of the here defined override values.
        /// </summary>
        /// <param name="playerToRemove"></param>
        /// <returns>true if the player was removed/not affected yet</returns>
        public bool _RemovePlayer(VRCPlayerApi playerToRemove) {
#if TLP_DEBUG
            _DebugLog(nameof(_RemovePlayer));
#endif
            if (!HasStartedOk) {
                _Error($"{nameof(_RemovePlayer)}: Not initialized");
                return false;
            }

            if (!Utilities.IsValid(playerToRemove)) {
                _Error("Player to remove is invalid");
                return false;
            }

            bool unused = LocallyAddedPlayers.Remove(playerToRemove.playerId);
            var playerListResult = PlayerSet._RemovePlayer(playerToRemove);
            switch (playerListResult) {
                case PlayerListResult.Success:
                    break;
                case PlayerListResult.NotPresent:
                    _Warn($"{playerToRemove._ToStringSafe()} was not affected");
                    return true;
                default:
                    _Error($"Unexpected result from {nameof(PlayerSet._RemovePlayer)}: {playerListResult}");
                    return false;
            }

            if (!isActiveAndEnabled) {
                _Warn($"Override {gameObject.name} is not enabled for {playerToRemove.displayName}");
                return true;
            }

            // make the controller apply default settings to the player again
            if (!PlayerAudioController._ClearPlayerOverride(this, playerToRemove)) {
                _Warn($"Failed to clear override for {playerToRemove.displayName}");
            }

            if (playerToRemove.isLocal && Utilities.IsValid(LocalPlayerRemoved)) {
                // DeactivateReverb();
                LocalPlayerRemoved._Raise(this);
            }

            return true;
        }

        /// <summary>
        /// Check whether the player given as playerId should make use of the here defined override values
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns>true if the player should be affected</returns>
        public bool _IsAffected(VRCPlayerApi player) {
#if TLP_DEBUG
            _DebugLog(nameof(_IsAffected));
#endif
            if (!HasStartedOk) {
                _Error($"{nameof(_IsAffected)}: Not initialized");
                return false;
            }

            return isActiveAndEnabled && PlayerSet._Contains(player);
        }

        public void _Refresh() {
#if TLP_DEBUG
            _DebugLog(nameof(_Refresh));
#endif
            if (!HasStartedOk) {
                _Error($"{nameof(_Refresh)}: Not initialized");
                return;
            }

            var nonLocalPlayersWithOverrides = PlayerAudioController._GetNonLocalPlayersWithOverrides();
            for (int i = 0; i < nonLocalPlayersWithOverrides.Count; i++) {
                var nonLocalPlayersWithOverride = nonLocalPlayersWithOverrides[i];
                var vrcPlayerApi = VRCPlayerApi.GetPlayerById(nonLocalPlayersWithOverride.Int);
                if (!PlayerSet._Contains(vrcPlayerApi)) {
                    PlayerAudioController._ClearPlayerOverride(this, vrcPlayerApi);
                }
            }

            bool containsLocal = false;
            var localPlayer = Networking.LocalPlayer;

            if (!Utilities.IsValid(localPlayer)) {
                _Error($"{nameof(_Refresh)}.{nameof(localPlayer)} invalid");
                return;
            }

            int localPlayerId = localPlayer.playerId;

            foreach (int playerListPlayer in PlayerSet.WorkingValues) {
                if (playerListPlayer == localPlayerId) {
                    containsLocal = true;
                }

                PlayerAudioController._OverridePlayerSettings(this, VRCPlayerApi.GetPlayerById(playerListPlayer));
            }

            // also remove from local player
            if (!containsLocal) {
                bool unused = PlayerAudioController._ClearPlayerOverride(this, localPlayer);
            }
        }

        public bool _Clear() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Clear));
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(_Clear)}: Not initialized");
                return false;
            }

            if (isActiveAndEnabled) {
                _ClearPlayerOverrides();
            }

            PlayerSet._Clear();
            LocallyAddedPlayers.Clear();
            return true;
        }


        public bool _IsPlayerBlackListed(VRCPlayerApi player) {
            return Utilities.IsValid(OptionalPlayerBlackList) &&
                   OptionalPlayerBlackList._IsBlackListed(player._DisplayNameSafe());
        }

        /// <summary>
        /// Enforces, that the list of affected players is not synchronized with other players
        /// </summary>
        /// <returns>false if not initialized</returns>
        public bool _ForceNoSynchronization() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_ForceNoSynchronization));
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(_ForceNoSynchronization)}: Not initialized");
                return false;
            }

            PlayerSet.SyncPaused = true;
            return true;
        }

        /// <summary>
        /// Allow that the list of affected players *may* synchronized with other players if the <see cref="PlayerSet"/>
        /// script has synchronization enabled
        /// </summary>
        /// <returns>false if not initialized</returns>
        public bool _AllowSynchronization() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_AllowSynchronization));
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(_AllowSynchronization)}: Not initialized");
                return false;
            }

            PlayerSet.SyncPaused = false;
            return true;
        }

        public bool _HasOverlappingChannels(PlayerAudioOverride audioOverride) {
            if (!HasStartedOk) {
                return false;
            }

            if (!Utilities.IsValid(audioOverride)) {
                // treat invalid overrides as NoPrivacyChannel
                return PrivacyChannelIds.ContainsKey(PlayerAudioController.ChannelNoPrivacy);
            }

            if (!audioOverride.HasStartedOk) {
                // treat failed scripts always as private channel
                return false;
            }

            var other = audioOverride.PrivacyChannelIds;
            var shorter = PrivacyChannelIds.Count > other.Count ? other : PrivacyChannelIds;
            var longer = ReferenceEquals(shorter, PrivacyChannelIds) ? other : PrivacyChannelIds;

            // optimized lookup of any potential matches by iterating over the shorter of the two dictionaries
            // Complexity best case: O(1) * O(key lookup) - first entry is found instantly
            // Worst case: O(n) * O(key lookup) - entire first (shorter) dictionary needed to be iterated
            var privacyChannelIds = shorter.GetKeys();
            for (int i = 0; i < privacyChannelIds.Count; i++) {
                if (longer.ContainsKey(privacyChannelIds[i].Int)) {
                    return true;
                }
            }

            return false;
        }

        public bool _IsPrivateChannel() {
            if (!HasStartedOk) {
                return false;
            }

            switch (PrivacyChannelIds.Count) {
                case 0:
                    return false;
                case 1:
                    return !PrivacyChannelIds.ContainsKey(PlayerAudioController.ChannelNoPrivacy);
                default:
                    // with more then one entry there MUST to be a private channel due to unique keys in dictionaries
                    return true;
            }
        }

        public bool _IsChannelAudible(int channelId) {
            if (!HasStartedOk) {
                return false;
            }

            return PrivacyChannelIds.ContainsKey(channelId);
        }
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            _MergePrivacyChannelsIntoDictionary();

            PlayerAudioController = VoiceUtils._FindPlayerAudioController();
            if (!Utilities.IsValid(PlayerAudioController)) {
                _Error($"{nameof(PlayerAudioController)} not found");
                return false;
            }

            if (!Utilities.IsValid(PlayerSet)) {
                _Error($"{nameof(PlayerSet)} not set");
                return false;
            }

            if (Utilities.IsValid(OptionalReverb)) {
                OptionalReverb.gameObject.SetActive(false);
            }

            if (PlayerSet.ListenerMethod != nameof(_OnPlayerListUpdated)) {
                _Warn(
                        $"Changing {PlayerSet._GetScriptPathInScene()}.{nameof(PlayerSet.ListenerMethod)} from '{PlayerSet.ListenerMethod}' to '{nameof(_OnPlayerListUpdated)}'");
                PlayerSet.ListenerMethod = nameof(_OnPlayerListUpdated);
            }

            if (!PlayerSet._AddListenerVerified(this, nameof(_OnPlayerListUpdated))) {
                _Error($"Failed to listen to {PlayerSet._GetScriptPathInScene()}.{nameof(_OnPlayerListUpdated)}");
                return false;
            }

            _ApplyToAffectedPlayers();
            return true;
        }

        public override void _OnEvent(string eventName) {
            switch (eventName) {
                case nameof(_OnPlayerListUpdated):

                    #region TLP_DEBUG
#if TLP_DEBUG
                    _DebugLog($"{nameof(_OnEvent)} {eventName}");
#endif
                    #endregion

                    _OnPlayerListUpdated();
                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Callback
        internal void _OnPlayerListUpdated() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnPlayerListUpdated));
#endif
            #endregion

            _Refresh();
        }
        #endregion

        #region internal
        internal void _ClearPlayerOverrides() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_ClearPlayerOverrides));
#endif
            #endregion

            int[] playerIds = PlayerSet.WorkingValues;
            int players = playerIds._LengthSafe();
            for (int i = 0; i < players; i++) {
                var player = playerIds[i]._IdToVrcPlayer();
                if (!Utilities.IsValid(player)) {
                    continue;
                }

                PlayerAudioController._ClearPlayerOverride(this, player);
            }
        }

        internal void _ApplyToAffectedPlayers() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_ApplyToAffectedPlayers));
#endif
            #endregion

            int[] playerIds = PlayerSet.WorkingValues;
            int players = playerIds._LengthSafe();
            for (int i = 0; i < players; i++) {
                var player = playerIds[i]._IdToVrcPlayer();
                if (!Utilities.IsValid(player)) {
                    continue;
                }

                if (!PlayerAudioController._OverridePlayerSettings(this, player)) {
                    _Error($"Overriding settings of {player._ToStringSafe()} failed");
                }

                if (player.isLocal && Utilities.IsValid(LocalPlayerAdded)) {
                    // ActivateReverb();
                    LocalPlayerAdded._Raise(this);
                }
            }
        }

        private void _MergePrivacyChannelsIntoDictionary() {
            _RegisterPrivacyChannel(PrivacyChannelId);

            if (AdditionalPrivacyChannelIds._LengthSafe() == 0) {
                return;
            }

            foreach (int additionalPrivacyChannelId in AdditionalPrivacyChannelIds) {
                _RegisterPrivacyChannel(additionalPrivacyChannelId);
            }
        }

        private void _RegisterPrivacyChannel(int privacyChannelId) {
            if (!PrivacyChannelIds.ContainsKey(privacyChannelId)) {
                PrivacyChannelIds.Add(privacyChannelId, new DataToken());
                return;
            }

            _Warn($"{nameof(_RegisterPrivacyChannel)}: Privacy channel {privacyChannelId} already registered.");
        }
        #endregion
    }
}