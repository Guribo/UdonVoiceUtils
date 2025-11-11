using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples.Microphone
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(MicView), ExecutionOrder)]
    public class MicView : View
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioView.ExecutionOrder + 1;

        #region State
        private MicModel _micModel;
        private MicController _micController;
        #endregion

        #region Visuals
        public GameObject OnIndicator;
        public GameObject OffIndicator;
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!IsSet(OnIndicator, nameof(OnIndicator))) {
                return false;
            }

            if (!IsSet(OffIndicator, nameof(OffIndicator))) {
                return false;
            }

            if (!InitializeMvcSingleGameObject(gameObject)) {
                Error("Failed to initialize Model-View-Controller");
                return false;
            }

            UpdateVisuals();
            return true;
        }

        protected override bool InitializeInternal() {
            _micModel = (MicModel)Model;
            if (!Utilities.IsValid(_micModel)) {
                Error($"{nameof(Model)} is not a {nameof(MicModel)}");
                return false;
            }

            _micController = (MicController)Controller;
            if (!Utilities.IsValid(_micController)) {
                Error($"{nameof(Controller)} is not a {nameof(MicController)}");
                return false;
            }

            UpdateVisuals();
            return base.InitializeInternal();
        }

        public override void OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnModelChanged));
#endif
            #endregion

            if (!HasStartedOk) {
                Error("Not initialized");
                return;
            }

            UpdateVisuals();
        }
        #endregion

        #region Internal
        private void UpdateVisuals() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(UpdateVisuals));
#endif
            #endregion

            OnIndicator.SetActive(_micModel.IsOn);
            OffIndicator.SetActive(!_micModel.IsOn);
        }
        #endregion
    }
}