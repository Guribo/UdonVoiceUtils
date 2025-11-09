using System.Globalization;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerAudioView), ExecutionOrder)]
    public class PlayerAudioView : View
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = View.ExecutionOrder + 100;


        #region Ui Elements
        #region General Settings
        [Header("General Settings")]
        [SerializeField]
        internal Slider SliderOcclusionFactor;

        [SerializeField]
        internal Slider SliderPlayerOcclusionFactor;

        [SerializeField]
        internal Slider SliderListenerDirectionality;

        [SerializeField]
        internal Slider SliderPlayerDirectionality;

        [SerializeField]
        internal Text TextListenerDirectionality;

        [SerializeField]
        internal Text TextPlayerDirectionality;

        [SerializeField]
        internal Text TextOcclusionFactor;

        [SerializeField]
        internal Text TextPlayerOcclusionFactor;

        [SerializeField]
        internal Text TextAllowMasterControl;

        [SerializeField]
        internal Toggle ToggleAllowMasterControl;
        #endregion

        #region Voice Settings
        [Header("Voice Settings")]
        [SerializeField]
        internal Slider SliderVoiceDistanceNear;

        [SerializeField]
        internal Slider SliderVoiceDistanceFar;

        [SerializeField]
        internal Slider SliderVoiceGain;

        [SerializeField]
        internal Slider SliderVoiceVolumetricRadius;

        [SerializeField]
        internal Text TextVoiceDistanceNear;

        [SerializeField]
        internal Text TextVoiceDistanceFar;

        [SerializeField]
        internal Text TextVoiceGain;

        [SerializeField]
        internal Text TextVoiceVolumetricRadius;

        [SerializeField]
        internal Toggle ToggleVoiceLowpass;
        #endregion

        #region Avatar Settings
        [Header("Avatar Settings")]
        [SerializeField]
        internal Slider SliderAvatarDistanceNear;

        [SerializeField]
        internal Slider SliderAvatarDistanceFar;

        [SerializeField]
        internal Slider SliderAvatarGain;

        [SerializeField]
        internal Slider SliderAvatarVolumetricRadius;

        [SerializeField]
        internal Text AvatarDistanceNear;

        [SerializeField]
        internal Text AvatarDistanceFar;

        [SerializeField]
        internal Text AvatarGain;

        [SerializeField]
        internal Text AvatarVolumetricRadius;

        [SerializeField]
        internal Toggle ToggleAvatarSpatialize;

        [SerializeField]
        internal Toggle ToggleAvatarCustomCurve;
        #endregion


        #region Events
        [Header("Events")]
        [SerializeField]
        internal UdonEvent UserChangedSettings;

        [SerializeField]
        internal UdonEvent ResetToDefault;
        #endregion

        #region Dependencies
        [Header("Dependencies")]
        [SerializeField]
        internal PlayerAudioConfigurationModel MasterConfig;

        [SerializeField]
        internal PlayerAudioConfigurationModel LocalConfig;

        [SerializeField]
        internal PlayerAudioConfigurationModel DefaultValues;
        #endregion
        #endregion

        #region State
        private bool _preventChangeEvent;
        private PlayerAudioConfigurationModel _displayedConfig;

        private PlayerAudioController _playerAudioController;
        #endregion

        #region Public API
        /// <summary>
        /// Callback function, can be called by the BetterPlayerAudio to update the UI
        /// </summary>
        public void UpdateUi() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(UpdateUi));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!IsViewInitialized) {
                Error($"Not initialized, did you reference it correctly on the {nameof(PlayerAudioController)}?");
                return;
            }

            var owner = Networking.GetOwner(_playerAudioController.gameObject);
            if (Utilities.IsValid(owner)) {
                TextAllowMasterControl.text = $"Let {owner.displayName} (owner) control everything";
            }

            _preventChangeEvent = true;

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Applying values from {_displayedConfig.GetScriptPathInScene()} to UI");
#endif
            #endregion

            UpdateSliderChecked(SliderOcclusionFactor, _displayedConfig.OcclusionFactor);
            UpdateSliderChecked(SliderPlayerOcclusionFactor, _displayedConfig.PlayerOcclusionFactor);
            UpdateSliderChecked(SliderListenerDirectionality, _displayedConfig.ListenerDirectionality);
            UpdateSliderChecked(SliderPlayerDirectionality, _displayedConfig.PlayerDirectionality);
            UpdateSliderChecked(SliderVoiceDistanceNear, _displayedConfig.VoiceDistanceNear);
            UpdateSliderChecked(SliderVoiceDistanceFar, _displayedConfig.VoiceDistanceFar);
            UpdateSliderChecked(SliderVoiceGain, _displayedConfig.VoiceGain);
            UpdateSliderChecked(SliderVoiceVolumetricRadius, _displayedConfig.VoiceVolumetricRadius);
            UpdateSliderChecked(SliderAvatarDistanceNear, _displayedConfig.AvatarNearRadius);
            UpdateSliderChecked(SliderAvatarDistanceFar, _displayedConfig.AvatarFarRadius);
            UpdateSliderChecked(SliderAvatarGain, _displayedConfig.AvatarGain);
            UpdateSliderChecked(SliderAvatarVolumetricRadius, _displayedConfig.AvatarVolumetricRadius);

            ToggleVoiceLowpass.isOn = _displayedConfig.EnableVoiceLowpass;
            ToggleAvatarSpatialize.isOn = _displayedConfig.ForceAvatarSpatialAudio;
            ToggleAvatarCustomCurve.isOn = _displayedConfig.AllowAvatarCustomAudioCurves;
            ToggleAllowMasterControl.isOn = LocalConfig.AllowMasterControlLocalValues;

            _preventChangeEvent = false;

            UpdateTextAndInteractiveElements();
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!IsSet(UserChangedSettings, nameof(UserChangedSettings))) {
                return false;
            }

            if (!IsSet(ResetToDefault, nameof(ResetToDefault))) {
                return false;
            }

            if (!UserChangedSettings.AddListenerVerified(this, nameof(OnSettingsChanged), true)) {
                return false;
            }

            if (!ResetToDefault.AddListenerVerified(this, nameof(ResetAll), true)) {
                return false;
            }

            return true;
        }

        public override void OnEvent(string eventName) {
            if (!HasStartedOk) {
                return;
            }

            switch (eventName) {
                case nameof(OnSettingsChanged):
                    OnSettingsChanged();
                    break;
                case nameof(ResetAll):
                    ResetAll();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }

        #region MVC
        public override void OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnModelChanged));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (Utilities.IsValid(EventInstigator) && Networking.IsMaster &&
                ReferenceEquals(EventInstigator, MasterConfig)) {
                // nothing to do
                return;
            }

            bool masterControls = LocalConfig.AllowMasterControlLocalValues && !Networking.IsMaster;

            _displayedConfig = masterControls ? MasterConfig : LocalConfig;

            UpdateUi();
        }

        protected override bool InitializeInternal() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InitializeInternal));
#endif
            #endregion

            if (!Utilities.IsValid(DefaultValues)) {
                ErrorAndDisableComponent($"{nameof(DefaultValues)} not set");
                return false;
            }

            if (!Utilities.IsValid(LocalConfig)) {
                ErrorAndDisableComponent($"{nameof(LocalConfig)} not set");
                return false;
            }

            if (!Utilities.IsValid(MasterConfig)) {
                ErrorAndDisableComponent($"{nameof(MasterConfig)} not set");
                return false;
            }

            if (!LocalConfig.ChangeEvent.AddListenerVerified(this, nameof(OnModelChanged))) {
                ErrorAndDisableComponent($"Failed to add listener to {nameof(LocalConfig)}");
                return false;
            }

            if (!MasterConfig.ChangeEvent.AddListenerVerified(this, nameof(OnModelChanged))) {
                ErrorAndDisableComponent($"Failed to add listener to {nameof(MasterConfig)}");
                return false;
            }

            if (!Utilities.IsValid(Controller)) {
                ErrorAndDisableComponent($"{nameof(Controller)} invalid");
                return false;
            }

            _playerAudioController = (PlayerAudioController)Controller;
            bool masterControls = LocalConfig.AllowMasterControlLocalValues && !Networking.IsMaster;
            _displayedConfig = masterControls ? MasterConfig : LocalConfig;
            return true;
        }

        protected override bool DeInitializeInternal() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(DeInitializeInternal));
#endif
            #endregion

            if (!Utilities.IsValid(LocalConfig)) {
                Warn($"{nameof(LocalConfig)} was destroyed");
                return true; // OK in case of de-init
            }

            if (!Utilities.IsValid(LocalConfig.ChangeEvent)) {
                Warn($"{nameof(LocalConfig.ChangeEvent)} was destroyed");
                return true; // OK in case of de-init
            }

            if (!Utilities.IsValid(MasterConfig)) {
                Warn($"{nameof(MasterConfig)} was destroyed");
                return true; // OK in case of de-init
            }

            if (!Utilities.IsValid(MasterConfig.ChangeEvent)) {
                Warn($"{nameof(MasterConfig.ChangeEvent)} was destroyed");
                return true; // OK in case of de-init
            }

            bool successLocal = LocalConfig.ChangeEvent.RemoveListener(this, true);
            bool successMaster = MasterConfig.ChangeEvent.RemoveListener(this, true);

            if (!successLocal) {
                Error($"Failed to remove listener from {nameof(LocalConfig)}");
            }

            if (!successMaster) {
                Error($"Failed to remove listener from {nameof(MasterConfig)}");
            }

            return successLocal && successMaster;
        }
        #endregion
        #endregion


        #region Internal
        internal void OnSettingsChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnSettingsChanged));
#endif
            #endregion

            if (!IsViewInitialized) {
                Warn($"Skipping {nameof(OnSettingsChanged)} event as {nameof(PlayerAudioView)} is not ready");
                return;
            }

            if (_preventChangeEvent) {
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Updating {LocalConfig.GetScriptPathInScene()} from UI");
#endif
            #endregion

            UpdateValueLogged(
                    ref LocalConfig.OcclusionFactor,
                    nameof(LocalConfig.OcclusionFactor),
                    SliderOcclusionFactor.value);
            UpdateValueLogged(
                    ref LocalConfig.PlayerOcclusionFactor,
                    nameof(LocalConfig.PlayerOcclusionFactor),
                    SliderPlayerOcclusionFactor.value);
            UpdateValueLogged(
                    ref LocalConfig.PlayerDirectionality,
                    nameof(LocalConfig.PlayerDirectionality),
                    SliderPlayerDirectionality.value);
            UpdateValueLogged(
                    ref LocalConfig.ListenerDirectionality,
                    nameof(LocalConfig.ListenerDirectionality),
                    SliderListenerDirectionality.value);
            UpdateValueLogged(
                    ref LocalConfig.VoiceDistanceNear,
                    nameof(LocalConfig.VoiceDistanceNear),
                    SliderVoiceDistanceNear.value);
            UpdateValueLogged(
                    ref LocalConfig.VoiceDistanceFar,
                    nameof(LocalConfig.VoiceDistanceFar),
                    SliderVoiceDistanceFar.value);
            UpdateValueLogged(
                    ref LocalConfig.VoiceGain,
                    nameof(LocalConfig.VoiceGain),
                    SliderVoiceGain.value);
            UpdateValueLogged(
                    ref LocalConfig.VoiceVolumetricRadius,
                    nameof(LocalConfig.VoiceVolumetricRadius),
                    SliderVoiceVolumetricRadius.value);
            UpdateValueLogged(
                    ref LocalConfig.EnableVoiceLowpass,
                    nameof(LocalConfig.EnableVoiceLowpass),
                    ToggleVoiceLowpass.isOn);
            UpdateValueLogged(
                    ref LocalConfig.AvatarNearRadius,
                    nameof(LocalConfig.AvatarNearRadius),
                    SliderAvatarDistanceNear.value);
            UpdateValueLogged(
                    ref LocalConfig.AvatarFarRadius,
                    nameof(LocalConfig.AvatarFarRadius),
                    SliderAvatarDistanceFar.value);
            UpdateValueLogged(
                    ref LocalConfig.AvatarGain,
                    nameof(LocalConfig.AvatarGain),
                    SliderAvatarGain.value);
            UpdateValueLogged(
                    ref LocalConfig.AvatarVolumetricRadius,
                    nameof(LocalConfig.AvatarVolumetricRadius),
                    SliderAvatarVolumetricRadius.value);
            UpdateValueLogged(
                    ref LocalConfig.ForceAvatarSpatialAudio,
                    nameof(LocalConfig.ForceAvatarSpatialAudio),
                    ToggleAvatarSpatialize.isOn);
            UpdateValueLogged(
                    ref LocalConfig.AllowAvatarCustomAudioCurves,
                    nameof(LocalConfig.AllowAvatarCustomAudioCurves),
                    ToggleAvatarCustomCurve.isOn);
            UpdateValueLogged(
                    ref LocalConfig.AllowMasterControlLocalValues,
                    nameof(LocalConfig.AllowMasterControlLocalValues),
                    ToggleAllowMasterControl.isOn);

            LocalConfig.Dirty = true;
            LocalConfig.NotifyIfDirty(1);

            if (Networking.IsMaster) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Updating Master config values from UI");
#endif
                #endregion

                UpdateValueLogged(
                        ref MasterConfig.OcclusionFactor,
                        nameof(MasterConfig.OcclusionFactor),
                        SliderOcclusionFactor.value);
                UpdateValueLogged(
                        ref MasterConfig.PlayerOcclusionFactor,
                        nameof(MasterConfig.PlayerOcclusionFactor),
                        SliderPlayerOcclusionFactor.value);
                UpdateValueLogged(
                        ref MasterConfig.PlayerDirectionality,
                        nameof(MasterConfig.PlayerDirectionality),
                        SliderPlayerDirectionality.value);
                UpdateValueLogged(
                        ref MasterConfig.ListenerDirectionality,
                        nameof(MasterConfig.ListenerDirectionality),
                        SliderListenerDirectionality.value);
                UpdateValueLogged(
                        ref MasterConfig.VoiceDistanceNear,
                        nameof(MasterConfig.VoiceDistanceNear),
                        SliderVoiceDistanceNear.value);
                UpdateValueLogged(
                        ref MasterConfig.VoiceDistanceFar,
                        nameof(MasterConfig.VoiceDistanceFar),
                        SliderVoiceDistanceFar.value);
                UpdateValueLogged(
                        ref MasterConfig.VoiceGain,
                        nameof(MasterConfig.VoiceGain),
                        SliderVoiceGain.value);
                UpdateValueLogged(
                        ref MasterConfig.VoiceVolumetricRadius,
                        nameof(MasterConfig.VoiceVolumetricRadius),
                        SliderVoiceVolumetricRadius.value);
                UpdateValueLogged(
                        ref MasterConfig.EnableVoiceLowpass,
                        nameof(MasterConfig.EnableVoiceLowpass),
                        ToggleVoiceLowpass.isOn);
                UpdateValueLogged(
                        ref MasterConfig.AvatarNearRadius,
                        nameof(MasterConfig.AvatarNearRadius),
                        SliderAvatarDistanceNear.value);
                UpdateValueLogged(
                        ref MasterConfig.AvatarFarRadius,
                        nameof(MasterConfig.AvatarFarRadius),
                        SliderAvatarDistanceFar.value);
                UpdateValueLogged(
                        ref MasterConfig.AvatarGain,
                        nameof(MasterConfig.AvatarGain),
                        SliderAvatarGain.value);
                UpdateValueLogged(
                        ref MasterConfig.AvatarVolumetricRadius,
                        nameof(MasterConfig.AvatarVolumetricRadius),
                        SliderAvatarVolumetricRadius.value);
                UpdateValueLogged(
                        ref MasterConfig.ForceAvatarSpatialAudio,
                        nameof(MasterConfig.ForceAvatarSpatialAudio),
                        ToggleAvatarSpatialize.isOn);
                UpdateValueLogged(
                        ref MasterConfig.AllowAvatarCustomAudioCurves,
                        nameof(MasterConfig.AllowAvatarCustomAudioCurves),
                        ToggleAvatarCustomCurve.isOn);

                MasterConfig.Dirty = true;
                MasterConfig.NotifyIfDirty(1);
                MasterConfig.MarkNetworkDirty();
            }
        }

        private void UpdateTextAndInteractiveElements() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(UpdateTextAndInteractiveElements));
#endif
            #endregion

            TextOcclusionFactor.text = SliderOcclusionFactor.value.ToString(
                    "F",
                    CultureInfo.InvariantCulture);
            TextPlayerOcclusionFactor.text = SliderPlayerOcclusionFactor.value.ToString(
                    "F",
                    CultureInfo.InvariantCulture);
            TextPlayerDirectionality.text = SliderPlayerDirectionality.value.ToString(
                    "F",
                    CultureInfo.InvariantCulture);
            TextListenerDirectionality.text = SliderListenerDirectionality.value.ToString(
                    "F",
                    CultureInfo.InvariantCulture);
            TextVoiceDistanceNear.text = SliderVoiceDistanceNear.value.ToString(
                    "F1",
                    CultureInfo.InvariantCulture);
            TextVoiceDistanceFar.text = SliderVoiceDistanceFar.value.ToString(
                    "F1",
                    CultureInfo.InvariantCulture);
            TextVoiceGain.text = SliderVoiceGain.value.ToString(
                    "F1",
                    CultureInfo.InvariantCulture);
            TextVoiceVolumetricRadius.text = SliderVoiceVolumetricRadius.value.ToString(
                    "F1",
                    CultureInfo.InvariantCulture);
            AvatarDistanceNear.text = SliderAvatarDistanceNear.value.ToString(
                    "F1",
                    CultureInfo.InvariantCulture);
            AvatarDistanceFar.text = SliderAvatarDistanceFar.value.ToString(
                    "F1",
                    CultureInfo.InvariantCulture);
            AvatarGain.text = SliderAvatarGain.value.ToString(
                    "F1",
                    CultureInfo.InvariantCulture);
            AvatarVolumetricRadius.text = SliderAvatarVolumetricRadius.value.ToString(
                    "F1",
                    CultureInfo.InvariantCulture);

            bool isOwner = Networking.IsOwner(MasterConfig.gameObject);
            bool locallyControlled = !LocalConfig.AllowMasterControlLocalValues || isOwner;

            SliderOcclusionFactor.interactable = locallyControlled;
            SliderPlayerOcclusionFactor.interactable = locallyControlled;
            SliderListenerDirectionality.interactable = locallyControlled;
            SliderPlayerDirectionality.interactable = locallyControlled;
            SliderVoiceDistanceNear.interactable = locallyControlled;
            SliderVoiceDistanceFar.interactable = locallyControlled;
            SliderVoiceGain.interactable = locallyControlled;
            SliderVoiceVolumetricRadius.interactable = locallyControlled;
            ToggleVoiceLowpass.interactable = locallyControlled;
            SliderAvatarDistanceNear.interactable = locallyControlled;
            SliderAvatarDistanceFar.interactable = locallyControlled;
            SliderAvatarGain.interactable = locallyControlled;
            SliderAvatarVolumetricRadius.interactable = locallyControlled;
            ToggleAvatarSpatialize.interactable = locallyControlled;
            ToggleAvatarCustomCurve.interactable = locallyControlled;

            ToggleAllowMasterControl.interactable = !isOwner;
        }

        internal void ResetAll() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(ResetAll));
#endif
            #endregion

            _preventChangeEvent = true;

            LocalConfig.AllowMasterControlLocalValues = false;

            _displayedConfig = LocalConfig;
            CopyValuesFromTo(DefaultValues, LocalConfig);
            if (Networking.IsMaster) {
                CopyValuesFromTo(DefaultValues, MasterConfig);
            }

            _preventChangeEvent = false;
            UpdateUi();
        }

        private void CopyValuesFromTo(PlayerAudioConfigurationModel from, PlayerAudioConfigurationModel to) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Copy from {from.GetScriptPathInScene()} to {to.GetScriptPathInScene()}");
#endif
            #endregion

            if (!Utilities.IsValid(from)) {
                Error($"{nameof(PlayerAudioConfigurationModel)} '{nameof(from)}' invalid");
                return;
            }

            if (!Utilities.IsValid(to)) {
                Error($"{nameof(PlayerAudioConfigurationModel)} '{nameof(to)}' invalid");
                return;
            }

            UpdateValueLogged(
                    ref to.OcclusionMask,
                    nameof(to.OcclusionMask),
                    from.OcclusionMask);
            UpdateValueLogged(
                    ref to.OcclusionFactor,
                    nameof(to.OcclusionFactor),
                    from.OcclusionFactor);
            UpdateValueLogged(
                    ref to.PlayerOcclusionFactor,
                    nameof(to.PlayerOcclusionFactor),
                    from.PlayerOcclusionFactor);
            UpdateValueLogged(
                    ref to.ListenerDirectionality,
                    nameof(to.ListenerDirectionality),
                    from.ListenerDirectionality);
            UpdateValueLogged(
                    ref to.PlayerDirectionality,
                    nameof(to.PlayerDirectionality),
                    from.PlayerDirectionality);
            UpdateValueLogged(
                    ref to.EnableVoiceLowpass,
                    nameof(to.EnableVoiceLowpass),
                    from.EnableVoiceLowpass);
            UpdateValueLogged(
                    ref to.VoiceDistanceNear,
                    nameof(to.VoiceDistanceNear),
                    from.VoiceDistanceNear);
            UpdateValueLogged(
                    ref to.VoiceDistanceFar,
                    nameof(to.VoiceDistanceFar),
                    from.VoiceDistanceFar);
            UpdateValueLogged(
                    ref to.VoiceGain,
                    nameof(to.VoiceGain),
                    from.VoiceGain);
            UpdateValueLogged(
                    ref to.VoiceVolumetricRadius,
                    nameof(to.VoiceVolumetricRadius),
                    from.VoiceVolumetricRadius);
            UpdateValueLogged(
                    ref to.ForceAvatarSpatialAudio,
                    nameof(to.ForceAvatarSpatialAudio),
                    from.ForceAvatarSpatialAudio);
            UpdateValueLogged(
                    ref to.AllowAvatarCustomAudioCurves,
                    nameof(to.AllowAvatarCustomAudioCurves),
                    from.AllowAvatarCustomAudioCurves);
            UpdateValueLogged(
                    ref to.AvatarNearRadius,
                    nameof(to.AvatarNearRadius),
                    from.AvatarNearRadius);
            UpdateValueLogged(
                    ref to.AvatarFarRadius,
                    nameof(to.AvatarFarRadius),
                    from.AvatarFarRadius);
            UpdateValueLogged(
                    ref to.AvatarGain,
                    nameof(to.AvatarGain),
                    from.AvatarGain);
            UpdateValueLogged(
                    ref to.AvatarVolumetricRadius,
                    nameof(to.AvatarVolumetricRadius),
                    from.AvatarVolumetricRadius);

            to.Dirty = true;
            to.NotifyIfDirty(1);
            to.MarkNetworkDirty();
        }

        internal void UpdateSliderChecked(Slider slider, float value) {
            if (value < slider.minValue || value > slider.maxValue) {
                Error(
                        $"{value} is out of range [{slider.minValue}, {slider.maxValue}] " +
                        $"for slider {slider.GetComponentPathInScene()}");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Updating slider {slider.GetComponentPathInScene()} to {value}");
#endif
            #endregion

            slider.value = value;
        }

        internal static void UpdateValueLogged<T>(ref T old, string variable, T replacement) {
            #region TLP_DEBUG
#if TLP_DEBUG
            TlpLogger.StaticDebugLog($"Updating {variable} from {old} to {replacement}", null);
#endif
            #endregion

            old = replacement;
        }
        #endregion
    }
}