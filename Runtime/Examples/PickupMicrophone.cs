using System.Globalization;
using Guribo.UdonUtils.Runtime.Common.Networking;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(10000)]
    public class PickupMicrophone : UdonSharpBehaviour
    {
        protected const int NoUser = -1;
        
        [HideInInspector, FieldChangeCallback(nameof(PlayerIdProperty))]
        public int playerIdField = NoUser;

        public int PlayerIdProperty
        {
            set
            {
                var valueUnchanged = playerIdField == value;
                if (valueUnchanged)
                {
                    return;
                }

                var oldPlayerId = playerIdField;
                playerIdField = value;
                
                CleanUpOldUser(oldPlayerId);
                NewUserStartUsingMic(playerIdField);

                if (!(Utilities.IsValid(syncedInteger)
                      && Networking.IsOwner(gameObject)))
                {
                    return;
                }

                if (!Networking.IsOwner(syncedInteger.gameObject))
                {
                    Networking.SetOwner(Networking.LocalPlayer, syncedInteger.gameObject);
                }

                syncedInteger.IntValueProperty = value;
            }
            get { return playerIdField; }
        }

        #region Mandatory references

        [Header("Mandatory references")]
        public BetterPlayerAudioOverride betterPlayerAudioOverride;
        public OwnershipTransfer ownershipTransfer;
        [SerializeField] protected SyncedInteger syncedInteger;

        #endregion
        
        public override void OnPickup()
        {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer))
            {
                return;
            }

            TakeOwnership(localPlayer);
            PlayerIdProperty = localPlayer.playerId;
        }

        public override void OnDrop()
        {
            if (Networking.IsOwner(gameObject))
            {
                PlayerIdProperty = NoUser;
            }
        }

        private void OnEnable()
        {
            NewUserStartUsingMic(playerIdField);
        }

        private void OnDisable()
        {
            CleanUpOldUser(playerIdField);
        }

        private void OnDestroy()
        {
            CleanUpOldUser(playerIdField);
        }

        /// <summary>
        /// take ownership of the microphone if the user doesn't have it yet, or force it
        /// </summary>
        /// <param name="localPlayer"></param>
        private void TakeOwnership(VRCPlayerApi localPlayer)
        {
            if (!Utilities.IsValid(ownershipTransfer))
            {
                Debug.LogError("PickupMicrophone.TakeOwnership: ownershipTransfer is invalid");
                return;
            }

            if (!ownershipTransfer.TransferOwnership(gameObject, localPlayer, true))
            {
                Debug.LogError("PickupMicrophone.TakeOwnership: failed to transfer ownership");
            }
        }

        /// <summary>
        /// if the mic is still held by the given user let that person no longer be affected by the mic
        /// </summary>
        private void CleanUpOldUser(int oldUser)
        {
            if (oldUser == NoUser)
            {
                return;
            }

            var currentMicUser = VRCPlayerApi.GetPlayerById(oldUser);
            if (Utilities.IsValid(currentMicUser))
            {
                if (Utilities.IsValid(betterPlayerAudioOverride))
                {
                    betterPlayerAudioOverride.RemovePlayer(currentMicUser);
                }
            }
        }

        /// <summary>
        /// let the given user be affected by the mic
        /// </summary>
        private void NewUserStartUsingMic(int newUser)
        {
            if (newUser == NoUser)
            {
                return;
            }

            var newMicUser = VRCPlayerApi.GetPlayerById(newUser);
            if (!Utilities.IsValid(newMicUser))
            {
                return;
            }

            if (Utilities.IsValid(betterPlayerAudioOverride))
            {
                betterPlayerAudioOverride.AddPlayer(newMicUser);
            }
        }
    }
}