using Guribo.UdonBetterAudio.Runtime.Examples;
using Guribo.UdonUtils.Tests.Runtime.Utils;
using NUnit.Framework;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Tests.Editor
{
    public class TestAutoPlayerRange
    {
        private AutoPlayerRange _autoPlayerRange;
        private AnimationCurve _playerRangeMapping;
        private const float DefaultRange = 25f;
        private UdonTestUtils.UdonTestEnvironment _udonTestEnvironment;


        [SetUp]
        public void Prepare()
        {
            _autoPlayerRange = new GameObject().AddComponent<AutoPlayerRange>();
            _playerRangeMapping = AnimationCurve.Linear(1, 25, 80, 10);
            _udonTestEnvironment = new UdonTestUtils.UdonTestEnvironment();
        }
        
        [TearDown]
        public void Cleanup()
        {
            _udonTestEnvironment.Deconstruct();
            _udonTestEnvironment = null;
        }

        [Test]
        public void OnPlayerJoined_invalid()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            
            Assert.DoesNotThrow(() => _autoPlayerRange.OnPlayerJoined(null));
            
            VerifyVoiceRange();
        }

        [Test]
        public void OnPlayerLeft_invalid()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            
            Assert.DoesNotThrow(() => _autoPlayerRange.OnPlayerLeft(null));
            
            VerifyVoiceRange();
        }

        [Test]
        public void UpdatePlayers()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            
           _autoPlayerRange.UpdatePlayerVoiceRange(false);
            
           VerifyVoiceRange();
        }
        
        [Test]
        public void OnPlayerJoined_ValidPlayer()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            
            Assert.DoesNotThrow(() => _autoPlayerRange.OnPlayerJoined(_udonTestEnvironment.CreatePlayer()));
            
            VerifyVoiceRange();
        }
        
        [Test]
        public void OnPlayerLeft_ValidPlayer()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            
            Assert.DoesNotThrow(() => _autoPlayerRange.OnPlayerLeft(new VRCPlayerApi()));
            
            VerifyVoiceRange();
        }

        [Test]
        public void GetRange_InvalidCurveInvalidCount()
        {
            Assert.AreEqual(DefaultRange, _autoPlayerRange.GetRange(null, 0));
        }

        [Test]
        public void GetRange_ValidCurveInvalidCount()
        {
            Assert.AreEqual(_playerRangeMapping.Evaluate(1), _autoPlayerRange.GetRange(_playerRangeMapping, 0));
            Assert.AreEqual(_playerRangeMapping.Evaluate(80), _autoPlayerRange.GetRange(_playerRangeMapping, 81));
        }

        [Test]
        public void GetRange_ValidCurveValidCount()
        {
            Assert.AreEqual(_playerRangeMapping.Evaluate(40), _autoPlayerRange.GetRange(_playerRangeMapping, 40));
        }

        [Test]
        public void UpdateVoiceRange_invalidPlayers()
        {
            Assert.DoesNotThrow(() => _autoPlayerRange.UpdateVoiceRange(null, 0));
        }

        [Test]
        public void UpdateVoiceRange_ValidPlayers()
        {
            var players = new[]
            {
                _udonTestEnvironment.CreatePlayer(),
                _udonTestEnvironment.CreatePlayer(),
                _udonTestEnvironment.CreatePlayer()
            };

            Assert.DoesNotThrow(() => _autoPlayerRange.UpdateVoiceRange(players, 0));
            foreach (var playerApi in players)
            {
                Assert.AreEqual(0, _udonTestEnvironment.GetPlayerData(playerApi).voiceRangeFar);
            }

            Assert.DoesNotThrow(() => _autoPlayerRange.UpdateVoiceRange(players, 7));
            foreach (var playerApi in players)
            {
                Assert.AreEqual(7, _udonTestEnvironment.GetPlayerData(playerApi).voiceRangeFar);
            }
        }

        [Test]
        public void UpdateVoiceRange_PartiallyValidPlayers()
        {
            var players = new[]
            {
                _udonTestEnvironment.CreatePlayer(),
                null,
                _udonTestEnvironment.CreatePlayer()
            };

            Assert.DoesNotThrow(() => _autoPlayerRange.UpdateVoiceRange(players, 5));
            Assert.AreEqual(5, _udonTestEnvironment.GetPlayerData(players[0]).voiceRangeFar);
            Assert.Null(players[1]);
            Assert.AreEqual(5, _udonTestEnvironment.GetPlayerData(players[2]).voiceRangeFar);
        }

        [Test]
        public void Disable_ResetsRange()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();

            _autoPlayerRange.OnPlayerJoined(_udonTestEnvironment.CreatePlayer());

            VerifyVoiceRange();

            _autoPlayerRange.OnDisable();

            VerifyVoiceRange(true);
            
            _autoPlayerRange.OnEnable();
            VerifyVoiceRange();
        }
        
        private void VerifyVoiceRange(bool expectDefault = false)
        {
            foreach (var vrcPlayerApi in VRCPlayerApi.AllPlayers)
            {
                if (expectDefault)
                {
                    Assert.AreEqual(DefaultRange, _udonTestEnvironment.GetPlayerData(vrcPlayerApi).voiceRangeFar);
                    continue;
                }
                Assert.AreNotEqual(DefaultRange, _udonTestEnvironment.GetPlayerData(vrcPlayerApi).voiceRangeFar);
                Assert.AreEqual(_playerRangeMapping.Evaluate(VRCPlayerApi.AllPlayers.Count),
                    _udonTestEnvironment.GetPlayerData(vrcPlayerApi).voiceRangeFar);
            }
        }
    }
}