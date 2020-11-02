using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterPlayerAudio : UdonSharpBehaviour
    {
        [SerializeField] public LayerMask occlusionMask = 1 << 11;
        [NonSerialized] public float OcclusionFactor = 0.66f;
        [NonSerialized] public float ListenerDirectionality = 0.66f;
        [NonSerialized] public float PlayerDirectionality = 0.66f;

        [NonSerialized] public bool EnableVoiceLowpass;

        [NonSerialized] public float TargetVoiceDistanceNear;
        [NonSerialized] public float TargetVoiceDistanceFar;
        [NonSerialized] public float TargetVoiceGain;
        [NonSerialized] public float TargetVoiceVolumetricRadius;

        [NonSerialized] public bool ForceAvatarSpatialAudio = true;
        [NonSerialized] public bool AllowAvatarCustomAudioCurves = true;

        [NonSerialized] public float TargetAvatarNearRadius;
        [NonSerialized] public float TargetAvatarFarRadius;
        [NonSerialized] public float TargetAvatarGain;
        [NonSerialized] public float TargetAvatarVolumetricRadius;


        private int _playerIndex = 0;
        private int _playerCount;
        private VRCPlayerApi[] _players = new VRCPlayerApi[1];
        private readonly RaycastHit[] _rayHits = new RaycastHit[1];

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

            var occlusionFactor = CalculateOcclusion(listenerHead, direction, distance);
            var directionality = CalculateDirectionality(localPlayer, vrcPlayerApi, direction);

            var rawDistanceReductionFactor = directionality * occlusionFactor;
            var voiceDistanceFactor =
                CalculateRangeReduction(distance, rawDistanceReductionFactor, TargetVoiceDistanceFar);
            UpdateVoiceAudio(vrcPlayerApi, voiceDistanceFactor);

            var avatarDistanceFactor =
                CalculateRangeReduction(distance, rawDistanceReductionFactor, TargetAvatarFarRadius);
            UpdateAvatarAudio(vrcPlayerApi, avatarDistanceFactor);
        }

        private float CalculateRangeReduction(float distance, float occlusionFactor, float maxAudibleRange)
        {
            if (maxAudibleRange <= 0f || Mathf.Abs(occlusionFactor - 1f) < 0.01f)
            {
                return 1f;
            }

            var remainingDistanceToFarRadius = maxAudibleRange - distance;
            var playerCouldBeAudible = remainingDistanceToFarRadius > 0f;

            var occlusion = 1f;
            if (playerCouldBeAudible)
            {
                var postOcclusionFarDistance = remainingDistanceToFarRadius * occlusionFactor + distance;
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

        private float CalculateOcclusion(Vector3 listenerHead, Vector3 direction, float distance)
        {
            var hits = Physics.RaycastNonAlloc(listenerHead,
                direction,
                _rayHits,
                distance,
                occlusionMask);

            if (hits > 0)
            {
                return OcclusionFactor;
            }

            return 1f;
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