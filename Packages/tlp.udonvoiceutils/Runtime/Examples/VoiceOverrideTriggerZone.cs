using TLP.UdonUtils;
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
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(TlpExecutionOrder.AudioStart)]
    public class VoiceOverrideTriggerZone : TlpBaseBehaviour
    {
        #region Mandatory references
        [FormerlySerializedAs("playerAudioOverride")]
        [Header("Mandatory references")]
        public PlayerAudioOverride PlayerAudioOverride;

        private Collider[] _allTrigger;

        public override void Start() {
            base.Start();
            // ensure that players already being inside the trigger before udon starts are detected by disabling them
            // once and enabling them 1 frame later again  
            _allTrigger = gameObject.GetComponents<Collider>();
            DisableAllTrigger();
            SendCustomEventDelayedFrames(nameof(EnableAllTriggerDelayed), 1, EventTiming.LateUpdate);
        }

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
        #endregion

        public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(OnPlayerTriggerEnter));
#endif
            if (!Assert(Utilities.IsValid(player), "Entering player invalid", this)) {
                return;
            }

            if (!Assert(Utilities.IsValid(PlayerAudioOverride), "playerAudioOverride invalid", this)) {
                return;
            }

            Assert(PlayerAudioOverride.AddPlayer(player), "Failed to add player", this);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(OnPlayerTriggerExit));
#endif
            if (!Assert(Utilities.IsValid(player), "Exiting player invalid", this)) {
                return;
            }

            if (!Assert(Utilities.IsValid(PlayerAudioOverride), "playerAudioOverride invalid", this)) {
                return;
            }

            Assert(PlayerAudioOverride.RemovePlayer(player), "Failed to remove player", this);
        }

        public void OnDisable() {
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            if (!Assert(Utilities.IsValid(PlayerAudioOverride), "playerAudioOverride invalid", this)) {
                return;
            }

            Assert(PlayerAudioOverride.Clear(), "Failed to clear on disable", this);
        }
    }
}