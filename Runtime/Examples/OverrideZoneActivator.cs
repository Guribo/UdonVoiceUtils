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
        public UdonDebug udonDebug;
        public BetterPlayerAudioOverride playerOverride;
        public PlayerList playerList;
        public SyncedIntegerArray syncedIntegerArray;

        // public bool isEntry = true;

        private bool _isInZone;

        public Transform optionalTeleportLocation;

        public GameObject reverbObject;

        private void Start()
        {
            if (Utilities.IsValid(reverbObject))
            {
                reverbObject.SetActive(false);
            }
        }

        public override void Interact()
        {
            EnterZone(Networking.LocalPlayer);
        }

        public bool EnterZone(VRCPlayerApi localPlayer)
        {
            if (!udonDebug.Assert(AddPlayerToZone(localPlayer), "Failed adding player to zone", this))
            {
                return false;
            }

            TeleportPlayer(optionalTeleportLocation);
            return true;
        }

        public bool ExitZone(VRCPlayerApi localPlayer, Transform optionTargetLocation)
        {
            if (!udonDebug.Assert(RemovePlayerFromZone(localPlayer), "Remove player from zone failed", this))
            {
                return false;
            }

            TeleportPlayer(optionTargetLocation);
            return true;
        }

        private bool AddPlayerToZone(VRCPlayerApi player)
        {
            if (!udonDebug.Assert(Utilities.IsValid(player), "Player invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(Utilities.IsValid(playerList), "Player list invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(playerList.AddPlayer(player), "Adding player to player list failed", this))
            {
                return false;
            }

            _isInZone = true;
            UpdateNetwork(player);

            if (Utilities.IsValid(reverbObject))
            {
                reverbObject.SetActive(true);
            }

            return true;
        }

        private bool RemovePlayerFromZone(VRCPlayerApi localPlayer)
        {
            if (!udonDebug.Assert(Utilities.IsValid(localPlayer), "Player invalid", this))
            {
                return false;
            }

            if (!udonDebug.Assert(Utilities.IsValid(playerList), "Player list invalid", this))
            {
                return false;
            }

            // ignore return value
            playerList.RemovePlayer(localPlayer);

            _isInZone = false;

            UpdateNetwork(localPlayer);

            if (Utilities.IsValid(reverbObject))
            {
                reverbObject.SetActive(false);
            }

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
            if (!udonDebug.Assert(Utilities.IsValid(syncedIntegerArray), "Synced integer array invalid", this))
            {
                return;
            }
            
            if (!udonDebug.Assert(Utilities.IsValid(playerOverride), "playerOverride invalid", this))
            {
                return;
            }

            if (syncedIntegerArray.oldValue != null)
            {
                foreach (var i in syncedIntegerArray.oldValue)
                {
                    playerOverride.RemoveAffectedPlayer(VRCPlayerApi.GetPlayerById(i));
                }
            }
            else
            {
                playerOverride.RemoveAll();
            }

            if (syncedIntegerArray.syncedValue != null)
            {
                foreach (var i in syncedIntegerArray.syncedValue)
                {
                    playerOverride.AffectPlayer(VRCPlayerApi.GetPlayerById(i));
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
                    AddPlayerToZone(localPlayer);
                }
                else
                {
                    RemovePlayerFromZone(localPlayer);
                }

                UpdateNetwork(localPlayer);
            }
        }
    }
}