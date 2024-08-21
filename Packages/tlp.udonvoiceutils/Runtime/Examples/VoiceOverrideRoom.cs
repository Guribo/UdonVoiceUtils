using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Player;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class VoiceOverrideRoom : TlpBaseBehaviour
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioOverride.ExecutionOrder + 1;
        #endregion

        #region State
        internal bool Initialized { private set; get; }
        internal bool IsInZone;
        #endregion

        #region VRC Callbacks
        public override void OnPlayerRespawn(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerRespawn)}: {player.ToStringSafe()}");
#endif
            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            if (!ExitZoneOnRespawn) {
                return;
            }

            if (!player.IsLocalSafe()) {
#if TLP_DEBUG
                Warn($"Ignore irrelevant {player.ToStringSafe()}");
#endif
                return;
            }

            if (!IsInZone) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"{player.ToStringSafe()} was not in {this.GetScriptPathInScene()}");
#endif
                #endregion

                return;
            }

            if (!RemoveLocalPlayerFromZone(player)) {
                Error($"Failed to remove {player.ToStringSafe()} from {this.GetScriptPathInScene()}");
            }
        }
        #endregion

        #region Events
        public void OnPlayerListUpdated() {
#if TLP_DEBUG
            DebugLog(nameof(OnPlayerListUpdated));
#endif
            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            PlayerAudioOverride.Refresh();

            var localPlayer = Networking.LocalPlayer;
            if (PlayerAudioOverride.IsAffected(localPlayer) == IsInZone) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"{localPlayer.ToStringSafe()} is in sync with other players ({nameof(IsInZone)} = {IsInZone})");
#endif
                #endregion

                return;
            }

            if (IsInZone) {
                Warn(
                        $"{localPlayer.ToStringSafe()} not in {PlayerAudioOverride.GetScriptPathInScene()} despite being in {this.GetScriptPathInScene()} (network state outdated)");
                if (!PlayerAudioOverride.AddPlayer(localPlayer)) {
                    Error($"Failed to add {localPlayer.ToStringSafe()} to {this.GetScriptPathInScene()}");
                    return;
                }
            } else {
                Warn(
                        $"{localPlayer.ToStringSafe()} in {PlayerAudioOverride.GetScriptPathInScene()} despite no longer being in {this.GetScriptPathInScene()} (network state outdated)");
                if (!PlayerAudioOverride.RemovePlayer(localPlayer)) {
                    Error($"Failed to remove {localPlayer.ToStringSafe()} from {this.GetScriptPathInScene()}");
                    return;
                }
            }

            UpdateNetwork(localPlayer);
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
            DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (Initialized) {
                OnPlayerListUpdated();
            }
        }

        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (Initialized) {
                PlayerSet.RemoveListener(this, true);
            }
        }
        #endregion

        #region Public
        public bool EnterRoom(VRCPlayerApi localPlayer, Transform optionTeleportLocation) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(EnterRoom));
#endif
            #endregion

            if (!Initialized) {
                Error("Not initialized");
                return false;
            }

            if (!localPlayer.IsLocalSafe()) {
                Error($"Player {localPlayer.ToStringSafe()} is not local");
                return false;
            }

            if (!AddLocalPlayerToZone(localPlayer)) {
                Error("Failed adding player to zone");
                return false;
            }

            TeleportPlayer(optionTeleportLocation);
            return true;
        }

        public bool ExitRoom(VRCPlayerApi localPlayer, Transform optionTeleportLocation) {
#if TLP_DEBUG
            DebugLog(nameof(ExitRoom));
#endif
            if (!Initialized) {
                Error("Not initialized");
                return false;
            }

            if (!localPlayer.IsLocalSafe()) {
                Error($"{localPlayer.ToStringSafe()} is not local");
                return false;
            }

            if (!RemoveLocalPlayerFromZone(localPlayer)) {
                Error("Local player was not in the zone");
                return false;
            }

            TeleportPlayer(optionTeleportLocation);
            return true;
        }


        public bool Contains(VRCPlayerApi playerApi) {
#if TLP_DEBUG
            DebugLog(nameof(Contains));
#endif
            if (Initialized) {
                return PlayerAudioOverride.IsAffected(playerApi);
            }

            Error("Not initialized");
            return false;
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(PlayerSet)) {
                Error($"{nameof(PlayerSet)} not set");
                return false;
            }

            if (!Utilities.IsValid(PlayerAudioOverride)) {
                Error($"{nameof(PlayerAudioOverride)} not set");
                return false;
            }

            if (!PlayerSet.AddListenerVerified(this, nameof(OnPlayerListUpdated))) {
                Error($"Failed listening to {PlayerSet.GetScriptPathInScene()}.{nameof(OnPlayerListUpdated)}");
                return false;
            }

            Initialized = true;

            OnPlayerListUpdated();
            return true;
        }

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(OnPlayerListUpdated):
#if TLP_DEBUG
                    DebugLog($"{nameof(OnEvent)} {eventName}");
#endif
                    OnPlayerListUpdated();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Internal
        internal void TeleportPlayer(Transform target) {
#if TLP_DEBUG
            DebugLog(nameof(TeleportPlayer));
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

        internal void UpdateNetwork(VRCPlayerApi localPlayer) {
#if TLP_DEBUG
            DebugLog(nameof(UpdateNetwork));
#endif
            var syncedIntegerArray = PlayerSet.gameObject;
            if (!Networking.IsOwner(syncedIntegerArray)) {
                Networking.SetOwner(localPlayer, syncedIntegerArray);
            }

            if (!PlayerSet.RaiseRemoteOnly(this)) {
                Error($"Failed to send current {nameof(PlayerSet)}");
            }
        }

        internal bool AddLocalPlayerToZone(VRCPlayerApi localPlayer) {
#if TLP_DEBUG
            DebugLog(nameof(AddLocalPlayerToZone));
#endif
            if (!PlayerAudioOverride.AddPlayer(localPlayer)) {
                Error($"Adding local player to {nameof(PlayerAudioOverride)} failed");
                return false;
            }

            IsInZone = true;
            UpdateNetwork(localPlayer);

            return true;
        }

        internal bool RemoveLocalPlayerFromZone(VRCPlayerApi localPlayer) {
#if TLP_DEBUG
            DebugLog(nameof(RemoveLocalPlayerFromZone));
#endif
            IsInZone = false;
            if (!PlayerAudioOverride.IsAffected(localPlayer)) {
#if TLP_DEBUG
                Warn(
                        $"{nameof(RemoveLocalPlayerFromZone)}: {localPlayer.ToStringSafe()} was not affected by {PlayerAudioOverride.GetScriptPathInScene()}");
#endif
                return true;
            }

            if (!PlayerAudioOverride.RemovePlayer(localPlayer)) {
                Error(
                        $"Failed to remove {localPlayer.ToStringSafe()} from {PlayerAudioOverride.GetScriptPathInScene()}");
                return false;
            }

            UpdateNetwork(localPlayer);
            return true;
        }
        #endregion
    }
}