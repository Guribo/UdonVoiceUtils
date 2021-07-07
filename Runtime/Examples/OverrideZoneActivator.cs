using Guribo.UdonUtils.Runtime.Common;
using Guribo.UdonUtils.Runtime.Common.Networking;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class OverrideZoneActivator : UdonSharpBehaviour
    {
        private bool _isInZone;

        #region Teleport settings

        [Header("Teleport settings")]
        public Transform optionalEnterLocation;

        #endregion


        #region Mandatory references

        [Header("Mandatory references")]
        public BetterPlayerAudioOverride playerOverride;
        public PlayerList playerList;
        public SyncedIntegerArray syncedIntegerArray;
        public UdonDebug udonDebug;

        #endregion

        public override void Interact()
        {
            EnterZone(Networking.LocalPlayer);
        }

        public bool EnterZone(VRCPlayerApi localPlayer)
        {
            if (!udonDebug.Assert(AddLocalPlayerToZone(localPlayer), "Failed adding player to zone", this))
            {
                return false;
            }

            TeleportPlayer(optionalEnterLocation);
            return true;
        }

        public bool ExitZone(VRCPlayerApi localPlayer, Transform optionTargetLocation)
        {
            if (!udonDebug.Assert(Utilities.IsValid(localPlayer), "Invalid local player", this)
                || !udonDebug.Assert(localPlayer.isLocal, "Player is not local", this)
                || !udonDebug.Assert(RemoveLocalPlayerFromZone(localPlayer), "Remove player from zone failed", this))
            {
                return false;
            }

            TeleportPlayer(optionTargetLocation);
            return true;
        }

        private bool AddLocalPlayerToZone(VRCPlayerApi player)
        {
            if (!udonDebug.Assert(Utilities.IsValid(player), "Player invalid", this)
                || !udonDebug.Assert(Utilities.IsValid(playerList), "Player list invalid", this)
                || !udonDebug.Assert(playerList.AddPlayer(player), "Adding player to player list failed", this))
            {
                return false;
            }

            _isInZone = true;
            UpdateNetwork(player);

            return true;
        }

        private bool RemoveLocalPlayerFromZone(VRCPlayerApi localPlayer)
        {
            if (!udonDebug.Assert(Utilities.IsValid(localPlayer), "Player invalid", this)
                || !udonDebug.Assert(localPlayer.isLocal, "Player is not local", this)
                || !udonDebug.Assert(Utilities.IsValid(playerList), "Player list invalid", this)
                || !playerList.RemovePlayer(localPlayer))
            {
                return false;
            }

            _isInZone = false;

            UpdateNetwork(localPlayer);

            return true;
        }

        private void TeleportPlayer(Transform target)
        {
            if (Utilities.IsValid(target))
            {
                Networking.LocalPlayer.TeleportTo(target.position,
                    target.rotation,
                    VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                    false);
            }
        }

        private void UpdateNetwork(VRCPlayerApi localPlayer)
        {
            if (!udonDebug.Assert(Utilities.IsValid(syncedIntegerArray), "Synced integer array invalid", this))
            {
                return;
            }

            if (!Networking.IsOwner(syncedIntegerArray.gameObject))
            {
                Networking.SetOwner(localPlayer, syncedIntegerArray.gameObject);
            }

            syncedIntegerArray.UpdateForAll();
        }

        public void PlayerListChanged()
        {
            if (!udonDebug.Assert(Utilities.IsValid(syncedIntegerArray), "Synced integer array invalid", this)
                || !udonDebug.Assert(Utilities.IsValid(playerOverride), "playerOverride invalid", this))
            {
                return;
            }

            if (syncedIntegerArray.oldValue != null)
            {
                foreach (var i in syncedIntegerArray.oldValue)
                {
                    playerOverride.RemovePlayer(VRCPlayerApi.GetPlayerById(i));
                }
            }
            else
            {
                playerOverride.Clear();
            }

            if (syncedIntegerArray.syncedValue != null)
            {
                foreach (var i in syncedIntegerArray.syncedValue)
                {
                    playerOverride.AddPlayer(VRCPlayerApi.GetPlayerById(i));
                }
            }

            if (!udonDebug.Assert(Utilities.IsValid(playerList), "Player list invalid", this))
            {
                return;
            }

            var localPlayer = Networking.LocalPlayer;
            if (playerList.Contains(localPlayer) != _isInZone)
            {
                if (_isInZone)
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
            return udonDebug.Assert(Utilities.IsValid(playerList), "playerList invalid", this)
                    && playerList.Contains(playerApi);
        }
    }
}