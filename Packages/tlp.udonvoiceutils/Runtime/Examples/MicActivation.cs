using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonVoiceUtils.Runtime.Examples.Microphone;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(MicActivation), ExecutionOrder)]
    public abstract class MicActivation : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = MicModel.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        [Header("Mandatory references")]
        public PickupMicrophone PickupMicrophone;
        #endregion

        #region State
        protected internal bool Initialized { get; private set; }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(PickupMicrophone)) {
                Error($"{nameof(PickupMicrophone)} not set");
                return false;
            }

            Initialized = true;
            return true;
        }
        #endregion

        #region Internal
        protected internal bool Activate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Activate));
#endif
            #endregion

            if (!Initialized) {
                Error("Not initialized");
                return false;
            }

            UpdateMicOnState(true);
            return true;
        }

        protected internal bool Deactivate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Activate));
#endif
            #endregion

            if (!Initialized) {
                Error("Not initialized");
                return false;
            }

            UpdateMicOnState(false);
            return true;
        }

        private void UpdateMicOnState(bool newOn) {
            PickupMicrophone.WorkingIsOn = newOn;
            if (!Networking.IsOwner(PickupMicrophone.gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, PickupMicrophone.gameObject);
            }

            PickupMicrophone.MarkNetworkDirty();
        }
        #endregion
    }
}