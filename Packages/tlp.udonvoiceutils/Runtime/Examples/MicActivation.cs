using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonVoiceUtils.Runtime.Examples.Microphone;
using UdonSharp;
using UnityEngine;


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

        #region State
        internal MicModel MicModel;
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!IsSet(MicModel, nameof(MicModel))) {
                return false;
            }

            return true;
        }
        #endregion

        #region Overrides

        public bool IsOn => MicModel.IsOn;

        protected internal bool Activate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Activate));
#endif
            #endregion

            if (!HasStartedOk) {
                return false;
            }

            MicModel.IsOn = true;
            return true;
        }

        protected internal bool Deactivate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Deactivate));
#endif
            #endregion

            if (!HasStartedOk) {
                return false;
            }

            MicModel.IsOn = false;
            return true;
        }
        #endregion
    }
}