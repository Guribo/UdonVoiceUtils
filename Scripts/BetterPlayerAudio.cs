using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterPlayerAudio : UdonSharpBehaviour
    {
        [Header("General Settings")] public LayerMask occlusionMask = 1 << 11; // Environment layer

        #region default values for resetting

        [Tooltip(
            "A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float defaultOcclusionFactor = 0.5f;

        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 50% when the listener is facing away from a voice/avatar and thus making them more quiet.")]
        public float defaultListenerDirectionality = 0.75f;

        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 50% when someone is speaking/playing avatar sounds but is facing away from the listener.")]
        public float defaultPlayerDirectionality = 0.75f;

        [Header("Voice Settings")] public bool defaultEnableVoiceLowpass = true;

        public float defaultVoiceDistanceNear = 0f;
        public float defaultVoiceDistanceFar = 100f;
        public float defaultVoiceGain = 0f;
        public float defaultVoiceVolumetricRadius = 0f;

        [Header("Avatar Settings")] public bool defaultForceAvatarSpatialAudio = false;
        public bool defaultAllowAvatarCustomAudioCurves = false;

        public float defaultAvatarNearRadius = 0f;
        public float defaultAvatarFarRadius = 100f;
        public float defaultAvatarGain = 0f;
        public float defaultAvatarVolumetricRadius = 0f;

        #endregion

        #region currently used values

        [NonSerialized] public float OcclusionFactor;
        [NonSerialized] public float ListenerDirectionality;
        [NonSerialized] public float PlayerDirectionality;

        [NonSerialized] public bool EnableVoiceLowpass;
        [NonSerialized] public float TargetVoiceDistanceNear;
        [NonSerialized] public float TargetVoiceDistanceFar;
        [NonSerialized] public float TargetVoiceGain;
        [NonSerialized] public float TargetVoiceVolumetricRadius;

        [NonSerialized] public bool ForceAvatarSpatialAudio;
        [NonSerialized] public bool AllowAvatarCustomAudioCurves;
        [NonSerialized] public float TargetAvatarNearRadius;
        [NonSerialized] public float TargetAvatarFarRadius;
        [NonSerialized] public float TargetAvatarGain;
        [NonSerialized] public float TargetAvatarVolumetricRadius;

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

            var listenerHead = localPlayer.GetBonePosition(HumanBodyBones.Head);
            var otherPlayerHead = vrcPlayerApi.GetBonePosition(HumanBodyBones.Head);

            var listenerToPlayer = (otherPlayerHead - listenerHead);
            var direction = listenerToPlayer.normalized;
            var distance = listenerToPlayer.magnitude;

            var occlusionFactor = CalculateOcclusion(listenerHead, direction, distance, OcclusionFactor);
            var directionality = CalculateDirectionality(localPlayer, vrcPlayerApi, direction);

            var rawDistanceReductionFactor = directionality * occlusionFactor;
            var voiceDistanceFactor =
                CalculateRangeReduction(distance, rawDistanceReductionFactor, TargetVoiceDistanceFar);
            UpdateVoiceAudio(vrcPlayerApi, voiceDistanceFactor);

            var avatarDistanceFactor =
                CalculateRangeReduction(distance, rawDistanceReductionFactor, TargetAvatarFarRadius);
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

        private float CalculateDirectionality(VRCPlayerApi localPlayer, VRCPlayerApi otherPlayer,
            Vector3 directionToPlayer)
        {
            var listenerForward = localPlayer.GetBoneRotation(HumanBodyBones.Head) * Vector3.forward;
            var playerBackward = otherPlayer.GetBoneRotation(HumanBodyBones.Head) * Vector3.back;


            var dotListener = 0.5f * (Vector3.Dot(listenerForward, directionToPlayer) + 1f);
            var dotSource = 0.5f * (Vector3.Dot(playerBackward, directionToPlayer) + 1f);

            var result = Mathf.Clamp01(dotListener + (1 - ListenerDirectionality)) +
                         Mathf.Clamp01(dotSource + (1 - PlayerDirectionality));

            return 0.5f * result;
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
    }
}