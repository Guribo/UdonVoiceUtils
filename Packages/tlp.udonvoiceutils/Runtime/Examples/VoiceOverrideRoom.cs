using System;
using TLP.UdonUtils.Runtime;
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
    public class VoiceOverrideRoom : TlpBaseBehaviour
    {
        internal bool IsInZone;

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

        public PlayerList PlayerList;
        #endregion

        #region LifeCycle
        protected virtual void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (!Utilities.IsValid(PlayerList)) {
                ErrorAndDisableGameObject($"{nameof(PlayerList)} invalid");
                return;
            }

            if (!Utilities.IsValid(PlayerAudioOverride)) {
                ErrorAndDisableGameObject($"{nameof(PlayerAudioOverride)} invalid");
                return;
            }

            if (!PlayerList.AddListenerVerified(this, nameof(RefreshPlayersInZone))) {
                ErrorAndDisableGameObject($"Failed listening to {nameof(PlayerList)}");
                return;
            }

            RefreshPlayersInZone();
        }

        protected virtual void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            #endregion

            PlayerList.RemoveListener(this, true);
        }
        #endregion

        #region Internal
        internal bool AddLocalPlayerToZone(VRCPlayerApi localPlayer) {
#if TLP_DEBUG
            DebugLog(nameof(AddLocalPlayerToZone));
#endif
            if (!Assert(Utilities.IsValid(localPlayer), "Player invalid", this)) {
                return false;
            }

            if (!Assert(localPlayer.isLocal, "Player is not local", this)) {
                return false;
            }

            if (!Assert(Utilities.IsValid(PlayerAudioOverride), "PlayerAudioOverride invalid", this)) {
                return false;
            }

            if (!Assert(
                        PlayerAudioOverride.AddPlayer(localPlayer),
                        "Adding player to PlayerAudioOverride failed",
                        this
                )) {
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
            if (!Assert(Utilities.IsValid(localPlayer), "Player invalid", this)
                || !Assert(localPlayer.isLocal, "Player is not local", this)
                || !Assert(Utilities.IsValid(PlayerAudioOverride), "Player list invalid", this)
                || !PlayerAudioOverride.RemovePlayer(localPlayer)) {
                return false;
            }

            IsInZone = false;

            UpdateNetwork(localPlayer);

            return true;
        }

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
            var syncedIntegerArray = PlayerList.gameObject;
            if (!Networking.IsOwner(syncedIntegerArray)) {
                Networking.SetOwner(localPlayer, syncedIntegerArray);
            }

            if (!PlayerList.RaiseRemoteOnly(this)) {
                Error($"Failed to send current {nameof(PlayerList)}");
            }
        }
        #endregion

        #region Public
        public override void OnPlayerRespawn(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(OnPlayerRespawn));
#endif
            if (!ExitZoneOnRespawn) {
                return;
            }

            if (!Contains(player)) {
                return;
            }

            if (!Assert(Utilities.IsValid(player), "player invalid", this)
                || !player.isLocal) {
                return;
            }

            ExitRoom(player, OptionalRespawnLocation);
        }

        public bool EnterRoom(VRCPlayerApi localPlayer, Transform optionTeleportLocation) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(EnterRoom));
#endif
            #endregion

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
            if (!Assert(Utilities.IsValid(localPlayer), "Invalid local player", this)
                || !Assert(localPlayer.isLocal, "Player is not local", this)
                || !Assert(RemoveLocalPlayerFromZone(localPlayer), "Remove player from zone failed", this)) {
                return false;
            }

            TeleportPlayer(optionTeleportLocation);
            return true;
        }

        public void RefreshPlayersInZone() {
#if TLP_DEBUG
            DebugLog(nameof(RefreshPlayersInZone));
#endif
            if (!Assert(PlayerAudioOverride, $"{nameof(PlayerAudioOverride)} invalid", this)) {
                return;
            }

            PlayerAudioOverride.Refresh();

            var localPlayer = Networking.LocalPlayer;
            if (PlayerAudioOverride.IsAffected(localPlayer) == IsInZone) {
                return;
            }

            if (IsInZone) {
#if TLP_DEBUG
                DebugLog("Player not in override despite being in the zone (network state outdated)");
#endif
                // update
                RemoveLocalPlayerFromZone(localPlayer);
                AddLocalPlayerToZone(localPlayer);
            } else {
#if TLP_DEBUG
                DebugLog("Player in override despite no longer being in the zone (network state outdated)");
#endif
                RemoveLocalPlayerFromZone(localPlayer);
            }

            UpdateNetwork(localPlayer);
        }

        public bool Contains(VRCPlayerApi playerApi) {
#if TLP_DEBUG
            DebugLog(nameof(Contains));
#endif
            return Assert(Utilities.IsValid(PlayerAudioOverride), "playerList invalid", this)
                   && PlayerAudioOverride.IsAffected(playerApi);
        }

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(RefreshPlayersInZone):
#if TLP_DEBUG
                    DebugLog($"{nameof(OnEvent)} {eventName}");
#endif
                    RefreshPlayersInZone();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
        #endregion
    }
}