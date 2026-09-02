using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sync;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
using TLP.UdonVoiceUtils.Runtime.Examples.Microphone;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Examples
{
    [CustomEditor(typeof(PickupMicrophone))]
    public class PickupMicrophoneEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Synced pickup that applies a PlayerAudioOverride to " +
                   "whoever holds the microphone.";
        }
    }
}
#endif
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
            _DebugLog(nameof(OnPickup));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!gameObject._TakeOwnership()) {
                _Error("Failed to take ownership");
                return;
            }

            MicModel.UserId = LocalPlayer.playerId;
        }

        public override void OnDrop() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDrop));
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
        internal void OnEnable() {
            if (HasStartedOk) {
                _OnModelChanged();
            }
        }

        public void OnDisable() {
            if (HasStartedOk) {
                _RemoveUserFromAudioOverride(_currentUser);
            }
        }
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_IsSet(MicModel, nameof(MicModel))) {
                return false;
            }

            if (!_IsSet(PlayerAudioOverride, nameof(PlayerAudioOverride))) {
                return false;
            }

            if (!_IsSet(MicActivation, nameof(MicActivation))) {
                return false;
            }

            if (!MicModel.HasStartedOk) {
                _Error($"Failed to start {nameof(MicModel)}");
                return false;
            }

            if (!MicModel.ChangeEvent._AddListenerVerified(this, nameof(_OnModelChanged))) {
                _Error($"Failed to listen to '{MicModel._GetScriptPathInScene()}.{nameof(MicModel.ChangeEvent)}' event");
                return false;
            }

            MicActivation.MicModel = MicModel;

            _OnModelChanged();
            return true;
        }

        public override void _OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnEvent));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            switch (eventName) {
                case nameof(_OnModelChanged):
                    _OnModelChanged();
                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Internal
        /// <summary>
        /// let the given user be affected by the mic
        /// </summary>
        private void _AddUserToAudioOverride(int newUser) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_AddUserToAudioOverride));
#endif
            #endregion

            if (newUser == NoUser) {
                return;
            }

            var newMicUser = VRCPlayerApi.GetPlayerById(newUser);
            if (!Utilities.IsValid(newMicUser)) {
                return;
            }

            if (PlayerAudioOverride._IsAffected(newMicUser)) {
                return; // already added, nothing to do
            }

            if (!PlayerAudioOverride._AddPlayer(newMicUser)) {
                _Error(
                        $"Failed to add player {newMicUser._DisplayNameUnique()} " +
                        $"to {PlayerAudioOverride._GetScriptPathInScene()}");
            }
        }

        /// <summary>
        /// if the mic is still held by the given user let that person no longer be affected by the mic
        /// </summary>
        private void _RemoveUserFromAudioOverride(int user) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RemoveUserFromAudioOverride));
#endif
            #endregion

            if (user == NoUser) {
                return;
            }

            var currentMicUser = VRCPlayerApi.GetPlayerById(user);
            if (!Utilities.IsValid(currentMicUser)) {
                return;
            }

            if (!PlayerAudioOverride._IsAffected(currentMicUser)) {
                return; // not affected, nothing to do
            }

            if (!PlayerAudioOverride._RemovePlayer(currentMicUser)) {
                _Warn(
                        $"Failed to remove player {currentMicUser._DisplayNameUnique()} " +
                        $"{PlayerAudioOverride._GetScriptPathInScene()} " +
                        $"(was never added in the first place?)");
            }
        }

        private void _OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnModelChanged));
#endif
            #endregion

            if (_MicIsOffOrUserChanged()) {
                _RemoveUserFromAudioOverride(_currentUser);
                _currentUser = MicModel.UserId;
            }

            if (!MicModel.IsOn) {
                return;
            }

            _AddUserToAudioOverride(MicModel.UserId);
        }

        private bool _MicIsOffOrUserChanged() {
            return !MicModel.IsOn || _currentUser != MicModel.UserId;
        }
        #endregion
    }
}