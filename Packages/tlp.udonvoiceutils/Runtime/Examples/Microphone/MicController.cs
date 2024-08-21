using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UdonSharp;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples.Microphone
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class MicController : Controller
    {
        #region State
        private MicModel _micModel;
        private MicView _micView;
        #endregion

        protected override bool InitializeInternal() {
            _micModel = (MicModel)Model;
            if (!Utilities.IsValid(_micModel)) {
                Error($"{nameof(Model)} is not a {nameof(MicModel)}");
                return false;
            }

            _micView = (MicView)View;
            if (!Utilities.IsValid(_micView)) {
                Error($"{nameof(Controller)} is not a {nameof(MicView)}");
                return false;
            }

            return base.InitializeInternal();
        }

        #region PublicApi

        public void Activate() {

        }

        #endregion
    }
}