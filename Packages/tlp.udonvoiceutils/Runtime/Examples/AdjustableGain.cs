using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Player;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Examples
{
    [CustomEditor(typeof(AdjustableGain))]
    public class AdjustableGainEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Synced UI slider controlling voice gain on multiple PlayerAudioOverrides. " +
                   "Optionally restricts control to whitelisted players.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    /// <summary>
    /// Controls the VoiceGain on given <see cref="PlayerAudioOverride"/>s globally.
    /// Optionally only whitelisted players are allowed to make changes.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(AdjustableGain), ExecutionOrder)]
    public class AdjustableGain : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = DynamicPrivacy.ExecutionOrder + 1;
        #endregion

        #region NetworkState
        [UdonSynced]
        private float _syncedGain;

        [Tooltip("Voice gain applied to all ControlledOverrides. VRChat range 0–24.")]
        public float Gain = 15f;
        #endregion

        #region Dependencies
        [Tooltip("Optional, can be empty")]
        public PlayerBlackList PlayersAllowedToControl;

        [Tooltip("PlayerAudioOverride components whose voice gain is controlled by this slider.")]
        public PlayerAudioOverride[] ControlledOverrides;

        [Tooltip("Unity UI Slider whose value maps to the voice gain.")]
        public Slider Slider;
        #endregion

        #region State
        private bool _updatingUi;
        #endregion

        #region NetworkEvents
        public override void OnPreSerialization() {
            _syncedGain = Gain;
            base.OnPreSerialization();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
            Gain = _syncedGain;

            RefreshUi();

            // apply gain to all receiving players
            ApplyGain();
        }
        #endregion

        #region UiHooks
        [PublicAPI]
        public void OnGainSliderValueChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnGainSliderValueChanged));
#endif
            #endregion

            if (_updatingUi) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"{nameof(OnGainSliderValueChanged)}: ignoring due to UI refresh");
#endif
                #endregion

                return;
            }

            if (!Utilities.IsValid(Slider)) {
                Error($"{nameof(OnGainSliderValueChanged)}: {nameof(Slider)} is not set");
                return;
            }

            var localPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(PlayersAllowedToControl) &&
                PlayersAllowedToControl.IsBlackListed(localPlayer.DisplayNameSafe())) {
                Warn(
                        $"{nameof(OnGainSliderValueChanged)}: {localPlayer.DisplayNameUniqueSafe()} is not allowed to make changes");
                return;
            }

            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(localPlayer, gameObject);
            }

            if (!Networking.IsOwner(gameObject)) {
                Error($"{nameof(OnGainSliderValueChanged)}: Failed to take ownership");
                return;
            }

            Gain = Slider.value;
            MarkNetworkDirty();

            // apply gain change to player who moved the slider
            ApplyGain();
        }
        #endregion

        #region Internal
        private void ApplyGain() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(ApplyGain)}: {nameof(Gain)}={Gain}");
#endif
            #endregion

            if (ControlledOverrides.LengthSafe() < 1) {
                Warn($"{nameof(ApplyGain)}: {nameof(ControlledOverrides)} is empty");
                return;
            }

            foreach (var controlledOverride in ControlledOverrides) {
                if (!Utilities.IsValid(controlledOverride)) {
                    Warn($"{nameof(ApplyGain)}: {nameof(ControlledOverrides)} contains invalid item");
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"{nameof(ApplyGain)}: {controlledOverride.GetScriptPathInScene()}");
#endif
                #endregion

                controlledOverride.VoiceGain = Gain;
            }
        }

        private void RefreshUi() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RefreshUi));
#endif
            #endregion

            _updatingUi = true;
            if (Utilities.IsValid(Slider)) {
                Slider.value = Gain;
            }

            _updatingUi = false;
        }
        #endregion
    }
}