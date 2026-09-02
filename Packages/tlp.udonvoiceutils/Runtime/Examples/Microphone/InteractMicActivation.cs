using JetBrains.Annotations;
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
    [CustomEditor(typeof(InteractMicActivation))]
    public class InteractMicActivationEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Toggles the microphone ON/OFF when the player interacts with it. " +
                   $"Optionally deactivates the microphone when dropped if " +
                   $"'{nameof(InteractMicActivation.DeactivateOnDrop)}' is enabled.";
        }
    }
}
#endif

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [TlpDefaultExecutionOrder(typeof(InteractMicActivation), ExecutionOrder)]
    public class InteractMicActivation : MicActivation
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PickupMicActivation.ExecutionOrder + 1;
        #endregion

        #region Configuration
        [Tooltip(
                "If true the mic will be deactivated when the player drops it. " +
                "Otherwise, it will remember its current state when picked up again.")]
        public bool DeactivateOnDrop = true;

        [Tooltip("If true the player must hold the use button to keep the mic ON")]
        public bool HoldUseToTalk;
        #endregion

        #region Dependencies
        [Tooltip("Used to update the 'Use' interaction text")]
        [SerializeField]
        private VRC_Pickup VrcPickup;
        #endregion

        #region State
        private int _framePickedUp;
        #endregion

        #region Overrides

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_IsSet(VrcPickup, nameof(VrcPickup))) {
                return false;
            }

            _UpdateInteractionText();
            return true;
        }

        public override void OnPickupUseDown() {
            base.OnPickupUseDown();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnPickupUseDown));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (_framePickedUp == Time.frameCount) {
                // prevent picking up triggering usage when interacting for the first time
                return;
            }

            if (HoldUseToTalk) {
                if (!_Activate()) {
                    _Error($"{nameof(OnPickupUseDown)}: failed to activate mic");
                }
            } else {
                _ToggleOnState();
            }

            _UpdateInteractionText();
        }


        public override void OnPickupUseUp() {
            base.OnPickupUseUp();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnPickupUseUp));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (_framePickedUp == Time.frameCount) {
                // prevent picking up triggering usage when interacting for the first time
                return;
            }

            if (HoldUseToTalk) {
                if (!_Deactivate()) {
                    _Error($"{nameof(OnPickupUseUp)}: failed to deactivate mic");
                }
            }

            _UpdateInteractionText();
        }

        public override void OnPickup() {
            base.OnPickup();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnPickup));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            _framePickedUp = Time.frameCount;

            _UpdateInteractionText();
        }

        public override void OnDrop() {
            base.OnDrop();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDrop));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!DeactivateOnDrop && !HoldUseToTalk) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"Leaving mic in {(IsOn ? "ON" : "OFF")} state");
#endif
                #endregion

                return;
            }

            if (!_Deactivate()) {
                _Error($"{nameof(OnDrop)} failed to deactivate mic");
                return;
            }

            _UpdateInteractionText();
        }
        #endregion

        #region Internal
        private void _UpdateInteractionText() {
            VrcPickup.UseText = $"Turn {(IsOn ? "OFF" : "ON")}";
        }

        private void _ToggleOnState() {
            if (IsOn) {
                _Deactivate();
            } else {
                _Activate();
            }
        }
        #endregion
    }
}