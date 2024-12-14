using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Sync;
using TLP.UdonUtils.Runtime.Sync.SyncedEvents;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples.Microphone
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(MicModel), ExecutionOrder)]
    public class MicModel : Model
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PickupMicrophone.ExecutionOrder + 1;

        [SerializeField]
        internal SyncedEventInt PlayerHoldingMic;

        [SerializeField]
        internal SyncedEventBool MicIsOnEvent;

        #region Public API
        /// <summary>
        /// Id of the player holding the mic, -1 if not held
        /// </summary>
        public int UserId
        {
            get => GetUserId();
            set => SetUserId(value);
        }

        public bool IsOn
        {
            get => GetIsOnState();
            set => SetIsOnState(value);
        }
        #endregion

        #region Overrides
        protected override bool InitializeInternal() {
            if (!Utilities.IsValid(PlayerHoldingMic)) {
                Error($"{nameof(PlayerHoldingMic)} not set");
                return false;
            }

            if (!PlayerHoldingMic.AddListenerVerified(this, nameof(OnUserChanged))) {
                Error($"Failed to listen to '{PlayerHoldingMic.GetScriptPathInScene()}.{nameof(OnUserChanged)}' event");
                return false;
            }

            if (!Utilities.IsValid(MicIsOnEvent)) {
                Error($"{nameof(MicIsOnEvent)} not set");
                return false;
            }

            if (!MicIsOnEvent.AddListenerVerified(this, nameof(OnMicOnEvent))) {
                Error($"Failed to listen to '{MicIsOnEvent.GetScriptPathInScene()}.{nameof(OnMicOnEvent)}' event");
                return false;
            }

            return base.InitializeInternal();
        }

        public override void OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnEvent)} {eventName}");
#endif
            #endregion

            if (!HasStartedOk) {
                Error("Not initialized");
                return;
            }

            switch (eventName) {
                case nameof(OnMicOnEvent):
                    OnMicOnEvent();
                    break;
                case nameof(OnUserChanged):
                    OnUserChanged();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Callbacks
        private void OnUserChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnUserChanged));
#endif
            #endregion

            Dirty = true;
            NotifyIfDirty(1);
        }

        private void OnMicOnEvent() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnMicOnEvent));
#endif
            #endregion

            Dirty = true;
            NotifyIfDirty(1);
        }
        #endregion

        #region Internal
        internal void SetIsOnState(bool value) {
            if (!HasStartedOk) {
                Error("Not initialized");
                return;
            }

            if (value == MicIsOnEvent.WorkingValue) {
                Warn($"{MicIsOnEvent.GetScriptPathInScene()} value is already {value}");
                return;
            }

            if (!MicIsOnEvent.TakeOwnership()) {
                Error($"Failed to take ownership of {MicIsOnEvent.GetScriptPathInScene()}");
                return;
            }

            MicIsOnEvent.WorkingValue = value;
            MicIsOnEvent.Raise(this);
        }

        internal bool GetIsOnState() {
            if (!HasStartedOk) {
                Error("Not initialized");
                return false;
            }

            return MicIsOnEvent.WorkingValue;
        }

        internal int GetUserId() {
            if (!HasStartedOk) {
                Error("Not initialized");
                return -1;
            }

            return PlayerHoldingMic.WorkingValue;
        }

        internal void SetUserId(int value) {
            if (!HasStartedOk) {
                Error("Not initialized");
                return;
            }

            if (value == PlayerHoldingMic.WorkingValue) {
                Warn($"{PlayerHoldingMic.GetScriptPathInScene()} value is already {value}");
                return;
            }

            if (!PlayerHoldingMic.TakeOwnership()) {
                Error($"Failed to take ownership of {PlayerHoldingMic.GetScriptPathInScene()}");
                return;
            }

            PlayerHoldingMic.WorkingValue = value;
            PlayerHoldingMic.Raise(this);
        }
        #endregion
    }
}