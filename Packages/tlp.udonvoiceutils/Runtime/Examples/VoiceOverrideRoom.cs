using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Player;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VoiceOverrideRoom), ExecutionOrder)]
    public class VoiceOverrideRoom : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = MicActivation.ExecutionOrder + 10;
        #endregion

        #region State
        internal bool Initialized { private set; get; }
        internal bool IsInZone;
        #endregion

        #region VRC Callbacks
        public override void OnPlayerRespawn(VRCPlayerApi player) {
#if TLP_DEBUG
            _DebugLog($"{nameof(OnPlayerRespawn)}: {player._ToStringSafe()}");
#endif
            if (!Initialized) {
                _Error("Not initialized");
                return;
            }

            if (!ExitZoneOnRespawn) {
                return;
            }

            if (!player._IsLocalSafe()) {
#if TLP_DEBUG
                _Warn($"Ignore irrelevant {player._ToStringSafe()}");
#endif
                return;
            }

            if (!IsInZone) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"{player._ToStringSafe()} was not in {this._GetScriptPathInScene()}");
#endif
                #endregion

                return;
            }

            if (!_RemoveLocalPlayerFromZone(player)) {
                _Error($"Failed to remove {player._ToStringSafe()} from {this._GetScriptPathInScene()}");
            }
        }
        #endregion

        #region Events
        public void _OnPlayerListUpdated() {
#if TLP_DEBUG
            _DebugLog(nameof(_OnPlayerListUpdated));
#endif
            if (!Initialized) {
                _Error("Not initialized");
                return;
            }

            PlayerAudioOverride._Refresh();

            var localPlayer = Networking.LocalPlayer;
            if (PlayerAudioOverride._IsAffected(localPlayer) == IsInZone) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"{localPlayer._ToStringSafe()} is in sync with other players ({nameof(IsInZone)} = {IsInZone})");
#endif
                #endregion

                return;
            }

            if (IsInZone) {
                _Warn(
                        $"{localPlayer._ToStringSafe()} not in {PlayerAudioOverride._GetScriptPathInScene()} despite being in {this._GetScriptPathInScene()} (network state outdated)");
                if (!PlayerAudioOverride._AddPlayer(localPlayer)) {
                    _Error($"Failed to add {localPlayer._ToStringSafe()} to {this._GetScriptPathInScene()}");
                    return;
                }
            } else {
                _Warn(
                        $"{localPlayer._ToStringSafe()} in {PlayerAudioOverride._GetScriptPathInScene()} despite no longer being in {this._GetScriptPathInScene()} (network state outdated)");
                if (!PlayerAudioOverride._RemovePlayer(localPlayer)) {
                    _Error($"Failed to remove {localPlayer._ToStringSafe()} from {this._GetScriptPathInScene()}");
                    return;
                }
            }

            _UpdateNetwork(localPlayer);
        }
        #endregion


        #region Mandatory references
        #region Teleport settings
        [FormerlySerializedAs("optionalRespawnLocation")]
        [Header("Teleport settings")]
        public Transform OptionalRespawnLocation;

        [FormerlySerializedAs("exitZoneOnRespawn")]
        public bool ExitZoneOnRespawn = true;
        #endregion

        [Header("Mandatory references")]
        [FormerlySerializedAs("playerOverride")]
        public PlayerAudioOverride PlayerAudioOverride;

        [FormerlySerializedAs("PlayerList")] public PlayerSet PlayerSet;
        #endregion

        #region LifeCycle
        public void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (Initialized) {
                _OnPlayerListUpdated();
            }
        }

        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (Initialized) {
                PlayerSet._RemoveListener(this, true);
            }
        }
        #endregion

        #region Public
        public bool _EnterRoom(VRCPlayerApi localPlayer, Transform optionTeleportLocation) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_EnterRoom));
#endif
            #endregion

            if (!Initialized) {
                _Error("Not initialized");
                return false;
            }

            if (!localPlayer._IsLocalSafe()) {
                _Error($"Player {localPlayer._ToStringSafe()} is not local");
                return false;
            }

            if (!_AddLocalPlayerToZone(localPlayer)) {
                _Error("Failed adding player to zone");
                return false;
            }

            _TeleportPlayer(optionTeleportLocation);
            return true;
        }

        public bool _ExitRoom(VRCPlayerApi localPlayer, Transform optionTeleportLocation) {
#if TLP_DEBUG
            _DebugLog(nameof(_ExitRoom));
#endif
            if (!Initialized) {
                _Error("Not initialized");
                return false;
            }

            if (!localPlayer._IsLocalSafe()) {
                _Error($"{localPlayer._ToStringSafe()} is not local");
                return false;
            }

            if (!_RemoveLocalPlayerFromZone(localPlayer)) {
                _Error("Local player was not in the zone");
                return false;
            }

            _TeleportPlayer(optionTeleportLocation);
            return true;
        }


        public bool _Contains(VRCPlayerApi playerApi) {
#if TLP_DEBUG
            _DebugLog(nameof(_Contains));
#endif
            if (Initialized) {
                return PlayerAudioOverride._IsAffected(playerApi);
            }

            _Error("Not initialized");
            return false;
        }
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(PlayerSet)) {
                _Error($"{nameof(PlayerSet)} not set");
                return false;
            }

            if (!Utilities.IsValid(PlayerAudioOverride)) {
                _Error($"{nameof(PlayerAudioOverride)} not set");
                return false;
            }

            if (!PlayerSet._AddListenerVerified(this, nameof(_OnPlayerListUpdated))) {
                _Error($"Failed listening to {PlayerSet._GetScriptPathInScene()}.{nameof(_OnPlayerListUpdated)}");
                return false;
            }

            Initialized = true;

            _OnPlayerListUpdated();
            return true;
        }

        public override void _OnEvent(string eventName) {
            switch (eventName) {
                case nameof(_OnPlayerListUpdated):
#if TLP_DEBUG
                    _DebugLog($"{nameof(_OnEvent)} {eventName}");
#endif
                    _OnPlayerListUpdated();
                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Internal
        internal void _TeleportPlayer(Transform target) {
#if TLP_DEBUG
            _DebugLog(nameof(_TeleportPlayer));
#endif
            if (Utilities.IsValid(target)) {
                Networking.LocalPlayer.TeleportTo(
                        target.position,
                        target.rotation,
                        VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                        false
                );
            }
        }

        internal void _UpdateNetwork(VRCPlayerApi localPlayer) {
#if TLP_DEBUG
            _DebugLog(nameof(_UpdateNetwork));
#endif
            var syncedIntegerArray = PlayerSet.gameObject;
            if (!Networking.IsOwner(syncedIntegerArray)) {
                Networking.SetOwner(localPlayer, syncedIntegerArray);
            }

            if (!PlayerSet._RaiseRemoteOnly(this)) {
                _Error($"Failed to send current {nameof(PlayerSet)}");
            }
        }

        internal bool _AddLocalPlayerToZone(VRCPlayerApi localPlayer) {
#if TLP_DEBUG
            _DebugLog(nameof(_AddLocalPlayerToZone));
#endif
            if (!PlayerAudioOverride._AddPlayer(localPlayer)) {
                _Error($"Adding local player to {nameof(PlayerAudioOverride)} failed");
                return false;
            }

            IsInZone = true;
            _UpdateNetwork(localPlayer);

            return true;
        }

        internal bool _RemoveLocalPlayerFromZone(VRCPlayerApi localPlayer) {
#if TLP_DEBUG
            _DebugLog(nameof(_RemoveLocalPlayerFromZone));
#endif
            IsInZone = false;
            if (!PlayerAudioOverride._IsAffected(localPlayer)) {
#if TLP_DEBUG
                _Warn(
                        $"{nameof(_RemoveLocalPlayerFromZone)}: {localPlayer._ToStringSafe()} was not affected by {PlayerAudioOverride._GetScriptPathInScene()}");
#endif
                return true;
            }

            if (!PlayerAudioOverride._RemovePlayer(localPlayer)) {
                _Error(
                        $"Failed to remove {localPlayer._ToStringSafe()} from {PlayerAudioOverride._GetScriptPathInScene()}");
                return false;
            }

            _UpdateNetwork(localPlayer);
            return true;
        }
        #endregion
    }
}