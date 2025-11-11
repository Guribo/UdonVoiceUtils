using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

#if UNITY_EDITOR
using TLP.UdonVoiceUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Examples
{
    [CustomEditor(typeof(PickupMicActivation))]
    public class PickupMicActivationEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Activates the microphone when the object is picked up and deactivates it when dropped.";
        }
    }
}
#endif

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [TlpDefaultExecutionOrder(typeof(PickupMicActivation), ExecutionOrder)]
    public class PickupMicActivation : MicActivation
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = MicActivation.ExecutionOrder + 2;

        #region Overrides
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

            if (!Activate()) {
                Error($"{nameof(OnPickup)} failed to activate mic");
            }
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

            if (!Deactivate()) {
                Error($"{nameof(OnDrop)} failed to deactivate mic");
            }
        }
        #endregion
    }
}