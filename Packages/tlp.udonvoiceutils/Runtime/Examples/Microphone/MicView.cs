using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
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
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_IsSet(OnIndicator, nameof(OnIndicator))) {
                return false;
            }

            if (!_IsSet(OffIndicator, nameof(OffIndicator))) {
                return false;
            }

            if (!_InitializeMvcSingleGameObject(gameObject)) {
                _Error("Failed to initialize Model-View-Controller");
                return false;
            }

            _UpdateVisuals();
            return true;
        }

        protected override bool _InitializeInternal() {
            _micModel = (MicModel)Model;
            if (!Utilities.IsValid(_micModel)) {
                _Error($"{nameof(Model)} is not a {nameof(MicModel)}");
                return false;
            }

            _micController = (MicController)Controller;
            if (!Utilities.IsValid(_micController)) {
                _Error($"{nameof(Controller)} is not a {nameof(MicController)}");
                return false;
            }

            _UpdateVisuals();
            return base._InitializeInternal();
        }

        public override void _OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnModelChanged));
#endif
            #endregion

            if (!HasStartedOk) {
                _Error("Not initialized");
                return;
            }

            _UpdateVisuals();
        }
        #endregion

        #region Internal
        private void _UpdateVisuals() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_UpdateVisuals));
#endif
            #endregion

            OnIndicator.SetActive(_micModel.IsOn);
            OffIndicator.SetActive(!_micModel.IsOn);
        }
        #endregion
    }
}