using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Examples
{
    [CustomEditor(typeof(DynamicPrivacy))]
    public class DynamicPrivacyEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
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
            _DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!LocalPlayerAdded._AddListenerVerified(this, nameof(_OnLocalPlayerAdded))) {
                _ErrorAndDisableComponent($"Failed to listen to {nameof(LocalPlayerAdded)}");
                return;
            }

            if (!LocalPlayerRemoved._AddListenerVerified(this, nameof(_OnLocalPlayerRemoved))) {
                _ErrorAndDisableComponent($"Failed to listen to {nameof(LocalPlayerRemoved)}");
            }
        }

        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!LocalPlayerAdded._RemoveListener(this, true)) {
                _ErrorAndDisableComponent($"Failed to stop listening to {nameof(LocalPlayerAdded)}");
            }

            if (!LocalPlayerRemoved._RemoveListener(this, true)) {
                _ErrorAndDisableComponent($"Failed to stop listening to {nameof(LocalPlayerRemoved)}");
            }
        }
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_IsSet(LocalPlayerAdded, nameof(LocalPlayerAdded))) {
                return false;
            }

            if (!_IsSet(LocalPlayerRemoved, nameof(LocalPlayerRemoved))) {
                return false;
            }

            if (!_IsSet(OverrideWithDynamicPrivacy, nameof(OverrideWithDynamicPrivacy))) {
                return false;
            }

            if (!OverrideWithDynamicPrivacy.HasStartedOk) {
                _Error($"{nameof(OverrideWithDynamicPrivacy)} not initialized");
                return false;
            }

            return true;
        }

        public override void _OnEvent(string eventName) {
            switch (eventName) {
                case nameof(_OnLocalPlayerAdded):
                    _OnLocalPlayerAdded();
                    break;
                case nameof(_OnLocalPlayerRemoved):
                    _OnLocalPlayerRemoved();
                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Events
        internal void _OnLocalPlayerAdded() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnLocalPlayerAdded));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (OverrideWithDynamicPrivacy.PrivacyChannelIds.Remove(PlayerExitedPrivacyChannelId)) {
                // no-op, just to make sure the two ids are not present at the same time

                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog(
                        $"{nameof(_OnLocalPlayerAdded)}: removed channel {nameof(PlayerExitedPrivacyChannelId)} {PlayerExitedPrivacyChannelId}");
#endif
                #endregion;
            }

            if (OverrideWithDynamicPrivacy.PrivacyChannelIds.ContainsKey(PlayerAddedPrivacyChannelId)) {
                _Error(
                        $"{nameof(_OnLocalPlayerAdded)}: {nameof(PlayerAddedPrivacyChannelId)} " +
                        $"{PlayerAddedPrivacyChannelId} is already in use by " +
                        $"{OverrideWithDynamicPrivacy._GetScriptPathInScene()}");
                return;
            }

            OverrideWithDynamicPrivacy.PrivacyChannelIds.Add(PlayerAddedPrivacyChannelId, new DataToken());

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(
                    $"{nameof(_OnLocalPlayerAdded)}: added channel {nameof(PlayerExitedPrivacyChannelId)} {PlayerAddedPrivacyChannelId}");
#endif
            #endregion;
        }

        internal void _OnLocalPlayerRemoved() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnLocalPlayerRemoved));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!OverrideWithDynamicPrivacy.PrivacyChannelIds.Remove(PlayerAddedPrivacyChannelId)) {
                _Error(
                        $"{nameof(_OnLocalPlayerRemoved)}: {nameof(PlayerAddedPrivacyChannelId)} " +
                        $"{PlayerAddedPrivacyChannelId} not found in " +
                        $"{OverrideWithDynamicPrivacy._GetScriptPathInScene()}");
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            else {
                _DebugLog(
                        $"{nameof(_OnLocalPlayerRemoved)}: removed channel {nameof(PlayerAddedPrivacyChannelId)} {PlayerAddedPrivacyChannelId}");
            }

#endif
            #endregion;

            if (OverrideWithDynamicPrivacy.PrivacyChannelIds.ContainsKey(PlayerExitedPrivacyChannelId)) {
                _Warn(
                        $"{nameof(_OnLocalPlayerRemoved)}: {nameof(PlayerExitedPrivacyChannelId)} " +
                        $"{PlayerExitedPrivacyChannelId} is already in use by " +
                        $"{OverrideWithDynamicPrivacy._GetScriptPathInScene()}");
                return;
            }

            OverrideWithDynamicPrivacy.PrivacyChannelIds.Add(PlayerExitedPrivacyChannelId, new DataToken());

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(
                    $"{nameof(_OnLocalPlayerRemoved)}: added channel {nameof(PlayerExitedPrivacyChannelId)} {PlayerExitedPrivacyChannelId}");
#endif
            #endregion;
        }
        #endregion
    }
}