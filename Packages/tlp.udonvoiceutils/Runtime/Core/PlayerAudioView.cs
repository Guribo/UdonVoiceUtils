using System.Globalization;
using JetBrains.Annotations;
using TLP.UdonUtils.DesignPatterns.MVC;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerAudioView : View
    {
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
        #endregion

        #region State
        private RectTransform[] _tabs;

        private bool _preventChangeEvent;
        private PlayerAudioConfigurationModel _displayedConfig;

        [SerializeField]
        internal PlayerAudioConfigurationModel MasterConfig;

        [SerializeField]
        internal PlayerAudioConfigurationModel LocalConfig;

        [SerializeField]
        internal PlayerAudioConfigurationModel DefaultValues;


        private PlayerAudioController _playerAudioController;
        #endregion

        /// <summary>
        /// Used by the Unity Ui elements
        /// </summary>
        [PublicAPI]
        public void OnSettingsChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnSettingsChanged));
#endif
            #endregion

            if (!Initialized) {
                Warn(
                        $"Skipping {nameof(OnSettingsChanged)} event as {nameof(PlayerAudioView)} is not ready"
                );
                return;
            }

            if (_preventChangeEvent) {
                return;
            }

            LocalConfig.OcclusionFactor = SliderOcclusionFactor.value;
            LocalConfig.PlayerOcclusionFactor = SliderPlayerOcclusionFactor.value;
            LocalConfig.PlayerDirectionality = SliderPlayerDirectionality.value;
            LocalConfig.ListenerDirectionality = SliderListenerDirectionality.value;

            LocalConfig.VoiceDistanceNear = SliderVoiceDistanceNear.value;
            LocalConfig.VoiceDistanceFar = SliderVoiceDistanceFar.value;
            LocalConfig.VoiceGain = SliderVoiceGain.value;
            LocalConfig.VoiceVolumetricRadius = SliderVoiceVolumetricRadius.value;
            LocalConfig.EnableVoiceLowpass = ToggleVoiceLowpass.isOn;

            LocalConfig.AvatarNearRadius = SliderAvatarDistanceNear.value;
            LocalConfig.AvatarFarRadius = SliderAvatarDistanceFar.value;
            LocalConfig.AvatarGain = SliderAvatarGain.value;
            LocalConfig.AvatarVolumetricRadius = SliderAvatarVolumetricRadius.value;
            LocalConfig.ForceAvatarSpatialAudio = ToggleAvatarSpatialize.isOn;
            LocalConfig.AllowAvatarCustomAudioCurves = ToggleAvatarCustomCurve.isOn;
            LocalConfig.AllowMasterControlLocalValues = ToggleAllowMasterControl.isOn;
            LocalConfig.Dirty = true;
            LocalConfig.NotifyIfDirty(1);

            if (Networking.IsMaster) {
                MasterConfig.OcclusionFactor = SliderOcclusionFactor.value;
                MasterConfig.PlayerOcclusionFactor = SliderPlayerOcclusionFactor.value;
                MasterConfig.PlayerDirectionality = SliderPlayerDirectionality.value;
                MasterConfig.ListenerDirectionality = SliderListenerDirectionality.value;

                MasterConfig.VoiceDistanceNear = SliderVoiceDistanceNear.value;
                MasterConfig.VoiceDistanceFar = SliderVoiceDistanceFar.value;
                MasterConfig.VoiceGain = SliderVoiceGain.value;
                MasterConfig.VoiceVolumetricRadius = SliderVoiceVolumetricRadius.value;
                MasterConfig.EnableVoiceLowpass = ToggleVoiceLowpass.isOn;

                MasterConfig.AvatarNearRadius = SliderAvatarDistanceNear.value;
                MasterConfig.AvatarFarRadius = SliderAvatarDistanceFar.value;
                MasterConfig.AvatarGain = SliderAvatarGain.value;
                MasterConfig.AvatarVolumetricRadius = SliderAvatarVolumetricRadius.value;
                MasterConfig.ForceAvatarSpatialAudio = ToggleAvatarSpatialize.isOn;
                MasterConfig.AllowAvatarCustomAudioCurves = ToggleAvatarCustomCurve.isOn;
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

            TextOcclusionFactor.text = SliderOcclusionFactor.value.ToString("F", CultureInfo.InvariantCulture);
            TextPlayerOcclusionFactor.text =
                    SliderPlayerOcclusionFactor.value.ToString("F", CultureInfo.InvariantCulture);
            TextPlayerDirectionality.text =
                    SliderPlayerDirectionality.value.ToString("F", CultureInfo.InvariantCulture);
            TextListenerDirectionality.text =
                    SliderListenerDirectionality.value.ToString("F", CultureInfo.InvariantCulture);

            TextVoiceDistanceNear.text = SliderVoiceDistanceNear.value.ToString("F1", CultureInfo.InvariantCulture);
            TextVoiceDistanceFar.text = SliderVoiceDistanceFar.value.ToString("F1", CultureInfo.InvariantCulture);
            TextVoiceGain.text = SliderVoiceGain.value.ToString("F1", CultureInfo.InvariantCulture);
            TextVoiceVolumetricRadius.text =
                    SliderVoiceVolumetricRadius.value.ToString("F1", CultureInfo.InvariantCulture);

            AvatarDistanceNear.text = SliderAvatarDistanceNear.value.ToString("F1", CultureInfo.InvariantCulture);
            AvatarDistanceFar.text = SliderAvatarDistanceFar.value.ToString("F1", CultureInfo.InvariantCulture);
            AvatarGain.text = SliderAvatarGain.value.ToString("F1", CultureInfo.InvariantCulture);
            AvatarVolumetricRadius.text =
                    SliderAvatarVolumetricRadius.value.ToString("F1", CultureInfo.InvariantCulture);

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

        /// <summary>
        /// Used by the Unity Ui elements
        /// </summary>
        public void ResetAll() {
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
        }

        /// <summary>
        /// Callback function, can be called by the BetterPlayerAudio to update the UI
        /// </summary>
        public void UpdateUi() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(UpdateUi));
#endif
            #endregion

            var owner = Networking.GetOwner(_playerAudioController.gameObject);
            if (Utilities.IsValid(owner)) {
                TextAllowMasterControl.text = $"Let {owner.displayName} (owner) control everything";
            }

            _preventChangeEvent = true;

            SliderOcclusionFactor.value = _displayedConfig.OcclusionFactor;
            SliderPlayerOcclusionFactor.value = _displayedConfig.PlayerOcclusionFactor;
            SliderListenerDirectionality.value = _displayedConfig.ListenerDirectionality;
            SliderPlayerDirectionality.value = _displayedConfig.PlayerDirectionality;

            SliderVoiceDistanceNear.value = _displayedConfig.VoiceDistanceNear;
            SliderVoiceDistanceFar.value = _displayedConfig.VoiceDistanceFar;
            SliderVoiceGain.value = _displayedConfig.VoiceGain;
            SliderVoiceVolumetricRadius.value = _displayedConfig.VoiceVolumetricRadius;

            ToggleVoiceLowpass.isOn = _displayedConfig.EnableVoiceLowpass;

            SliderAvatarDistanceNear.value = _displayedConfig.AvatarNearRadius;
            SliderAvatarDistanceFar.value = _displayedConfig.AvatarFarRadius;
            SliderAvatarGain.value = _displayedConfig.AvatarGain;
            SliderAvatarVolumetricRadius.value = _displayedConfig.AvatarVolumetricRadius;

            ToggleAvatarSpatialize.isOn = _displayedConfig.ForceAvatarSpatialAudio;
            ToggleAvatarCustomCurve.isOn = _displayedConfig.AllowAvatarCustomAudioCurves;

            ToggleAllowMasterControl.isOn = LocalConfig.AllowMasterControlLocalValues;

            _preventChangeEvent = false;

            UpdateTextAndInteractiveElements();
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
            ResetAll();
            return true;
        }

        public override void OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnModelChanged));
#endif
            #endregion

            if (Utilities.IsValid(EventInstigator) && Networking.IsMaster &&
                ReferenceEquals(EventInstigator, MasterConfig)) {
                // nothing to do
                return;
            }

            bool masterControls = LocalConfig.AllowMasterControlLocalValues && !Networking.IsMaster;

            _displayedConfig = masterControls ? MasterConfig : LocalConfig;

            UpdateUi();
        }

        #region Internal
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


        private void CopyValuesFromTo(PlayerAudioConfigurationModel from, PlayerAudioConfigurationModel to) {
            if (!Utilities.IsValid(from)) {
                Error($"{nameof(PlayerAudioConfigurationModel)} '{nameof(from)}' invalid");
                return;
            }

            if (!Utilities.IsValid(to)) {
                Error($"{nameof(PlayerAudioConfigurationModel)} '{nameof(to)}' invalid");
                return;
            }

            to.OcclusionMask = from.OcclusionMask;
            to.OcclusionFactor = from.OcclusionFactor;
            to.PlayerOcclusionFactor = from.PlayerOcclusionFactor;
            to.ListenerDirectionality = from.ListenerDirectionality;
            to.PlayerDirectionality = from.PlayerDirectionality;
            to.EnableVoiceLowpass = from.EnableVoiceLowpass;
            to.VoiceDistanceNear = from.VoiceDistanceNear;
            to.VoiceDistanceFar = from.VoiceDistanceFar;
            to.VoiceGain = from.VoiceGain;
            to.VoiceVolumetricRadius = from.VoiceVolumetricRadius;
            to.ForceAvatarSpatialAudio = from.ForceAvatarSpatialAudio;
            to.AllowAvatarCustomAudioCurves = from.AllowAvatarCustomAudioCurves;
            to.AvatarNearRadius = from.AvatarNearRadius;
            to.AvatarFarRadius = from.AvatarFarRadius;
            to.AvatarGain = from.AvatarGain;
            to.AvatarVolumetricRadius = from.AvatarVolumetricRadius;

            to.Dirty = true;
            to.NotifyIfDirty(1);
            to.MarkNetworkDirty();
        }
        #endregion
    }
}