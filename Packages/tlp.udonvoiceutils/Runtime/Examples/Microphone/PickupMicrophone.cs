using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sync;
using TLP.UdonVoiceUtils.Runtime.Core;
using TLP.UdonVoiceUtils.Runtime.Examples.Microphone;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PickupMicrophone), ExecutionOrder)]
    public class PickupMicrophone : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedPlayerAudioConfigurationModel.ExecutionOrder + 1;
        #endregion

        #region State
        private int _currentUser = NoUser;
        #endregion

        #region Mandatory references
        [Header("Mandatory references")]
        public PlayerAudioOverride PlayerAudioOverride;

        public MicModel MicModel;

        public MicActivation MicActivation;
        #endregion

        #region Pickup
        public override void OnPickup() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnPickup));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!gameObject.TakeOwnership()) {
                Error("Failed to take ownership");
                return;
            }

            MicModel.UserId = LocalPlayer.playerId;
        }

        public override void OnDrop() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDrop));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (Networking.IsOwner(gameObject)) {
                MicModel.UserId = NoUser;
            }
        }
        #endregion

        #region Lifecycle
        public void OnEnable() {
            if (HasStartedOk) {
                OnModelChanged();
            }
        }

        public void OnDisable() {
            if (HasStartedOk) {
                RemoveUserFromAudioOverride(_currentUser);
            }
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!IsSet(MicModel, nameof(MicModel))) {
                return false;
            }

            if (!IsSet(PlayerAudioOverride, nameof(PlayerAudioOverride))) {
                return false;
            }

            if (!IsSet(MicActivation, nameof(MicActivation))) {
                return false;
            }

            if (!MicModel.HasStartedOk) {
                Error($"Failed to start {nameof(MicModel)}");
                return false;
            }

            if (!MicModel.ChangeEvent.AddListenerVerified(this, nameof(OnModelChanged))) {
                Error($"Failed to listen to '{MicModel.GetScriptPathInScene()}.{nameof(MicModel.ChangeEvent)}' event");
                return false;
            }

            MicActivation.MicModel = MicModel;

            OnModelChanged();
            return true;
        }

        public override void OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEvent));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            switch (eventName) {
                case nameof(OnModelChanged):
                    OnModelChanged();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Internal
        /// <summary>
        /// let the given user be affected by the mic
        /// </summary>
        private void AddUserToAudioOverride(int newUser) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(AddUserToAudioOverride));
#endif
            #endregion

            if (newUser == NoUser) {
                return;
            }

            var newMicUser = VRCPlayerApi.GetPlayerById(newUser);
            if (!Utilities.IsValid(newMicUser)) {
                return;
            }

            if (PlayerAudioOverride.IsAffected(newMicUser)) {
                return; // already added, nothing to do
            }

            if (!PlayerAudioOverride.AddPlayer(newMicUser)) {
                Error($"Failed to add player {newMicUser.DisplayNameUnique()} " +
                      $"to {PlayerAudioOverride.GetScriptPathInScene()}");
            }
        }

        /// <summary>
        /// if the mic is still held by the given user let that person no longer be affected by the mic
        /// </summary>
        private void RemoveUserFromAudioOverride(int user) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RemoveUserFromAudioOverride));
#endif
            #endregion

            if (user == NoUser) {
                return;
            }

            var currentMicUser = VRCPlayerApi.GetPlayerById(user);
            if (!Utilities.IsValid(currentMicUser)) {
                return;
            }

            if (!PlayerAudioOverride.IsAffected(currentMicUser)) {
                return; // not affected, nothing to do
            }

            if (!PlayerAudioOverride.RemovePlayer(currentMicUser)) {
                Warn(
                        $"Failed to remove player {currentMicUser.DisplayNameUnique()} " +
                        $"{PlayerAudioOverride.GetScriptPathInScene()} " +
                        $"(was never added in the first place?)");
            }
        }

        private void OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnModelChanged));
#endif
            #endregion

            if (MicIsOffOrUserChanged()) {
                RemoveUserFromAudioOverride(_currentUser);
                _currentUser = MicModel.UserId;
            }

            if (!MicModel.IsOn) {
                return;
            }

            AddUserToAudioOverride(MicModel.UserId);
        }

        private bool MicIsOffOrUserChanged() {
            return !MicModel.IsOn || _currentUser != MicModel.UserId;
        }
        #endregion
    }
}