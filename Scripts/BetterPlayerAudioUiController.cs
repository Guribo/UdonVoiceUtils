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

            ResetAll();
        }

        public void OnEnable()
        {
            ResetAll();
        }

        public void OnDisable()
        {
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
            sliderOcclusionFactor.value = 0.5f;
            sliderListenerDirectionality.value = 0.5f;
            sliderPlayerDirectionality.value = 0.75f;

            sliderVoiceDistanceNear.value = 1f;
            sliderVoiceDistanceFar.value = 250f;
            sliderVoiceGain.value = 0f;
            sliderVoiceVolumetricRadius.value = 0f;

            toggleVoiceLowpass.isOn = true;

            sliderAvatarDistanceNear.value = 0f;
            sliderAvatarDistanceFar.value = 25f;
            sliderAvatarGain.value = 10f;
            sliderAvatarVolumetricRadius.value = 0f;

            toggleAvatarSpatialize.isOn = true;
            toggleAvatarCustomCurve.isOn = false;
        }
    }
}