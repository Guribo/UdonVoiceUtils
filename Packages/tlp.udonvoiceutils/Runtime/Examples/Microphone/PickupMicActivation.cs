
namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    public class PickupMicActivation : MicActivation
    {
        public override void OnDrop() {
            base.OnDrop();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDrop));
#endif
            #endregion

            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            if (!Activate()) {
                Error($"{nameof(OnPickup)} failed to activate mic");
            }
        }

        public override void OnPickup() {
            base.OnPickup();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnPickup));
#endif
            #endregion

            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            if (!Activate()) {
                Error($"{nameof(OnPickup)} failed to activate mic");
            }
        }
    }
}