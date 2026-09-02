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
        public MicModel MicModel;
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_IsSet(MicModel, nameof(MicModel))) {
                return false;
            }

            return true;
        }

        public bool IsOn => MicModel.IsOn;

        protected internal bool _Activate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Activate));
#endif
            #endregion

            if (!HasStartedOk) {
                return false;
            }

            MicModel.IsOn = true;
            return true;
        }

        protected internal bool _Deactivate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Deactivate));
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