using System;
using Guribo.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Runtime
{
    /// <summary>
    /// This override contains values that can be used to override the default audio settings in
    /// <see cref="BetterPlayerAudio"/> for a group of players.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BetterPlayerAudioOverride : UdonSharpBehaviour
    {
        #region Settings

        [Header("General settings")]
        
        /// <summary>
        /// Determines whether it 
        /// Overrides with equal or higher values can override other overrides with lower priority.
        /// When removed from a player and it was currently the highest priority override it will
        /// fall back to the next lower override available.
        /// </summary>
        public int priority;

        #endregion

        #region Occlusion settings

        #region Constants

        private const int EnvironmentLayerMask = 1 << 11;
        private const int UILayerMask = 1 << 5;

        #endregion

        [Header("Occlusion settings")]
        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.occlusionMask"/>
        /// </summary>
        [Tooltip(
            "Objects on these layers reduce the voice/avatar sound volume when they are in-between the local player and the player/avatar that produces the sound")]
        public LayerMask occlusionMask = EnvironmentLayerMask | UILayerMask;


        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultOcclusionFactor"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float occlusionFactor = 0.7f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultPlayerOcclusionFactor"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Occlusion when a player is occluded by another player. A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float playerOcclusionFactor = 0.85f;

        #endregion

        #region Directionality settings

        [Header("Directionality settings")]
        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultListenerDirectionality"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when the listener is facing away from a voice/avatar and thus making them more quiet.")]
        public float listenerDirectionality = 0.5f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultPlayerDirectionality"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is facing away from the listener.")]
        public float playerDirectionality = 0.3f;

        #endregion

        #region Reverb settings

        [Header("Reverb settings")]
        public AudioReverbFilter optionalReverb;

        #endregion


        #region Voice settings

        [Header("Voice settings")]
        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultEnableVoiceLowpass"/>
        /// </summary>
        [Tooltip("When enabled the voice of a player sounds muffled when close to the max. audible range.")]
        public bool enableVoiceLowpass = true;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultVoiceDistanceNear"/>
        /// </summary>
        [Tooltip("The volume will stay at max. when the player is closer than this distance.")]
        [Range(0, 1000000)] public float voiceDistanceNear;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultVoiceDistanceFar"/>
        /// </summary>
        [Tooltip("Beyond this distance the player can't be heard.")]
        [Range(0, 1000000)] public float voiceDistanceFar = 25f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultVoiceGain"/>
        /// </summary>
        [Tooltip("Additional volume increase. Changing this may require re-adjusting occlusion parameters!")]
        [Range(0, 24)] public float voiceGain = 15f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultVoiceVolumetricRadius"/>
        /// </summary>
        [Tooltip(
            "Range in which the player voice is not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed.")]
        [Range(0, 1000)] public float voiceVolumetricRadius;

        #endregion

        #region Avatar settings

        [Header("Avatar settings")]
        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultForceAvatarSpatialAudio"/>
        /// </summary>
        [Tooltip("When set overrides all avatar audio sources to be spatialized.")]
        public bool forceAvatarSpatialAudio;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAllowAvatarCustomAudioCurves"/>
        /// </summary>
        [Tooltip("When set custom audio curves on avatar audio sources are used.")]
        public bool allowAvatarCustomAudioCurves = true;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAvatarNearRadius"/>
        /// </summary>
        [Tooltip("Max. distance at which player audio sources start to fall of in volume.")]
        public float targetAvatarNearRadius = 40f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAvatarFarRadius"/>
        /// </summary>
        [Tooltip("Max. allowed distance at which player audio sources can be heard.")]
        public float targetAvatarFarRadius = 40f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAvatarGain"/>
        /// </summary>
        [Range(0, 10)]
        [Tooltip("Volume increase in decibel.")]
        public float targetAvatarGain = 10f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAvatarVolumetricRadius"/>
        /// </summary>
        [Tooltip(
            "Range in which the player audio sources are not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed.")]
        public float targetAvatarVolumetricRadius;

        #endregion

        #region Privacy Settings

        /// <summary>
        /// Players affected by different overrides with the same privacy channel id can hear each other and can't be
        /// heard by non-affected players.
        /// If set to -1 the feature is turned off for this override component and only players affected by this
        /// override can hear each other.
        /// </summary>
        [Header("Privacy settings")]
        [Tooltip(
            "Players affected by different overrides with the same privacy channel id can hear each other and can't be heard by non-affected players. If set to -1 the feature is turned off and all players affected by this override can be heard.")]
        public int privacyChannelId = -1;

        /// <summary>
        /// If set to true affected players also can't hear non-affected players.
        /// Only in effect in combination with a privacy channel not equal to -1.
        /// </summary>
        [Tooltip(
            "If set to true affected players also can't hear non-affected players. Only in effect in combination with a privacy channel not equal to -1.")]
        public bool muteOutsiders = true;
        
        /// <summary>
        /// prevents the local player from listening to any other player in the same channel, can be used to talk to
        /// players in a private room without being able to hear what is going on inside
        /// </summary>
        public bool disallowListeningToChannel;

        #endregion
        #region Optional listeners
        
        [Header("Optional Listeners")]
        
        [Tooltip("Behaviours to notify when the local player has been added (does not mean the override is active e.g. when another override with higher priority is already active)")]
        public UdonSharpBehaviour[] localPlayerAddedListeners;
        public string localPlayerAddedEvent = "LocalPlayerAdded";
        
        [Tooltip("Behaviours to notify when the local player has been removed (does not mean the override was active e.g. when another override with higher priority was already active)")]
        public UdonSharpBehaviour[] localPlayerRemovedListeners;
        public string localPlayerRemovedEvent = "LocalPlayerRemoved";
        
        #endregion

        #region Mandatory references

        [Header("Mandatory references")]
        public BetterPlayerAudio betterPlayerAudio;
        public UdonDebug udonDebug;
        public PlayerList playerList;

        #endregion

        #region State

        private bool _hasStarted;

        #endregion

        #region Udon Lifecycle

        public void OnEnable()
        {
            if (!_hasStarted)
            {
                Start();
            }

            if (!udonDebug.Assert(playerList, "playerList invalid", this))
            {
                return;
            }

            playerList.DiscardInvalid();
            if (playerList.players == null)
            {
                return;
            }

            foreach (var playerId in playerList.players)
            {
                var playerToAffect = VRCPlayerApi.GetPlayerById(playerId);
                if (!Utilities.IsValid(playerToAffect))
                {
                    continue;
                }

                if (playerToAffect.isLocal)
                {
                    ActivateReverb();
                    Notify(localPlayerAddedListeners,localPlayerAddedEvent);
                }
                
                betterPlayerAudio.OverridePlayerSettings(this, playerToAffect);
            }
        }

        public void Start()
        {
            if (_hasStarted)
            {
                return;
            }

            _hasStarted = true;
            DeactivateReverb();
        }

        public void OnDisable()
        {
            if (!udonDebug.Assert(playerList, "playerList invalid", this))
            {
                return;
            }

            playerList.DiscardInvalid();
            if (playerList.players == null)
            {
                return;
            }

            foreach (var playerId in playerList.players)
            {
                var playerToRemove = VRCPlayerApi.GetPlayerById(playerId);
                if (!Utilities.IsValid(playerToRemove))
                {
                    continue;
                }

                if (playerToRemove.isLocal)
                {
                    DeactivateReverb();
                    Notify(localPlayerRemovedListeners,localPlayerRemovedEvent);
                }
                betterPlayerAudio.ClearPlayerOverride(this, playerToRemove);
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// Add a single player to the list of players that should make use of the here defined override values.
        /// When adding multiple players use <see cref="AffectPlayers"/> instead.
        /// Methods can be expensive when call with a lot of players! Avoid using in Update in every frame!
        /// </summary>
        /// <param name="playerToAffect"></param>
        /// <returns>true if the player was added/was already added before</returns>
        public bool AddPlayer(VRCPlayerApi playerToAffect)
        {
            if (!_hasStarted)
            {
                Start();
            }

            if (!udonDebug.Assert(playerList, "playerList invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(Utilities.IsValid(playerToAffect), "Player to affect invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(playerList.AddPlayer(playerToAffect), "player already affected", this))
            {
                return false;
            }
            
            if (!IsActiveAndEnabled())
            {
                Debug.LogWarning($"Override {gameObject.name} is not enabled for {playerToAffect.displayName}");
                return true;
            }

            if (playerToAffect.isLocal)
            {
                ActivateReverb();
                
                Notify(localPlayerAddedListeners, localPlayerAddedEvent);
            }

            if (!udonDebug.Assert(Utilities.IsValid(betterPlayerAudio), "betterPlayerAudio invalid", this))
            {
                return false;
            }

            if (!betterPlayerAudio.OverridePlayerSettings(this, playerToAffect))
            {
                if (playerList.players != null)
                {
                    var listContainedInvalidPlayer = playerList.players.Length > playerList.DiscardInvalid();
                    return listContainedInvalidPlayer;
                }

                return false;
            }

            return true;
        }

        


        /// <summary>
        /// Remove player from the list of players that should make use of the here defined override values.
        /// </summary>
        /// <param name="playerToRemove"></param>
        /// <returns>true if the player was removed/not affected yet</returns>
        public bool RemovePlayer(VRCPlayerApi playerToRemove)
        {
            if (!udonDebug.Assert(playerList, "playerList invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(Utilities.IsValid(playerToRemove), "Player to remove invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(playerList.RemovePlayer(playerToRemove), "player not affected", this))
            {
                return false;
            }

            if (!IsActiveAndEnabled())
            {
                Debug.LogWarning($"Override {gameObject.name} is not enabled for {playerToRemove.displayName}");
                return true;
            }

            if (playerToRemove.isLocal)
            {
                DeactivateReverb();
                
                Notify(localPlayerRemovedListeners, localPlayerRemovedEvent);
            }

            if (!udonDebug.Assert(Utilities.IsValid(betterPlayerAudio), "PlayerAudio invalid", this))
            {
                return false;
            }

            // make the controller apply default settings to the player again
            return betterPlayerAudio.ClearPlayerOverride(this, playerToRemove);
        }

        /// <summary>
        /// Check whether the player given as playerId should make use of the here defined override values
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns>true if the player should be affected</returns>
        public bool IsAffected(VRCPlayerApi player)
        {
            if (!udonDebug.Assert(playerList, "playerList invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(Utilities.IsValid(player), "Player to remove invalid", this))
            {
                return false;
            }

            return playerList.Contains(player);
        }

        public void Refresh()
        {
            var nonLocalPlayersWithOverrides = betterPlayerAudio.GetNonLocalPlayersWithOverrides();
            foreach (var nonLocalPlayersWithOverride in nonLocalPlayersWithOverrides)
            {
                var vrcPlayerApi = VRCPlayerApi.GetPlayerById(nonLocalPlayersWithOverride);
                if (!playerList.Contains(vrcPlayerApi))
                {
                    betterPlayerAudio.ClearPlayerOverride(this, vrcPlayerApi);
                }
            }

            foreach (var playerListPlayer in playerList.players)
            {
                betterPlayerAudio.OverridePlayerSettings(this, VRCPlayerApi.GetPlayerById(playerListPlayer));
            }
        }

        public bool Clear()
        {
            if (!udonDebug.Assert(Utilities.IsValid(playerList), "playerList invalid", this))
            {
                return false;
            }
            
            if (IsActiveAndEnabled())
            {
                DeactivateReverb();

                if (!udonDebug.Assert(Utilities.IsValid(betterPlayerAudio), "playerAudio invalid", this))
                {
                    return false;
                }

                if (playerList.players != null)
                {
                    foreach (var affectedPlayer in playerList.players)
                    {
                        betterPlayerAudio.ClearPlayerOverride(this, VRCPlayerApi.GetPlayerById(affectedPlayer));
                    }
                }
            }

            playerList.Clear();
            return true;
        }

        #endregion

        #region internal
        
        internal bool IsActiveAndEnabled()
        {
            return Utilities.IsValid(this) && enabled && gameObject.activeInHierarchy;
        }

        private void DeactivateReverb()
        {
            if (ReverbValid())
            {
                optionalReverb.gameObject.SetActive(false);
            }
        }

        private void ActivateReverb()
        {
            if (ReverbValid())
            {
                optionalReverb.gameObject.SetActive(true);
            }
        }

        private bool ReverbValid()
        {
            return Utilities.IsValid(optionalReverb)
                   && udonDebug.Assert(Utilities.IsValid(optionalReverb.gameObject.GetComponent(typeof(AudioListener))),
                       "For reverb to work an AudioListener is required on the gameobject with the Reverb Filter",
                       this);
        }
        
        internal void Notify(UdonSharpBehaviour[] listeners, string eventName)
        {
            if (listeners != null && !string.IsNullOrEmpty(eventName))
            {
                foreach (var preSerializationEventListener in listeners)
                {
                    if (Utilities.IsValid(preSerializationEventListener))
                    {
                        preSerializationEventListener.SendCustomEvent(eventName);
                    }
                }
            }
        }

        #endregion
    }
}