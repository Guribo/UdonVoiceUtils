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

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio
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
        public void _UpdateUi() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_UpdateUi));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!IsViewInitialized) {
                _Error($"Not initialized, did you reference it correctly on the {nameof(PlayerAudioController)}?");
                return;
            }

            var owner = Networking.GetOwner(_playerAudioController.gameObject);
            if (Utilities.IsValid(owner)) {
                TextAllowMasterControl.text = $"Let {owner.displayName} (owner) control everything";
            }

            _preventChangeEvent = true;

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"Applying values from {_displayedConfig._GetScriptPathInScene()} to UI");
#endif
            #endregion

            _UpdateSliderChecked(SliderOcclusionFactor, _displayedConfig.OcclusionFactor);
            _UpdateSliderChecked(SliderPlayerOcclusionFactor, _displayedConfig.PlayerOcclusionFactor);
            _UpdateSliderChecked(SliderListenerDirectionality, _displayedConfig.ListenerDirectionality);
            _UpdateSliderChecked(SliderPlayerDirectionality, _displayedConfig.PlayerDirectionality);
            _UpdateSliderChecked(SliderVoiceDistanceNear, _displayedConfig.VoiceDistanceNear);
            _UpdateSliderChecked(SliderVoiceDistanceFar, _displayedConfig.VoiceDistanceFar);
            _UpdateSliderChecked(SliderVoiceGain, _displayedConfig.VoiceGain);
            _UpdateSliderChecked(SliderVoiceVolumetricRadius, _displayedConfig.VoiceVolumetricRadius);
            _UpdateSliderChecked(SliderAvatarDistanceNear, _displayedConfig.AvatarNearRadius);
            _UpdateSliderChecked(SliderAvatarDistanceFar, _displayedConfig.AvatarFarRadius);
            _UpdateSliderChecked(SliderAvatarGain, _displayedConfig.AvatarGain);
            _UpdateSliderChecked(SliderAvatarVolumetricRadius, _displayedConfig.AvatarVolumetricRadius);

            ToggleVoiceLowpass.isOn = _displayedConfig.EnableVoiceLowpass;
            ToggleAvatarSpatialize.isOn = _displayedConfig.ForceAvatarSpatialAudio;
            ToggleAvatarCustomCurve.isOn = _displayedConfig.AllowAvatarCustomAudioCurves;
            ToggleAllowMasterControl.isOn = LocalConfig.AllowMasterControlLocalValues;

            _preventChangeEvent = false;

            _UpdateTextAndInteractiveElements();
        }
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_IsSet(UserChangedSettings, nameof(UserChangedSettings))) {
                return false;
            }

            if (!_IsSet(ResetToDefault, nameof(ResetToDefault))) {
                return false;
            }

            if (!UserChangedSettings._AddListenerVerified(this, nameof(_OnSettingsChanged), true)) {
                return false;
            }

            if (!ResetToDefault._AddListenerVerified(this, nameof(_ResetAll), true)) {
                return false;
            }

            return true;
        }

        public override void _OnEvent(string eventName) {
            if (!HasStartedOk) {
                return;
            }

            switch (eventName) {
                case nameof(_OnSettingsChanged):
                    _OnSettingsChanged();
                    break;
                case nameof(_ResetAll):
                    _ResetAll();
                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }

        #region MVC
        public override void _OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnModelChanged));
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

            _UpdateUi();
        }

        protected override bool _InitializeInternal() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_InitializeInternal));
#endif
            #endregion

            if (!Utilities.IsValid(DefaultValues)) {
                _ErrorAndDisableComponent($"{nameof(DefaultValues)} not set");
                return false;
            }

            if (!Utilities.IsValid(LocalConfig)) {
                _ErrorAndDisableComponent($"{nameof(LocalConfig)} not set");
                return false;
            }

            if (!Utilities.IsValid(MasterConfig)) {
                _ErrorAndDisableComponent($"{nameof(MasterConfig)} not set");
                return false;
            }

            if (!LocalConfig.ChangeEvent._AddListenerVerified(this, nameof(_OnModelChanged))) {
                _ErrorAndDisableComponent($"Failed to add listener to {nameof(LocalConfig)}");
                return false;
            }

            if (!MasterConfig.ChangeEvent._AddListenerVerified(this, nameof(_OnModelChanged))) {
                _ErrorAndDisableComponent($"Failed to add listener to {nameof(MasterConfig)}");
                return false;
            }

            if (!Utilities.IsValid(Controller)) {
                _ErrorAndDisableComponent($"{nameof(Controller)} invalid");
                return false;
            }

            _playerAudioController = (PlayerAudioController)Controller;
            bool masterControls = LocalConfig.AllowMasterControlLocalValues && !Networking.IsMaster;
            _displayedConfig = masterControls ? MasterConfig : LocalConfig;
            return true;
        }

        protected override bool _DeInitializeInternal() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_DeInitializeInternal));
#endif
            #endregion

            if (!Utilities.IsValid(LocalConfig)) {
                _Warn($"{nameof(LocalConfig)} was destroyed");
                return true; // OK in case of de-init
            }

            if (!Utilities.IsValid(LocalConfig.ChangeEvent)) {
                _Warn($"{nameof(LocalConfig.ChangeEvent)} was destroyed");
                return true; // OK in case of de-init
            }

            if (!Utilities.IsValid(MasterConfig)) {
                _Warn($"{nameof(MasterConfig)} was destroyed");
                return true; // OK in case of de-init
            }

            if (!Utilities.IsValid(MasterConfig.ChangeEvent)) {
                _Warn($"{nameof(MasterConfig.ChangeEvent)} was destroyed");
                return true; // OK in case of de-init
            }

            bool successLocal = LocalConfig.ChangeEvent._RemoveListener(this, true);
            bool successMaster = MasterConfig.ChangeEvent._RemoveListener(this, true);

            if (!successLocal) {
                _Error($"Failed to remove listener from {nameof(LocalConfig)}");
            }

            if (!successMaster) {
                _Error($"Failed to remove listener from {nameof(MasterConfig)}");
            }

            return successLocal && successMaster;
        }
        #endregion
        #endregion


        #region Internal
        internal void _OnSettingsChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnSettingsChanged));
#endif
            #endregion

            if (!IsViewInitialized) {
                _Warn($"Skipping {nameof(_OnSettingsChanged)} event as {nameof(PlayerAudioView)} is not ready");
                return;
            }

            if (_preventChangeEvent) {
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"Updating {LocalConfig._GetScriptPathInScene()} from UI");
#endif
            #endregion

            _UpdateValueLogged(
                    ref LocalConfig.OcclusionFactor,
                    nameof(LocalConfig.OcclusionFactor),
                    SliderOcclusionFactor.value);
            _UpdateValueLogged(
                    ref LocalConfig.PlayerOcclusionFactor,
                    nameof(LocalConfig.PlayerOcclusionFactor),
                    SliderPlayerOcclusionFactor.value);
            _UpdateValueLogged(
                    ref LocalConfig.PlayerDirectionality,
                    nameof(LocalConfig.PlayerDirectionality),
                    SliderPlayerDirectionality.value);
            _UpdateValueLogged(
                    ref LocalConfig.ListenerDirectionality,
                    nameof(LocalConfig.ListenerDirectionality),
                    SliderListenerDirectionality.value);
            _UpdateValueLogged(
                    ref LocalConfig.VoiceDistanceNear,
                    nameof(LocalConfig.VoiceDistanceNear),
                    SliderVoiceDistanceNear.value);
            _UpdateValueLogged(
                    ref LocalConfig.VoiceDistanceFar,
                    nameof(LocalConfig.VoiceDistanceFar),
                    SliderVoiceDistanceFar.value);
            _UpdateValueLogged(
                    ref LocalConfig.VoiceGain,
                    nameof(LocalConfig.VoiceGain),
                    SliderVoiceGain.value);
            _UpdateValueLogged(
                    ref LocalConfig.VoiceVolumetricRadius,
                    nameof(LocalConfig.VoiceVolumetricRadius),
                    SliderVoiceVolumetricRadius.value);
            _UpdateValueLogged(
                    ref LocalConfig.EnableVoiceLowpass,
                    nameof(LocalConfig.EnableVoiceLowpass),
                    ToggleVoiceLowpass.isOn);
            _UpdateValueLogged(
                    ref LocalConfig.AvatarNearRadius,
                    nameof(LocalConfig.AvatarNearRadius),
                    SliderAvatarDistanceNear.value);
            _UpdateValueLogged(
                    ref LocalConfig.AvatarFarRadius,
                    nameof(LocalConfig.AvatarFarRadius),
                    SliderAvatarDistanceFar.value);
            _UpdateValueLogged(
                    ref LocalConfig.AvatarGain,
                    nameof(LocalConfig.AvatarGain),
                    SliderAvatarGain.value);
            _UpdateValueLogged(
                    ref LocalConfig.AvatarVolumetricRadius,
                    nameof(LocalConfig.AvatarVolumetricRadius),
                    SliderAvatarVolumetricRadius.value);
            _UpdateValueLogged(
                    ref LocalConfig.ForceAvatarSpatialAudio,
                    nameof(LocalConfig.ForceAvatarSpatialAudio),
                    ToggleAvatarSpatialize.isOn);
            _UpdateValueLogged(
                    ref LocalConfig.AllowAvatarCustomAudioCurves,
                    nameof(LocalConfig.AllowAvatarCustomAudioCurves),
                    ToggleAvatarCustomCurve.isOn);
            _UpdateValueLogged(
                    ref LocalConfig.AllowMasterControlLocalValues,
                    nameof(LocalConfig.AllowMasterControlLocalValues),
                    ToggleAllowMasterControl.isOn);

            LocalConfig.Dirty = true;
            LocalConfig._NotifyIfDirty(1);

            if (Networking.IsMaster) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"Updating Master config values from UI");
#endif
                #endregion

                _UpdateValueLogged(
                        ref MasterConfig.OcclusionFactor,
                        nameof(MasterConfig.OcclusionFactor),
                        SliderOcclusionFactor.value);
                _UpdateValueLogged(
                        ref MasterConfig.PlayerOcclusionFactor,
                        nameof(MasterConfig.PlayerOcclusionFactor),
                        SliderPlayerOcclusionFactor.value);
                _UpdateValueLogged(
                        ref MasterConfig.PlayerDirectionality,
                        nameof(MasterConfig.PlayerDirectionality),
                        SliderPlayerDirectionality.value);
                _UpdateValueLogged(
                        ref MasterConfig.ListenerDirectionality,
                        nameof(MasterConfig.ListenerDirectionality),
                        SliderListenerDirectionality.value);
                _UpdateValueLogged(
                        ref MasterConfig.VoiceDistanceNear,
                        nameof(MasterConfig.VoiceDistanceNear),
                        SliderVoiceDistanceNear.value);
                _UpdateValueLogged(
                        ref MasterConfig.VoiceDistanceFar,
                        nameof(MasterConfig.VoiceDistanceFar),
                        SliderVoiceDistanceFar.value);
                _UpdateValueLogged(
                        ref MasterConfig.VoiceGain,
                        nameof(MasterConfig.VoiceGain),
                        SliderVoiceGain.value);
                _UpdateValueLogged(
                        ref MasterConfig.VoiceVolumetricRadius,
                        nameof(MasterConfig.VoiceVolumetricRadius),
                        SliderVoiceVolumetricRadius.value);
                _UpdateValueLogged(
                        ref MasterConfig.EnableVoiceLowpass,
                        nameof(MasterConfig.EnableVoiceLowpass),
                        ToggleVoiceLowpass.isOn);
                _UpdateValueLogged(
                        ref MasterConfig.AvatarNearRadius,
                        nameof(MasterConfig.AvatarNearRadius),
                        SliderAvatarDistanceNear.value);
                _UpdateValueLogged(
                        ref MasterConfig.AvatarFarRadius,
                        nameof(MasterConfig.AvatarFarRadius),
                        SliderAvatarDistanceFar.value);
                _UpdateValueLogged(
                        ref MasterConfig.AvatarGain,
                        nameof(MasterConfig.AvatarGain),
                        SliderAvatarGain.value);
                _UpdateValueLogged(
                        ref MasterConfig.AvatarVolumetricRadius,
                        nameof(MasterConfig.AvatarVolumetricRadius),
                        SliderAvatarVolumetricRadius.value);
                _UpdateValueLogged(
                        ref MasterConfig.ForceAvatarSpatialAudio,
                        nameof(MasterConfig.ForceAvatarSpatialAudio),
                        ToggleAvatarSpatialize.isOn);
                _UpdateValueLogged(
                        ref MasterConfig.AllowAvatarCustomAudioCurves,
                        nameof(MasterConfig.AllowAvatarCustomAudioCurves),
                        ToggleAvatarCustomCurve.isOn);

                MasterConfig.Dirty = true;
                MasterConfig._NotifyIfDirty(1);
                MasterConfig._MarkNetworkDirty();
            }
        }

        private void _UpdateTextAndInteractiveElements() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_UpdateTextAndInteractiveElements));
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

        internal void _ResetAll() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_ResetAll));
#endif
            #endregion

            _preventChangeEvent = true;

            LocalConfig.AllowMasterControlLocalValues = false;

            _displayedConfig = LocalConfig;
            _CopyValuesFromTo(DefaultValues, LocalConfig);
            if (Networking.IsMaster) {
                _CopyValuesFromTo(DefaultValues, MasterConfig);
            }

            _preventChangeEvent = false;
            _UpdateUi();
        }

        private void _CopyValuesFromTo(PlayerAudioConfigurationModel from, PlayerAudioConfigurationModel to) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"Copy from {from._GetScriptPathInScene()} to {to._GetScriptPathInScene()}");
#endif
            #endregion

            if (!Utilities.IsValid(from)) {
                _Error($"{nameof(PlayerAudioConfigurationModel)} '{nameof(from)}' invalid");
                return;
            }

            if (!Utilities.IsValid(to)) {
                _Error($"{nameof(PlayerAudioConfigurationModel)} '{nameof(to)}' invalid");
                return;
            }

            _UpdateValueLogged(
                    ref to.OcclusionMask,
                    nameof(to.OcclusionMask),
                    from.OcclusionMask);
            _UpdateValueLogged(
                    ref to.OcclusionFactor,
                    nameof(to.OcclusionFactor),
                    from.OcclusionFactor);
            _UpdateValueLogged(
                    ref to.PlayerOcclusionFactor,
                    nameof(to.PlayerOcclusionFactor),
                    from.PlayerOcclusionFactor);
            _UpdateValueLogged(
                    ref to.ListenerDirectionality,
                    nameof(to.ListenerDirectionality),
                    from.ListenerDirectionality);
            _UpdateValueLogged(
                    ref to.PlayerDirectionality,
                    nameof(to.PlayerDirectionality),
                    from.PlayerDirectionality);
            _UpdateValueLogged(
                    ref to.EnableVoiceLowpass,
                    nameof(to.EnableVoiceLowpass),
                    from.EnableVoiceLowpass);
            _UpdateValueLogged(
                    ref to.VoiceDistanceNear,
                    nameof(to.VoiceDistanceNear),
                    from.VoiceDistanceNear);
            _UpdateValueLogged(
                    ref to.VoiceDistanceFar,
                    nameof(to.VoiceDistanceFar),
                    from.VoiceDistanceFar);
            _UpdateValueLogged(
                    ref to.VoiceGain,
                    nameof(to.VoiceGain),
                    from.VoiceGain);
            _UpdateValueLogged(
                    ref to.VoiceVolumetricRadius,
                    nameof(to.VoiceVolumetricRadius),
                    from.VoiceVolumetricRadius);
            _UpdateValueLogged(
                    ref to.ForceAvatarSpatialAudio,
                    nameof(to.ForceAvatarSpatialAudio),
                    from.ForceAvatarSpatialAudio);
            _UpdateValueLogged(
                    ref to.AllowAvatarCustomAudioCurves,
                    nameof(to.AllowAvatarCustomAudioCurves),
                    from.AllowAvatarCustomAudioCurves);
            _UpdateValueLogged(
                    ref to.AvatarNearRadius,
                    nameof(to.AvatarNearRadius),
                    from.AvatarNearRadius);
            _UpdateValueLogged(
                    ref to.AvatarFarRadius,
                    nameof(to.AvatarFarRadius),
                    from.AvatarFarRadius);
            _UpdateValueLogged(
                    ref to.AvatarGain,
                    nameof(to.AvatarGain),
                    from.AvatarGain);
            _UpdateValueLogged(
                    ref to.AvatarVolumetricRadius,
                    nameof(to.AvatarVolumetricRadius),
                    from.AvatarVolumetricRadius);

            to.Dirty = true;
            to._NotifyIfDirty(1);
            to._MarkNetworkDirty();
        }

        internal void _UpdateSliderChecked(Slider slider, float value) {
            if (value < slider.minValue || value > slider.maxValue) {
                _Error(
                        $"{value} is out of range [{slider.minValue}, {slider.maxValue}] " +
                        $"for slider {slider._GetComponentPathInScene()}");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"Updating slider {slider._GetComponentPathInScene()} to {value}");
#endif
            #endregion

            slider.value = value;
        }

        internal static void _UpdateValueLogged<T>(ref T old, string variable, T replacement) {
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