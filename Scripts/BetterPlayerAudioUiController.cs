using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterPlayerAudioUiController : UdonSharpBehaviour
    {
        [SerializeField] private BetterPlayerAudio betterPlayerAudio;

        [Header("General Settings")] [SerializeField]
        private Slider sliderOcclusionFactor;

        [SerializeField] private Slider sliderListenerDirectionality;
        [SerializeField] private Slider sliderPlayerDirectionality;

        [SerializeField] private Text textListenerDirectionality;
        [SerializeField] private Text textPlayerDirectionality;
        [SerializeField] private Text textOcclusionFactor;

        [Header("Voice Settings")] [SerializeField]
        private Slider sliderVoiceDistanceNear;

        [SerializeField] private Slider sliderVoiceDistanceFar;
        [SerializeField] private Slider sliderVoiceGain;
        [SerializeField] private Slider sliderVoiceVolumetricRadius;

        [SerializeField] private Text textVoiceDistanceNear;
        [SerializeField] private Text textVoiceDistanceFar;
        [SerializeField] private Text textVoiceGain;
        [SerializeField] private Text textVoiceVolumetricRadius;

        [SerializeField] private Toggle toggleVoiceLowpass;

        [Header("Avatar Settings")] [SerializeField]
        private Slider sliderAvatarDistanceNear;

        [SerializeField] private Slider sliderAvatarDistanceFar;
        [SerializeField] private Slider sliderAvatarGain;
        [SerializeField] private Slider sliderAvatarVolumetricRadius;

        [SerializeField] private Text texAvatarDistanceNear;
        [SerializeField] private Text texAvatarDistanceFar;
        [SerializeField] private Text texAvatarGain;
        [SerializeField] private Text texAvatarVolumetricRadius;

        [SerializeField] private Toggle toggleAvatarSpatialize;
        [SerializeField] private Toggle toggleAvatarCustomCurve;


        void Start()
        {
            if (!betterPlayerAudio)
            {
                Debug.LogError("Invalid betterPlayerAudio");
            }

            // make sure the betterPlayerAudio component is initialized, to prevent accidentally setting everything to 0
            betterPlayerAudio.Initialize();
            ResetAll();
        }

        public void OnSettingsChanged()
        {
            betterPlayerAudio.OcclusionFactor = sliderOcclusionFactor.value;
            betterPlayerAudio.PlayerDirectionality = sliderPlayerDirectionality.value;
            betterPlayerAudio.ListenerDirectionality = sliderListenerDirectionality.value;

            betterPlayerAudio.TargetVoiceDistanceNear = sliderVoiceDistanceNear.value;
            betterPlayerAudio.TargetVoiceDistanceFar = sliderVoiceDistanceFar.value;
            betterPlayerAudio.TargetVoiceGain = sliderVoiceGain.value;
            betterPlayerAudio.TargetVoiceVolumetricRadius = sliderVoiceVolumetricRadius.value;
            betterPlayerAudio.EnableVoiceLowpass = toggleVoiceLowpass.isOn;

            betterPlayerAudio.TargetAvatarNearRadius = sliderAvatarDistanceNear.value;
            betterPlayerAudio.TargetAvatarFarRadius = sliderAvatarDistanceFar.value;
            betterPlayerAudio.TargetAvatarGain = sliderAvatarGain.value;
            betterPlayerAudio.TargetAvatarVolumetricRadius = sliderAvatarVolumetricRadius.value;
            betterPlayerAudio.ForceAvatarSpatialAudio = toggleAvatarSpatialize.isOn;
            betterPlayerAudio.AllowAvatarCustomAudioCurves = toggleAvatarCustomCurve.isOn;

            textOcclusionFactor.text = sliderOcclusionFactor.value.ToString("F");
            textPlayerDirectionality.text = sliderPlayerDirectionality.value.ToString("F");
            textListenerDirectionality.text = sliderListenerDirectionality.value.ToString("F");

            textVoiceDistanceNear.text = sliderVoiceDistanceNear.value.ToString("F1");
            textVoiceDistanceFar.text = sliderVoiceDistanceFar.value.ToString("F1");
            textVoiceGain.text = sliderVoiceGain.value.ToString("F1");
            textVoiceVolumetricRadius.text = sliderVoiceVolumetricRadius.value.ToString("F1");

            texAvatarDistanceNear.text = sliderAvatarDistanceNear.value.ToString("F1");
            texAvatarDistanceFar.text = sliderAvatarDistanceFar.value.ToString("F1");
            texAvatarGain.text = sliderAvatarGain.value.ToString("F1");
            texAvatarVolumetricRadius.text = sliderAvatarVolumetricRadius.value.ToString("F1");
        }

        public void ResetAll()
        {
            // Resetting the sliders/toggles will cause the betterPlayerAudio script to be reset automatically
            // due to the change events being triggered, so there is no need to call betterPlayerAudio.Reset()

            sliderOcclusionFactor.value = betterPlayerAudio.defaultOcclusionFactor;
            sliderListenerDirectionality.value = betterPlayerAudio.defaultListenerDirectionality;
            sliderPlayerDirectionality.value = betterPlayerAudio.defaultPlayerDirectionality;

            sliderVoiceDistanceNear.value = betterPlayerAudio.defaultVoiceDistanceNear;
            sliderVoiceDistanceFar.value = betterPlayerAudio.defaultVoiceDistanceFar;
            sliderVoiceGain.value = betterPlayerAudio.defaultVoiceGain;
            sliderVoiceVolumetricRadius.value = betterPlayerAudio.defaultVoiceVolumetricRadius;

            toggleVoiceLowpass.isOn = betterPlayerAudio.defaultEnableVoiceLowpass;

            sliderAvatarDistanceNear.value = betterPlayerAudio.defaultAvatarNearRadius;
            sliderAvatarDistanceFar.value = betterPlayerAudio.defaultAvatarFarRadius;
            sliderAvatarGain.value = betterPlayerAudio.defaultAvatarGain;
            sliderAvatarVolumetricRadius.value = betterPlayerAudio.defaultAvatarVolumetricRadius;

            toggleAvatarSpatialize.isOn = betterPlayerAudio.defaultForceAvatarSpatialAudio;
            toggleAvatarCustomCurve.isOn = betterPlayerAudio.defaultAllowAvatarCustomAudioCurves;
        }
    }
}