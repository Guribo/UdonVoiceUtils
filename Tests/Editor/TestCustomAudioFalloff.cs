using Guribo.UdonBetterAudio.Runtime.Examples;
using Guribo.UdonUtils.Tests.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Tests.Editor
{
    [TestFixture]
    public class TestCustomAudioFalloff
    {
        #region Start

        [Test]
        public void Start_DisablesComponentIfAudioInvalid()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();

            Assert.Null(customAudioFallOff.audioSource);

            LogAssert.Expect(LogType.Error, "CustomAudioFalloff: AudioSource has not been set");
            customAudioFallOff.Start();

            Assert.False(customAudioFallOff.enabled);
        }

        [Test]
        public void Start_EnabledIfAudioValid()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();

            LogAssert.NoUnexpectedReceived();
            customAudioFallOff.Start();

            Assert.NotNull(customAudioFallOff.audioSource);
            Assert.True(customAudioFallOff.enabled);
        }

        #endregion

        #region UpdateVolume

        [Test]
        public void UpdateVolume_SetsVolumeToCurveValue()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();
            customAudioFallOff.audioSource = customAudioFallOff.gameObject.AddComponent<AudioSource>();

            var ute = new UdonTestUtils.UdonTestEnvironment();
            var player = ute.CreatePlayer();

            player.gameObject.transform.position = Vector3.forward * 12.5f;

            customAudioFallOff.UpdateVolume(player);

            var headPosition = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            var distance = Vector3.Distance(customAudioFallOff.transform.position,headPosition);
            Assert.True(Mathf.Abs(customAudioFallOff.customFallOff.Evaluate(distance) - customAudioFallOff.audioSource.volume) < 0.001f);
        }

        [Test]
        public void UpdateVolume_DoesNothingWhenPlayerInvalid()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();
            customAudioFallOff.audioSource = customAudioFallOff.gameObject.AddComponent<AudioSource>();

            Assert.DoesNotThrow(() => customAudioFallOff.UpdateVolume(null));
        }
        
        #endregion

        #region PostLateUpdate

        [Test]
        public void PostLateUpdate_SetsVolumeToCurveValue()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();
            customAudioFallOff.audioSource = customAudioFallOff.gameObject.AddComponent<AudioSource>();

            var ute = new UdonTestUtils.UdonTestEnvironment();
            var player = ute.CreatePlayer();
            
            Assert.DoesNotThrow(() => customAudioFallOff.PostLateUpdate());
        }
        
        [Test]
        public void PostLateUpdate_DoesNothingWhenPlayerInvalid()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();
            customAudioFallOff.audioSource = customAudioFallOff.gameObject.AddComponent<AudioSource>();

            Assert.DoesNotThrow(() => customAudioFallOff.PostLateUpdate());
        }
        
        
        #endregion
    }
}