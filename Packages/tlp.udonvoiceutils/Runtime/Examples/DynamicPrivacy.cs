using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
#if UNITY_EDITOR
using TLP.UdonVoiceUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Examples
{
    [CustomEditor(typeof(DynamicPrivacy))]
    public class DynamicPrivacyEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Dynamically manages privacy channels for voice communication based " +
                   "on player presence. Automatically adds/removes a single privacy channel " +
                   "ID when local players enters or exits the monitored area, " +
                   "providing seamless voice privacy control.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class DynamicPrivacy : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VoiceOverrideTriggerZone.ExecutionOrder + 1;

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

            if (!HasStartedOk) {
                return;
            }

            if (!LocalPlayerAdded.AddListenerVerified(this, nameof(OnLocalPlayerAdded))) {
                ErrorAndDisableComponent($"Failed to listen to {nameof(LocalPlayerAdded)}");
                return;
            }

            if (!LocalPlayerRemoved.AddListenerVerified(this, nameof(OnLocalPlayerRemoved))) {
                ErrorAndDisableComponent($"Failed to listen to {nameof(LocalPlayerRemoved)}");
            }
        }

        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!LocalPlayerAdded.RemoveListener(this, true)) {
                ErrorAndDisableComponent($"Failed to stop listening to {nameof(LocalPlayerAdded)}");
            }

            if (!LocalPlayerRemoved.RemoveListener(this, true)) {
                ErrorAndDisableComponent($"Failed to stop listening to {nameof(LocalPlayerRemoved)}");
            }
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!IsSet(LocalPlayerAdded, nameof(LocalPlayerAdded))) {
                return false;
            }

            if (!IsSet(LocalPlayerRemoved, nameof(LocalPlayerRemoved))) {
                return false;
            }

            if (!IsSet(OverrideWithDynamicPrivacy, nameof(OverrideWithDynamicPrivacy))) {
                return false;
            }

            if (!OverrideWithDynamicPrivacy.HasStartedOk) {
                Error($"{nameof(OverrideWithDynamicPrivacy)} not initialized");
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

            if (!HasStartedOk) {
                return;
            }

            if (OverrideWithDynamicPrivacy.PrivacyChannelIds.Remove(PlayerExitedPrivacyChannelId)) {
                // no-op, just to make sure the two ids are not present at the same time

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog(
                        $"{nameof(OnLocalPlayerAdded)}: removed channel {nameof(PlayerExitedPrivacyChannelId)} {PlayerExitedPrivacyChannelId}");
#endif
                #endregion;
            }

            if (OverrideWithDynamicPrivacy.PrivacyChannelIds.ContainsKey(PlayerAddedPrivacyChannelId)) {
                Error(
                        $"{nameof(OnLocalPlayerAdded)}: {nameof(PlayerAddedPrivacyChannelId)} " +
                        $"{PlayerAddedPrivacyChannelId}is already in use by " +
                        $"{OverrideWithDynamicPrivacy.GetScriptPathInScene()}");
                return;
            }

            OverrideWithDynamicPrivacy.PrivacyChannelIds.Add(PlayerAddedPrivacyChannelId, new DataToken());

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"{nameof(OnLocalPlayerAdded)}: added channel {nameof(PlayerExitedPrivacyChannelId)} {PlayerAddedPrivacyChannelId}");
#endif
            #endregion;
        }

        internal void OnLocalPlayerRemoved() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnLocalPlayerRemoved));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!OverrideWithDynamicPrivacy.PrivacyChannelIds.Remove(PlayerAddedPrivacyChannelId)) {
                Error(
                        $"{nameof(OnLocalPlayerRemoved)}: {nameof(PlayerAddedPrivacyChannelId)} " +
                        $"{PlayerAddedPrivacyChannelId} not found in " +
                        $"{OverrideWithDynamicPrivacy.GetScriptPathInScene()}");
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            else {
                DebugLog(
                        $"{nameof(OnLocalPlayerAdded)}: removed channel {nameof(PlayerAddedPrivacyChannelId)} {PlayerAddedPrivacyChannelId}");
            }

#endif
            #endregion;

            if (OverrideWithDynamicPrivacy.PrivacyChannelIds.ContainsKey(PlayerExitedPrivacyChannelId)) {
                Warn(
                        $"{nameof(OnLocalPlayerAdded)}: {nameof(PlayerExitedPrivacyChannelId)} " +
                        $"{PlayerExitedPrivacyChannelId} is already in use by " +
                        $"{OverrideWithDynamicPrivacy.GetScriptPathInScene()}");
                return;
            }

            OverrideWithDynamicPrivacy.PrivacyChannelIds.Add(PlayerExitedPrivacyChannelId, new DataToken());

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"{nameof(OnLocalPlayerAdded)}: added channel {nameof(PlayerExitedPrivacyChannelId)} {PlayerExitedPrivacyChannelId}");
#endif
            #endregion;
        }
        #endregion
    }
}