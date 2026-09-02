using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Sync;
using TLP.UdonUtils.Runtime.Sync.SyncedEvents;
using UdonSharp;
using UnityEngine;

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

        #region Dependencies

        [SerializeField]
        internal SyncedEventInt PlayerHoldingMic;

        [SerializeField]
        internal SyncedEventBool MicIsOnEvent;

        [SerializeField]
        internal UdonEvent Change;
        #endregion

        #region Public API
        /// <summary>
        /// Id of the player holding the mic, -1 if not held
        /// </summary>
        public int UserId
        {
            get => _GetUserId();
            set => _SetUserId(value);
        }

        public bool IsOn
        {
            get => _GetIsOnState();
            set => _SetIsOnState(value);
        }
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_IsSet(MicIsOnEvent, nameof(MicIsOnEvent))) {
                return false;
            }

            if (!_IsSet(PlayerHoldingMic, nameof(PlayerHoldingMic))) {
                return false;
            }

            if (!PlayerHoldingMic._AddListenerVerified(this, nameof(_OnUserChanged))) {
                _Error($"Failed to listen to '{PlayerHoldingMic._GetScriptPathInScene()}.{nameof(_OnUserChanged)}' event");
                return false;
            }

            if (!_IsSet(Change, nameof(Change))) {
                return false;
            }

            if (!MicIsOnEvent._AddListenerVerified(this, nameof(_OnMicOnEvent))) {
                _Error($"Failed to listen to '{MicIsOnEvent._GetScriptPathInScene()}.{nameof(_OnMicOnEvent)}' event");
                return false;
            }

            return true;
        }

        public override void _OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_OnEvent)} {eventName}");
#endif
            #endregion

            if (!HasStartedOk) {
                _Error("Not initialized");
                return;
            }

            switch (eventName) {
                case nameof(_OnMicOnEvent):
                    _OnMicOnEvent();
                    break;
                case nameof(_OnUserChanged):
                    _OnUserChanged();
                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Callbacks
        internal void _OnUserChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnUserChanged));
#endif
            #endregion

            Dirty = true;
            _NotifyIfDirty(1);
        }

        internal void _OnMicOnEvent() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnMicOnEvent));
#endif
            #endregion

            Dirty = true;
            _NotifyIfDirty(1);
        }
        #endregion

        #region Internal
        internal void _SetIsOnState(bool value) {
            if (!HasStartedOk) {
                _Error("Not initialized");
                return;
            }

            if (value == MicIsOnEvent.WorkingValue) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"{MicIsOnEvent._GetScriptPathInScene()} value is already {value}");
#endif
#endregion
                return;
            }

            if (!MicIsOnEvent._TakeOwnership()) {
                _Error($"Failed to take ownership of {MicIsOnEvent._GetScriptPathInScene()}");
                return;
            }

            MicIsOnEvent.WorkingValue = value;
            MicIsOnEvent._Raise(this);
            Dirty = true;
            _NotifyIfDirty();
        }

        internal bool _GetIsOnState() {
            if (!HasStartedOk) {
                _Error("Not initialized");
                return false;
            }

            return MicIsOnEvent.WorkingValue;
        }

        internal int _GetUserId() {
            if (!HasStartedOk) {
                _Error("Not initialized");
                return -1;
            }

            return PlayerHoldingMic.WorkingValue;
        }

        internal void _SetUserId(int value) {
            if (!HasStartedOk) {
                _Error("Not initialized");
                return;
            }

            if (value == PlayerHoldingMic.WorkingValue) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"{PlayerHoldingMic._GetScriptPathInScene()} value is already {value}");
#endif
#endregion
                return;
            }

            if (!PlayerHoldingMic._TakeOwnership()) {
                _Error($"Failed to take ownership of {PlayerHoldingMic._GetScriptPathInScene()}");
                return;
            }

            PlayerHoldingMic.WorkingValue = value;
            PlayerHoldingMic._Raise(this);
            Dirty = true;
            _NotifyIfDirty();
        }
        #endregion
    }
}