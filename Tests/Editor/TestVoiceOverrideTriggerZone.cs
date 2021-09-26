using Guribo.UdonBetterAudio.Runtime;
using Guribo.UdonBetterAudio.Runtime.Examples;
using Guribo.UdonUtils.Runtime.Common;
using Guribo.UdonUtils.Tests.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Tests.Editor
{
    [TestFixture]
    public class TestVoiceOverrideTriggerZone
    {
        private GameObject _betterPlayerAudioGameObject;
        private GameObject _betterPlayerAudioOverrideGameObject;
        private PlayerList _playerList;
        private VRCPlayerApi _localPlayer;
        private VRCPlayerApi _player1;
        private VRCPlayerApi _player2;
        private VRCPlayerApi _player3;
        private BetterPlayerAudio _betterPlayerAudio;
        private BetterPlayerAudioOverride _voiceOverride;
        private UdonTestUtils.UdonTestEnvironment _udonTestEnvironment;
        
        [SetUp]
        public void Prepare()
        {
            _betterPlayerAudioGameObject = new GameObject();
            _betterPlayerAudioOverrideGameObject = new GameObject();

            _betterPlayerAudio = TestBetterPlayerAudio.CreateBetterPlayerAudio(_betterPlayerAudioGameObject);
            _voiceOverride = TestBetterPlayerAudio.CreateBetterPlayerVoiceOverride(_betterPlayerAudioOverrideGameObject, _betterPlayerAudio);

            _voiceOverride.udonDebug = _betterPlayerAudioGameObject.AddComponent<UdonDebug>();
            _voiceOverride.playerList = _betterPlayerAudioGameObject.AddComponent<PlayerList>();
            _voiceOverride.playerList.udonDebug = _voiceOverride.udonDebug;
            
            _udonTestEnvironment = new UdonTestUtils.UdonTestEnvironment();
            _localPlayer = _udonTestEnvironment.CreatePlayer();
            _player1 = _udonTestEnvironment.CreatePlayer();
            _player2 = _udonTestEnvironment.CreatePlayer();
            _player3 = _udonTestEnvironment.CreatePlayer();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(_betterPlayerAudioGameObject);
            Object.DestroyImmediate(_betterPlayerAudioOverrideGameObject);
            _udonTestEnvironment.Deconstruct();
            _udonTestEnvironment = null;
        }
        
        [Test]
        public void OnEnable_AddsPlayerInZoneToOverride()
        {
            var zoneGameObject = new GameObject();
            zoneGameObject.AddComponent<BoxCollider>();
            var voiceOverrideTriggerZone = zoneGameObject.AddComponent<VoiceOverrideTriggerZone>();
            voiceOverrideTriggerZone.playerAudioOverride = _voiceOverride;

            Assert.False(voiceOverrideTriggerZone.playerAudioOverride.IsAffected(_player1));
            
            voiceOverrideTriggerZone.OnPlayerTriggerEnter(_player1);

            Assert.True(voiceOverrideTriggerZone.playerAudioOverride.IsAffected(_player1));
        }
        
        [Test]
        public void OnDisable_RemovesPlayerInZoneFromOverride()
        {
            var zoneGameObject = new GameObject();
            zoneGameObject.AddComponent<BoxCollider>();
            var voiceOverrideTriggerZone = zoneGameObject.AddComponent<VoiceOverrideTriggerZone>();
            voiceOverrideTriggerZone.playerAudioOverride = _voiceOverride;

            voiceOverrideTriggerZone.playerAudioOverride.AddPlayer(_player1);
            voiceOverrideTriggerZone.playerAudioOverride.AddPlayer(_player2);
            
            Assert.AreEqual(2,  voiceOverrideTriggerZone.playerAudioOverride.playerList.players.Length);

            Assert.True( voiceOverrideTriggerZone.playerAudioOverride.IsAffected(_player1));
            Assert.True( voiceOverrideTriggerZone.playerAudioOverride.IsAffected(_player2));
            
            voiceOverrideTriggerZone.OnDisable();

            Assert.AreEqual(0,  voiceOverrideTriggerZone.playerAudioOverride.playerList.players.Length);
            
            Assert.False(voiceOverrideTriggerZone.playerAudioOverride.IsAffected(_player1));
            Assert.False(voiceOverrideTriggerZone.playerAudioOverride.IsAffected(_player2));
        }
        
        [Test]
        public void OnDisable_ThrowsErrorWhenInvalidOverride()
        {
            var zoneGameObject = new GameObject();
            zoneGameObject.AddComponent<BoxCollider>();
            var voiceOverrideTriggerZone = zoneGameObject.AddComponent<VoiceOverrideTriggerZone>();
            voiceOverrideTriggerZone.playerAudioOverride = null;

            LogAssert.Expect(LogType.Error, "playerAudioOverride is invalid");
            voiceOverrideTriggerZone.OnDisable();
        }
    }
}