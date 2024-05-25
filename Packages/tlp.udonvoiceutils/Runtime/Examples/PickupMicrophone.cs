using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Sync;
using TLP.UdonUtils.Runtime.Sync.SyncedEvents;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class PickupMicrophone : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioController.ExecutionOrder + 1;

        [SerializeField]
        private SyncedEventInt PlayerIdSync;

        private int _playerId = NoUser;

        public int PlayerIdProperty
        {
            set
            {
                DebugLog($"Set {nameof(PlayerIdProperty)} = {value}");
                bool valueUnchanged = _playerId == value;
                if (valueUnchanged) {
                    return;
                }

                int oldPlayerId = _playerId;
                _playerId = value;

                CleanUpOldUser(oldPlayerId);
                NewUserStartUsingMic(_playerId);

                if (!(Utilities.IsValid(PlayerIdSync)
                      && Networking.IsOwner(gameObject))) {
                    return;
                }

                if (!Networking.IsOwner(PlayerIdSync.gameObject)) {
                    Networking.SetOwner(Networking.LocalPlayer, PlayerIdSync.gameObject);
                }

                PlayerIdSync.Value = _playerId;
                PlayerIdSync.Raise(this);
            }
            get => _playerId;
        }

        #region Mandatory references
        [Header("Mandatory references")]
        public PlayerAudioOverride PlayerAudioOverride;
        #endregion

        public override void Start() {
            base.Start();

            if (!Utilities.IsValid(PlayerIdSync)) {
                Error($"{nameof(PlayerIdSync)} not set");
                return;
            }

            PlayerIdSync.AddListenerVerified(this, nameof(UserChanged));
            PlayerIdSync.Value = NoUser;
        }

        public override void OnPickup() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnPickup));
#endif
            #endregion

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                return;
            }

            TakeOwnership(localPlayer);
            PlayerIdProperty = localPlayer.playerId;
        }

        public override void OnDrop() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDrop));
#endif
            #endregion


            if (Networking.IsOwner(gameObject)) {
                PlayerIdProperty = NoUser;
            }
        }

        private void OnEnable() {
            NewUserStartUsingMic(_playerId);
        }

        private void OnDisable() {
            CleanUpOldUser(_playerId);
        }

        private void OnDestroy() {
            CleanUpOldUser(_playerId);
        }

        /// <summary>
        /// take ownership of the microphone if the user doesn't have it yet, or force it
        /// </summary>
        /// <param name="localPlayer"></param>
        private void TakeOwnership(VRCPlayerApi localPlayer) {
            if (!OwnershipTransfer.TransferOwnership(gameObject, localPlayer, true)) {
                Error("PickupMicrophone.TakeOwnership: failed to transfer ownership");
            }
        }

        /// <summary>
        /// if the mic is still held by the given user let that person no longer be affected by the mic
        /// </summary>
        private void CleanUpOldUser(int oldUser) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CleanUpOldUser));
#endif
            #endregion

            if (oldUser == NoUser) {
                return;
            }

            var currentMicUser = VRCPlayerApi.GetPlayerById(oldUser);
            if (Utilities.IsValid(currentMicUser)) {
                if (Utilities.IsValid(PlayerAudioOverride)) {
                    PlayerAudioOverride.RemovePlayer(currentMicUser);
                }
            }
        }

        /// <summary>
        /// let the given user be affected by the mic
        /// </summary>
        private void NewUserStartUsingMic(int newUser) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(NewUserStartUsingMic));
#endif
            #endregion

            if (newUser == NoUser) {
                return;
            }

            var newMicUser = VRCPlayerApi.GetPlayerById(newUser);
            if (!Utilities.IsValid(newMicUser)) {
                return;
            }

            if (Utilities.IsValid(PlayerAudioOverride)) {
                PlayerAudioOverride.AddPlayer(newMicUser);
            }
        }

        public override void OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEvent));
#endif
            #endregion

            switch (eventName) {
                case nameof(UserChanged):
                    UserChanged();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }

        private void UserChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(UserChanged));
#endif
            #endregion

            PlayerIdProperty = PlayerIdSync.Value;
        }
    }
}