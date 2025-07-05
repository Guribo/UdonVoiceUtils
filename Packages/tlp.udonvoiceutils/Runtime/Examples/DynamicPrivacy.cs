using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
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

        [Header("Privacy Channel Settings")]
        [SerializeField]
        [Tooltip("Privacy channels to set when player is added to the zone")]
        private int[] PlayerAddedPrivacyChannelIds;

        [SerializeField]
        [Tooltip("Privacy channels to set when player exits the zone")]
        private int[] PlayerExitedPrivacyChannelIds;

        [Header("Advanced Settings")]
        [SerializeField]
        [Tooltip("If true, adds channels to existing list. If false, replaces entire list.")]
        private bool AdditiveModeOnEnter = false;

        [SerializeField]
        [Tooltip("If true, removes only specified channels on exit. If false, replaces entire list.")]
        private bool RemoveModeOnExit = false;

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

            if (!Utilities.IsValid(OverrideWithDynamicPrivacy)) {
                return;
            }

            if (PlayerAddedPrivacyChannelIds == null || PlayerAddedPrivacyChannelIds.Length == 0) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog("No privacy channels to add on player enter");
#endif
                #endregion
                return;
            }

            if (AdditiveModeOnEnter) {
                // Add channels to existing list
                for (int i = 0; i < PlayerAddedPrivacyChannelIds.Length; i++) {
                    OverrideWithDynamicPrivacy.AddPrivacyChannel(PlayerAddedPrivacyChannelIds[i]);
                }
            } else {
                // Replace entire list
                SetPrivacyChannels(PlayerAddedPrivacyChannelIds);
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Updated privacy channels on player enter. Channel count: {OverrideWithDynamicPrivacy.PrivacyChannelIds.Count}");
#endif
            #endregion
        }

        internal void OnLocalPlayerRemoved() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnLocalPlayerRemoved));
#endif
            #endregion

            if (!Utilities.IsValid(OverrideWithDynamicPrivacy)) {
                return;
            }

            if (RemoveModeOnExit) {
                // Remove specific channels
                if (PlayerExitedPrivacyChannelIds != null && PlayerExitedPrivacyChannelIds.Length > 0) {
                    for (int i = 0; i < PlayerExitedPrivacyChannelIds.Length; i++) {
                        OverrideWithDynamicPrivacy.RemovePrivacyChannel(PlayerExitedPrivacyChannelIds[i]);
                    }
                }
            } else {
                // Replace entire list
                if (PlayerExitedPrivacyChannelIds != null && PlayerExitedPrivacyChannelIds.Length > 0) {
                    SetPrivacyChannels(PlayerExitedPrivacyChannelIds);
                } else {
                    // Clear all channels
                    ClearPrivacyChannels();
                }
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Updated privacy channels on player exit. Channel count: {OverrideWithDynamicPrivacy.PrivacyChannelIds.Count}");
#endif
            #endregion
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Set the privacy channels to the specified array, replacing any existing channels
        /// </summary>
        /// <param name="channelIds">Array of channel IDs to set</param>
        private void SetPrivacyChannels(int[] channelIds) {
            if (!Utilities.IsValid(OverrideWithDynamicPrivacy)) {
                return;
            }

            // Clear existing channels
            ClearPrivacyChannels();

            // Add new channels
            if (channelIds != null && channelIds.Length > 0) {
                for (int i = 0; i < channelIds.Length; i++) {
                    OverrideWithDynamicPrivacy.AddPrivacyChannel(channelIds[i]);
                }
            }
        }

        /// <summary>
        /// Clear all privacy channels
        /// </summary>
        private void ClearPrivacyChannels() {
            if (!Utilities.IsValid(OverrideWithDynamicPrivacy)) {
                return;
            }

            if (OverrideWithDynamicPrivacy.PrivacyChannelIdsList == null) {
                OverrideWithDynamicPrivacy.PrivacyChannelIdsList = new DataList();
            } else {
                OverrideWithDynamicPrivacy.PrivacyChannelIdsList.Clear();
            }
        }

        /// <summary>
        /// Public method to manually add a privacy channel
        /// </summary>
        /// <param name="channelId">Channel ID to add</param>
        public void AddPrivacyChannel(int channelId) {
            if (Utilities.IsValid(OverrideWithDynamicPrivacy)) {
                OverrideWithDynamicPrivacy.AddPrivacyChannel(channelId);
            }
        }

        /// <summary>
        /// Public method to manually remove a privacy channel
        /// </summary>
        /// <param name="channelId">Channel ID to remove</param>
        public void RemovePrivacyChannel(int channelId) {
            if (Utilities.IsValid(OverrideWithDynamicPrivacy)) {
                OverrideWithDynamicPrivacy.RemovePrivacyChannel(channelId);
            }
        }

        /// <summary>
        /// Public method to manually set all privacy channels
        /// </summary>
        /// <param name="channelIds">Array of channel IDs to set</param>
        public void SetPrivacyChannelsPublic(int[] channelIds) {
            SetPrivacyChannels(channelIds);
        }

        /// <summary>
        /// Public method to manually clear all privacy channels
        /// </summary>
        public void ClearAllPrivacyChannels() {
            ClearPrivacyChannels();
        }
        #endregion
    }
}