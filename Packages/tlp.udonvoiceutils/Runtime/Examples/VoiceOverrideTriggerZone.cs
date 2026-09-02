using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Examples
{
    [CustomEditor(typeof(VoiceOverrideTriggerZone))]
    public class VoiceOverrideTriggerZoneEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Adds or removes players from a PlayerAudioOverride when " +
                   "entering or exiting a trigger zone.";
        }
    }
}
#endif
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
        public override int ExecutionOrderReadOnly => ExecutionOrder;

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
            _DebugLog(nameof(OnDisable));
#endif
            if (!Utilities.IsValid(PlayerAudioOverride) || PlayerAudioOverride._Clear()) {
                return;
            }

            _Error("Failed to clear on disable");
        }
        #endregion

        #region Player Events
        public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
#if TLP_DEBUG
            _DebugLog(nameof(OnPlayerTriggerEnter));
#endif
            if (!Initialized) {
                _Error("Not initialized");
                return;
            }

            if (!Utilities.IsValid(player)) {
                _Error($"{nameof(OnPlayerTriggerEnter)}: Invalid player entered");
                return;
            }

            if (!PlayerAudioOverride._AddPlayer(player)) {
                _Error($"Failed to add {player._ToStringSafe()} to {PlayerAudioOverride._GetScriptPathInScene()}");
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player) {
#if TLP_DEBUG
            _DebugLog(nameof(OnPlayerTriggerExit));
#endif
            if (!Initialized) {
                _Error("Not initialized");
                return;
            }

            if (!Utilities.IsValid(player)) {
                _Error($"{nameof(OnPlayerTriggerExit)}: Invalid player exited");
                return;
            }

            if (!PlayerAudioOverride._RemovePlayer(player)) {
                _Error($"Failed to remove {player._ToStringSafe()} from {PlayerAudioOverride._GetScriptPathInScene()}");
            }
        }
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(PlayerAudioOverride)) {
                _Error($"{nameof(PlayerAudioOverride)} not set");
                return false;
            }

            if (!PlayerAudioOverride._ForceNoSynchronization()) {
                _Error($"Failed to disable synchronization on {PlayerAudioOverride._GetScriptPathInScene()}");
                return false;
            }

            Initialized = true;

            // ensure that players already being inside the trigger before udon starts are detected by disabling them
            // once and enabling them 1 frame later again
            _allTrigger = gameObject.GetComponents<Collider>();
            _DisableAllTrigger();
            SendCustomEventDelayedFrames(nameof(_EnableAllTriggerDelayed), 1, EventTiming.LateUpdate);
            return true;
        }
        #endregion

        #region Internal
        private void _DisableAllTrigger() {
#if TLP_DEBUG
            _DebugLog(nameof(_DisableAllTrigger));
#endif

            foreach (var trigger in _allTrigger) {
                if (trigger.isTrigger) {
                    trigger.enabled = false;
                }
            }
        }

        public void _EnableAllTriggerDelayed() {
#if TLP_DEBUG
            _DebugLog(nameof(_EnableAllTriggerDelayed));
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