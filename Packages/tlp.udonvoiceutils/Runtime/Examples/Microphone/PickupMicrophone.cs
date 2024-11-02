using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Sync;
using TLP.UdonUtils.Runtime.Sync.SyncedEvents;
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
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = DynamicPrivacy.ExecutionOrder + 1;
        #endregion

        #region State
        internal bool WorkingIsOn;
        private int _playerId = NoUser;
        internal bool Initialized { private set; get; }
        #endregion

        private int PlayerIdProperty
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

                PlayerIdSync.WorkingValue = _playerId;
                PlayerIdSync.Raise(this);
            }
            get => _playerId;
        }

        #region Mandatory references
        [Header("Mandatory references")]
        public PlayerAudioOverride PlayerAudioOverride;

        public SyncedEventInt PlayerIdSync;
        public SyncedEventBool MicIsOnEvent;
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

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                return;
            }

            if (!gameObject.TakeOwnership()) {
                Error("Failed to take ownership");
                return;
            }

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
        #endregion


        #region Lifecycle
        public void OnEnable() {
            if (Initialized) {
                NewUserStartUsingMic(_playerId);
            }
        }


        public void OnDisable() {
            if (Initialized) {
                CleanUpOldUser(_playerId);
            }
        }
        #endregion


        #region Callbacks
        private void OnMicOnEvent() {
        }

        private void OnUserChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnUserChanged));
#endif
            #endregion

            PlayerIdProperty = PlayerIdSync.WorkingValue;
            MicModel.IsOn = PlayerIdProperty != NoUser;
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(MicModel)) {
                Error($"{nameof(MicModel)} not set");
                return false;
            }

            if (!Utilities.IsValid(PlayerAudioOverride)) {
                Error($"{nameof(PlayerAudioOverride)} not set");
                return false;
            }

            if (!Utilities.IsValid(MicActivation)) {
                Error($"{nameof(MicActivation)} not set");
                return false;
            }

            if (!Utilities.IsValid(PlayerIdSync)) {
                Error($"{nameof(PlayerIdSync)} not set");
                return false;
            }

            if (!Utilities.IsValid(MicIsOnEvent)) {
                Error($"{nameof(MicIsOnEvent)} not set");
                return false;
            }

            if (!MicIsOnEvent.AddListenerVerified(this, nameof(OnMicOnEvent), true)) {
                Error($"Failed to listen to '{MicIsOnEvent.GetScriptPathInScene()}.{nameof(OnMicOnEvent)}' event");
                return false;
            }

            if (!PlayerIdSync.AddListenerVerified(this, nameof(OnUserChanged), true)) {
                Error($"Failed to listen to '{PlayerIdSync.GetScriptPathInScene()}.{nameof(OnUserChanged)}' event");
                return false;
            }

            if (Networking.IsOwner(PlayerIdSync.gameObject)) {
                PlayerIdSync.WorkingValue = NoUser;
            }

            MicActivation.PickupMicrophone = this;
            Initialized = true;

            NewUserStartUsingMic(_playerId);
            return true;
        }

        public override void OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEvent));
#endif
            #endregion

            switch (eventName) {
                case nameof(OnUserChanged):
                    OnUserChanged();
                    break;
                case nameof(OnMicOnEvent):
                    OnMicOnEvent();
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
        #endregion
    }
}