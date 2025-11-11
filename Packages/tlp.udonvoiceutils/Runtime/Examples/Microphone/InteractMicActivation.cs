using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonVoiceUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Examples
{
    [CustomEditor(typeof(InteractMicActivation))]
    public class InteractMicActivationEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
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

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!IsSet(VrcPickup, nameof(VrcPickup))) {
                return false;
            }

            UpdateInteractionText();
            return true;
        }

        public override void OnPickupUseDown() {
            base.OnPickupUseDown();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnPickupUseDown));
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
                if (!Activate()) {
                    Error($"{nameof(OnPickupUseDown)}: failed to activate mic");
                }
            } else {
                ToggleOnState();
            }

            UpdateInteractionText();
        }


        public override void OnPickupUseUp() {
            base.OnPickupUseUp();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnPickupUseUp));
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
                if (!Deactivate()) {
                    Error($"{nameof(OnPickupUseUp)}: failed to deactivate mic");
                }
            }

            UpdateInteractionText();
        }

        public override void OnPickup() {
            base.OnPickup();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnPickup));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            _framePickedUp = Time.frameCount;

            UpdateInteractionText();
        }

        public override void OnDrop() {
            base.OnDrop();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDrop));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!DeactivateOnDrop && !HoldUseToTalk) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Leaving mic in {(IsOn ? "ON" : "OFF")} state");
#endif
                #endregion

                return;
            }

            if (!Deactivate()) {
                Error($"{nameof(OnDrop)} failed to deactivate mic");
                return;
            }

            UpdateInteractionText();
        }
        #endregion


        #region Internal
        private void UpdateInteractionText() {
            VrcPickup.UseText = $"Turn {(IsOn ? "OFF" : "ON")}";
        }

        private void ToggleOnState() {
            if (IsOn) {
                Deactivate();
            } else {
                Activate();
            }
        }
        #endregion
    }
}