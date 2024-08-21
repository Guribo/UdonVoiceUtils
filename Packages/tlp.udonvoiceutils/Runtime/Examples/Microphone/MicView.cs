using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples.Microphone
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class MicView : View
    {
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

            if (!InitializeMvcSingleGameObject(gameObject)) {
                Error("Failed to initialize Model-View-Controller");
                return false;
            }

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

            if (!Initialized) {
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

            bool isOn = _micModel.IsOn;
            OnIndicator.SetActive(isOn);
            OffIndicator.SetActive(!isOn);
        }
        #endregion
    }
}