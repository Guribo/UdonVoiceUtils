using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class DynamicPrivacy : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioOverrideList.ExecutionOrder + 1;

        [SerializeField]
        private UdonEvent LocalPlayerAdded;

        [SerializeField]
        private UdonEvent LocalPlayerRemoved;

        [FormerlySerializedAs("OverrideWithDynamicPriority")]
        [SerializeField]
        private PlayerAudioOverride OverrideWithDynamicPrivacy;


        [SerializeField]
        private int PlayerAddedPrivacyChannelId;

        [SerializeField]
        private int PlayerExitedPrivacyChannelId;


        #region Lifecylce
        public void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (!Utilities.IsValid(LocalPlayerAdded) ||
                !LocalPlayerAdded.AddListenerVerified(this, nameof(OnLocalPlayerAdded))) {
                ErrorAndDisableComponent($"Failed to listen to {nameof(LocalPlayerAdded)}");
                return;
            }

            if (!Utilities.IsValid(LocalPlayerRemoved) ||
                !LocalPlayerRemoved.AddListenerVerified(this, nameof(OnLocalPlayerRemoved))) {
                ErrorAndDisableComponent($"Failed to listen to {nameof(LocalPlayerRemoved)}");
            }
        }

        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (!Utilities.IsValid(LocalPlayerAdded) || !LocalPlayerAdded.RemoveListener(this, true)) {
                ErrorAndDisableComponent($"Failed to stop listening to {nameof(LocalPlayerAdded)}");
            }

            if (!Utilities.IsValid(LocalPlayerRemoved) || !LocalPlayerRemoved.RemoveListener(this, true)) {
                ErrorAndDisableComponent($"Failed to stop listening to {nameof(LocalPlayerRemoved)}");
            }
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(LocalPlayerAdded)) {
                Error($"{nameof(LocalPlayerAdded)} is not set");
                return false;
            }

            if (!Utilities.IsValid(LocalPlayerRemoved)) {
                Error($"{nameof(LocalPlayerRemoved)} is not set");
                return false;
            }

            if (!Utilities.IsValid(OverrideWithDynamicPrivacy)) {
                Error($"{nameof(OverrideWithDynamicPrivacy)} is not set");
                return false;
            }

            return true;
        }

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(OnLocalPlayerAdded):
                    OnLocalPlayerAdded();
                    break;
                case nameof(OnLocalPlayerRemoved):
                    OnLocalPlayerRemoved();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Events
        internal void OnLocalPlayerAdded() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnLocalPlayerAdded));
#endif
            #endregion

            if (Utilities.IsValid(OverrideWithDynamicPrivacy)) {
                OverrideWithDynamicPrivacy.PrivacyChannelId = PlayerAddedPrivacyChannelId;
            }
        }

        internal void OnLocalPlayerRemoved() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnLocalPlayerRemoved));
#endif
            #endregion

            if (Utilities.IsValid(OverrideWithDynamicPrivacy)) {
                OverrideWithDynamicPrivacy.PrivacyChannelId = PlayerExitedPrivacyChannelId;
            }
        }
        #endregion
    }
}