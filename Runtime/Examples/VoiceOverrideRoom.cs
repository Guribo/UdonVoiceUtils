using System;
using Guribo.UdonUtils.Runtime.Common;
using Guribo.UdonUtils.Runtime.Common.Networking;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VoiceOverrideRoom : UdonSharpBehaviour
    {
        internal bool IsInZone;
        
        #region Mandatory references
        
        #region Teleport settings

        [Header("Teleport settings")]
        public Transform optionalRespawnLocation;
        public bool exitZoneOnRespawn = true;

        #endregion

        [Header("Mandatory references")]
        public BetterPlayerAudioOverride playerOverride;
        public SyncedIntegerArray syncedIntegerArray;
        public UdonDebug udonDebug;

        #endregion
        
        #region Internal

        internal bool AddLocalPlayerToZone(VRCPlayerApi localPlayer)
        {
            if (!udonDebug.Assert(Utilities.IsValid(localPlayer), "Player invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(localPlayer.isLocal, "Player is not local", this))
            {
                return false;
            }

            if (!udonDebug.Assert(Utilities.IsValid(playerOverride), "Player list invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(playerOverride.AddPlayer(localPlayer), "Adding player to player list failed", this))
            {
                return false;
            }

            IsInZone = true;
            UpdateNetwork(localPlayer);

            return true;
        }

        internal bool RemoveLocalPlayerFromZone(VRCPlayerApi localPlayer)
        {
            if (!udonDebug.Assert(Utilities.IsValid(localPlayer), "Player invalid", this)
                || !udonDebug.Assert(localPlayer.isLocal, "Player is not local", this)
                || !udonDebug.Assert(Utilities.IsValid(playerOverride), "Player list invalid", this)
                || !playerOverride.RemovePlayer(localPlayer))
            {
                return false;
            }

            IsInZone = false;

            UpdateNetwork(localPlayer);

            return true;
        }

        internal void TeleportPlayer(Transform target)
        {
            if (Utilities.IsValid(target))
            {
                Networking.LocalPlayer.TeleportTo(target.position,
                    target.rotation,
                    VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                    false);
            }
        }

        internal void UpdateNetwork(VRCPlayerApi localPlayer)
        {
            if (!udonDebug.Assert(Utilities.IsValid(syncedIntegerArray), "Synced integer array invalid", this))
            {
                return;
            }

            var arrayGameObject = syncedIntegerArray.gameObject;
            if (!Networking.IsOwner(arrayGameObject))
            {
                Networking.SetOwner(localPlayer, arrayGameObject);
            }

            syncedIntegerArray.UpdateForAll();
        }

        #endregion

        #region Public
        
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!exitZoneOnRespawn)
            {
                return;
            }

            if (!Contains(player))
            {
                return;
            }
            
            if (!udonDebug.Assert(Utilities.IsValid(player), "player invalid", this)
                  ||  !player.isLocal)
            {
                return;
            }

            ExitRoom(player, optionalRespawnLocation);
        }

        public bool EnterRoom(VRCPlayerApi localPlayer, Transform optionTeleportLocation)
        {
            if (!udonDebug.Assert(Utilities.IsValid(localPlayer), "Invalid local player", this))
            {
                return false;
            }

            if (!udonDebug.Assert(localPlayer.isLocal, "Player is not local", this))
            {
                return false;
            }

            if (!udonDebug.Assert(AddLocalPlayerToZone(localPlayer), "Failed adding player to zone", this))
            {
                return false;
            }

            TeleportPlayer(optionTeleportLocation);
            return true;
        }

        public bool ExitRoom(VRCPlayerApi localPlayer, Transform optionTeleportLocation)
        {
            if (!udonDebug.Assert(Utilities.IsValid(localPlayer), "Invalid local player", this)
                || !udonDebug.Assert(localPlayer.isLocal, "Player is not local", this)
                || !udonDebug.Assert(RemoveLocalPlayerFromZone(localPlayer), "Remove player from zone failed", this))
            {
                return false;
            }

            TeleportPlayer(optionTeleportLocation);
            return true;
        }

        public void RefreshPlayersInZone()
        {
            if (!udonDebug.Assert(playerOverride, "playerList invalid", this))
            {
                return;
            }
            
            playerOverride.Refresh();
            
            var localPlayer = Networking.LocalPlayer;
            if (playerOverride.IsAffected(localPlayer) != IsInZone)
            {
                if (IsInZone)
                {
                    AddLocalPlayerToZone(localPlayer);
                }
                else
                {
                    RemoveLocalPlayerFromZone(localPlayer);
                }

                UpdateNetwork(localPlayer);
            }
        }

        public bool Contains(VRCPlayerApi playerApi)
        {
            return udonDebug.Assert(Utilities.IsValid(playerOverride), "playerList invalid", this)
                   && playerOverride.IsAffected(playerApi);
        }

        #endregion
    }
}