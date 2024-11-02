using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    /// <summary>
    /// All players that are in contact with the trigger are affected. Exiting the trigger removes the player from the
    /// associated voice override.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VoiceOverrideTriggerZone), ExecutionOrder)]
    public class VoiceOverrideTriggerZone : TlpBaseBehaviour
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VoiceOverrideRoomEnterButton.ExecutionOrder + 1;
        #endregion

        #region Mandatory references
        [FormerlySerializedAs("playerAudioOverride")]
        [Header("Mandatory references")]
        public PlayerAudioOverride PlayerAudioOverride;
        #endregion

        #region State
        private Collider[] _allTrigger;
        internal bool Initialized { private set; get; }
        #endregion

        #region Lifecycle
        public void OnDisable() {
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            if (!Utilities.IsValid(PlayerAudioOverride) || PlayerAudioOverride.Clear()) {
                return;
            }

            Error("Failed to clear on disable");
        }
        #endregion

        #region Player Events
        public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(OnPlayerTriggerEnter));
#endif
            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            if (!Utilities.IsValid(player)) {
                Error($"{nameof(OnPlayerTriggerEnter)}: Invalid player entered");
                return;
            }

            if (!PlayerAudioOverride.AddPlayer(player)) {
                Error($"Failed to add {player.ToStringSafe()} to {PlayerAudioOverride.GetScriptPathInScene()}");
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(OnPlayerTriggerExit));
#endif
            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            if (!Utilities.IsValid(player)) {
                Error($"{nameof(OnPlayerTriggerExit)}: Invalid player exited");
                return;
            }

            if (!PlayerAudioOverride.RemovePlayer(player)) {
                Error($"Failed to remove {player.ToStringSafe()} from {PlayerAudioOverride.GetScriptPathInScene()}");
            }
        }
        #endregion


        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(PlayerAudioOverride)) {
                Error($"{nameof(PlayerAudioOverride)} not set");
                return false;
            }

            if (!PlayerAudioOverride.ForceNoSynchronization()) {
                Error($"Failed to disable synchronization on {PlayerAudioOverride.GetScriptPathInScene()}");
                return false;
            }

            Initialized = true;

            // ensure that players already being inside the trigger before udon starts are detected by disabling them
            // once and enabling them 1 frame later again
            _allTrigger = gameObject.GetComponents<Collider>();
            DisableAllTrigger();
            SendCustomEventDelayedFrames(nameof(EnableAllTriggerDelayed), 1, EventTiming.LateUpdate);
            return true;
        }
        #endregion

        #region Internal
        private void DisableAllTrigger() {
#if TLP_DEBUG
            DebugLog(nameof(DisableAllTrigger));
#endif

            foreach (var trigger in _allTrigger) {
                if (trigger.isTrigger) {
                    trigger.enabled = false;
                }
            }
        }

        public void EnableAllTriggerDelayed() {
#if TLP_DEBUG
            DebugLog(nameof(EnableAllTriggerDelayed));
#endif
            foreach (var trigger in _allTrigger) {
                if (trigger.isTrigger) {
                    trigger.enabled = true;
                }
            }
        }
        #endregion
    }
}