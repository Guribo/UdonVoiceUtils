using System.Collections.Generic;
#if GURIBO_DEBUG
using System.Text.RegularExpressions;
#endif
using Guribo.UdonBetterAudio.Runtime;
using Guribo.UdonUtils.Runtime.Common;
using Guribo.UdonUtils.Tests.Runtime.Utils;
using NUnit.Framework;
using UdonSharp;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Tests.Editor
{
    public class TestBetterPlayerAudio
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

            _betterPlayerAudio = CreateBetterPlayerAudio(_betterPlayerAudioGameObject);
            _voiceOverride = CreateBetterPlayerVoiceOverride(_betterPlayerAudioOverrideGameObject, _betterPlayerAudio);

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
        }
        
        [Test]
        public void VoiceOverride_AffectedPlayers_EmptyVoiceOverrideDoesNotAffectPlayer()
        {
            Assert.AreEqual(new int[0], _voiceOverride.playerList.players);
            Assert.False(_voiceOverride.IsAffected(_player1));
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+player not affected.", RegexOptions.Singleline));
#endif
            Assert.False(_voiceOverride.RemovePlayer(_player1));
        }

        [Test]
        public void VoiceOverride_AffectedPlayers_PlayerCanBeAddedToAndRemovedFromOverride()
        {
            // add
            Assert.True(_voiceOverride.AddPlayer(_player1));
            Assert.AreEqual(1, _voiceOverride.playerList.players.Length);
            Assert.True(_voiceOverride.IsAffected(_player1));
            Assert.AreEqual(_player1.playerId, _voiceOverride.playerList.players[0]);

            // remove

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            Assert.True(_voiceOverride.RemovePlayer(_player1));
            Assert.AreEqual(0, _voiceOverride.playerList.players.Length);
            Assert.False(_voiceOverride.IsAffected(_player1));
        }

        [Test]
        public void VoiceOverride_AffectedPlayers_MultiplePlayersCanBeAddedToAndRemovedFromOverride()
        {
            Assert.True(_voiceOverride.AddPlayer(_player1));
            Assert.AreEqual(1, _voiceOverride.playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _voiceOverride.playerList.players[0]);

            Assert.True(_voiceOverride.AddPlayer(_player2));
            Assert.AreEqual(2, _voiceOverride.playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _voiceOverride.playerList.players[0]);
            Assert.AreEqual(_player2.playerId, _voiceOverride.playerList.players[1]);

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            Assert.True(_voiceOverride.RemovePlayer(_player1));
            Assert.AreEqual(1, _voiceOverride.playerList.players.Length);
            Assert.AreEqual(_player2.playerId, _voiceOverride.playerList.players[0]);

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            Assert.True(_voiceOverride.RemovePlayer(_player2));
            Assert.AreEqual(0, _voiceOverride.playerList.players.Length);

            Assert.True(_voiceOverride.AddPlayer(_player1));
            Assert.True(_voiceOverride.AddPlayer(_player2));
            Assert.AreEqual(2, _voiceOverride.playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _voiceOverride.playerList.players[0]);
            Assert.AreEqual(_player2.playerId, _voiceOverride.playerList.players[1]);

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            Assert.True(_voiceOverride.RemovePlayer(_player1));
            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            Assert.True(_voiceOverride.RemovePlayer(_player2));
            Assert.AreEqual(0, _voiceOverride.playerList.players.Length);
        }

        [Test]
        public void VoiceOverride_AffectedPlayers_MultiplePlayersAddedToOverrideAreSorted()
        {
            Assert.True(_voiceOverride.AddPlayer(_player1));
            Assert.True(_voiceOverride.AddPlayer(_player2));
            Assert.True(_voiceOverride.AddPlayer(_player3));


            Assert.AreEqual(3, _voiceOverride.playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _voiceOverride.playerList.players[0]);
            Assert.AreEqual(_player2.playerId, _voiceOverride.playerList.players[1]);
            Assert.AreEqual(_player3.playerId, _voiceOverride.playerList.players[2]);

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            Assert.True(_voiceOverride.RemovePlayer(_player1));
            Assert.AreEqual(2, _voiceOverride.playerList.players.Length);
            Assert.AreEqual(_player2.playerId, _voiceOverride.playerList.players[0]);
            Assert.AreEqual(_player3.playerId, _voiceOverride.playerList.players[1]);
        }

        [Test]
        public void BetterPlayerAudio_IgnorePlayer_PlayersCanBeIgnored()
        {
            Assert.False(_betterPlayerAudio.IsIgnored(_player1));

            _betterPlayerAudio.IgnorePlayer(_player1);
            Assert.True(_betterPlayerAudio.IsIgnored(_player1));

            _betterPlayerAudio.UnIgnorePlayer(_player1);
            Assert.False(_betterPlayerAudio.IsIgnored(_player1));
        }

        [Test]
        public void BetterPlayerAudio_IgnorePlayer_IgnoredPlayersDontUseGlobalSettings()
        {
            Assert.True(_betterPlayerAudio.UsesDefaultEffects(_player1));

            _betterPlayerAudio.IgnorePlayer(_player1);
            Assert.False(_betterPlayerAudio.UsesDefaultEffects(_player1));

            _betterPlayerAudio.UnIgnorePlayer(_player1);
            Assert.True(_betterPlayerAudio.UsesDefaultEffects(_player1));
        }

        [Test]
        public void VoiceOverride_UsesDefaultEffects_PlayersWithOverrideDontUseGlobalSettings()
        {
            Assert.True(_betterPlayerAudio.UsesDefaultEffects(_player1));

            _voiceOverride.AddPlayer(_player1);
            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            Assert.False(_betterPlayerAudio.UsesDefaultEffects(_player1));

            _voiceOverride.RemovePlayer(_player1);
            Assert.True(_betterPlayerAudio.UsesDefaultEffects(_player1));
        }

        [Test]
        public void BetterPlayerAudio_HasVoiceOverrides_PlayerWithOverrideHasOverrides()
        {
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Player invalid.", RegexOptions.Singleline));
#endif
            Assert.False(_betterPlayerAudio.HasVoiceOverrides(null));
            Assert.False(_betterPlayerAudio.HasVoiceOverrides(_player1));

            _voiceOverride.AddPlayer(_player1);

#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Player invalid.", RegexOptions.Singleline));
#endif
            Assert.False(_betterPlayerAudio.HasVoiceOverrides(null));
            Assert.True(_betterPlayerAudio.HasVoiceOverrides(_player1));

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            _voiceOverride.RemovePlayer(_player1);

#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Player invalid.", RegexOptions.Singleline));
#endif
            Assert.False(_betterPlayerAudio.HasVoiceOverrides(null));
            Assert.False(_betterPlayerAudio.HasVoiceOverrides(_player1));
        }

        [Test]
        public void BetterPlayerAudio_HasOverrides_IgnoredPlayersWithOverrideStayIgnored()
        {
            Assert.False(_betterPlayerAudio.HasVoiceOverrides(_player1));
            Assert.False(_betterPlayerAudio.UsesVoiceOverride(_player1));
            Assert.True(_betterPlayerAudio.UsesDefaultEffects(_player1));

            _betterPlayerAudio.IgnorePlayer(_player1);
            _voiceOverride.AddPlayer(_player1);
            Assert.True(_betterPlayerAudio.HasVoiceOverrides(_player1));
            Assert.False(_betterPlayerAudio.UsesVoiceOverride(_player1));
            Assert.False(_betterPlayerAudio.UsesDefaultEffects(_player1));

            _betterPlayerAudio.UnIgnorePlayer(_player1);
            Assert.True(_betterPlayerAudio.HasVoiceOverrides(_player1));
            Assert.True(_betterPlayerAudio.UsesVoiceOverride(_player1));
            Assert.False(_betterPlayerAudio.UsesDefaultEffects(_player1));

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            _voiceOverride.RemovePlayer(_player1);
            Assert.False(_betterPlayerAudio.HasVoiceOverrides(_player1));
            Assert.False(_betterPlayerAudio.UsesVoiceOverride(_player1));
            Assert.True(_betterPlayerAudio.UsesDefaultEffects(_player1));
        }

        [Test]
        public void BetterPlayerAudio_CreateOverrideSlotForPlayer()
        {
            Assert.AreEqual(0, _betterPlayerAudio.PlayersToOverride.Length);
            var ids = _betterPlayerAudio.GetNonLocalPlayersWithOverrides();
            Assert.AreEqual(0, ids.Length);
            Assert.AreEqual(1, _betterPlayerAudio.CreateOverrideSlotForPlayer(_localPlayer));
            ids = _betterPlayerAudio.GetNonLocalPlayersWithOverrides();
            Assert.AreEqual(1, ids.Length);
            Assert.AreEqual(0, ids[0]);
            Assert.AreEqual(1, _betterPlayerAudio.PlayersToOverride.Length);
            Assert.AreEqual(2, _betterPlayerAudio.CreateOverrideSlotForPlayer(_player1));
            ids = _betterPlayerAudio.GetNonLocalPlayersWithOverrides();
            Assert.AreEqual(2, ids.Length);
            Assert.AreEqual(0, ids[0]);
            Assert.AreEqual(1, ids[1]);
            Assert.AreEqual(2, _betterPlayerAudio.PlayersToOverride.Length);
            Assert.AreEqual(3, _betterPlayerAudio.CreateOverrideSlotForPlayer(_player2));
            Assert.AreEqual(3, _betterPlayerAudio.PlayersToOverride.Length);
            ids = _betterPlayerAudio.GetNonLocalPlayersWithOverrides();
            Assert.AreEqual(3, ids.Length);
            Assert.AreEqual(0, ids[0]);
            Assert.AreEqual(1, ids[1]);
            Assert.AreEqual(2, ids[2]);
        }

        [Test]
        public void BetterPlayerAudio_OverridePlayerSettings()
        {
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+betterPlayerAudioOverride invalid.", RegexOptions.Singleline));
#endif
            Assert.False(_betterPlayerAudio.OverridePlayerSettings(null, null));
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+playerToAffect invalid.", RegexOptions.Singleline));
#endif
            Assert.False(_betterPlayerAudio.OverridePlayerSettings(_voiceOverride, null));

#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+betterPlayerAudioOverride invalid.", RegexOptions.Singleline));
#endif
            Assert.False(_betterPlayerAudio.OverridePlayerSettings(null, _player1));

            Assert.True(_betterPlayerAudio.OverridePlayerSettings(_voiceOverride, _player1));
        }

        [Test]
        public void BetterPlayerAudio_GetMaxPriorityOverride_PlayerWithOverrideHasHighPriorityOverride()
        {
            Assert.IsNull(_betterPlayerAudio.GetMaxPriorityOverride(null));
            Assert.IsNull(_betterPlayerAudio.GetMaxPriorityOverride(_player2));
            Assert.True(_voiceOverride.AddPlayer(_player2));
            Assert.True(_voiceOverride.IsAffected(_player2));
            Assert.AreEqual(1, _voiceOverride.playerList.players.Length);
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_player2));
            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            Assert.True(_voiceOverride.RemovePlayer(_player2));
            Assert.IsNull(_betterPlayerAudio.GetMaxPriorityOverride(_player2));
            Assert.False(_voiceOverride.IsAffected(_player2));
            Assert.AreEqual(0, _voiceOverride.playerList.players.Length);

            Assert.True(_voiceOverride.AddPlayer(_localPlayer));
            Assert.AreEqual(1, _voiceOverride.playerList.players.Length);
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.localPlayerOverrideList.Get(0));
        }

        [Test]
        public void BetterPlayerAudio_GetMaxPriorityOverride_HighPriorityOverrideIsMaxPriorityOverride()
        {
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);
            
            voiceOverride2.udonDebug = _betterPlayerAudioGameObject.AddComponent<UdonDebug>();
            voiceOverride2.playerList = _betterPlayerAudioGameObject.AddComponent<PlayerList>();
            voiceOverride2.playerList.udonDebug = _voiceOverride.udonDebug;

            _voiceOverride.priority = 1;
            voiceOverride2.priority = 2;

            _voiceOverride.AddPlayer(_player1);
            voiceOverride2.AddPlayer(_player1);
            Assert.AreNotEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_player1));
            Assert.AreEqual(voiceOverride2, _betterPlayerAudio.GetMaxPriorityOverride(_player1));
        }

        [Test]
        public void BetterPlayerAudio_OtherPlayerWithOverrideCanBeHeard()
        {
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);
            var voiceOverride3 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            voiceOverride2.udonDebug = _betterPlayerAudioGameObject.AddComponent<UdonDebug>();
            voiceOverride2.playerList = _betterPlayerAudioGameObject.AddComponent<PlayerList>();
            voiceOverride2.playerList.udonDebug = _voiceOverride.udonDebug;
            voiceOverride3.udonDebug = _betterPlayerAudioGameObject.AddComponent<UdonDebug>();
            voiceOverride3.playerList = _betterPlayerAudioGameObject.AddComponent<PlayerList>();
            voiceOverride3.playerList.udonDebug = _voiceOverride.udonDebug;

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            
            _voiceOverride.priority = 0;
            _voiceOverride.privacyChannelId = 0;
            voiceOverride2.priority = 2;
            _voiceOverride.privacyChannelId = -1;
            voiceOverride3.priority = 1;
            _voiceOverride.privacyChannelId = 1;

            _voiceOverride.AddPlayer(_player2);
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_player2));
            voiceOverride2.AddPlayer(_player2);
            Assert.AreEqual(voiceOverride2, _betterPlayerAudio.GetMaxPriorityOverride(_player2));
            voiceOverride3.AddPlayer(_player2);
            Assert.AreEqual(voiceOverride2, _betterPlayerAudio.GetMaxPriorityOverride(_player2));
            Assert.True(_betterPlayerAudio.OtherPlayerWithOverrideCanBeHeard(voiceOverride2, false, -1, false, false));
        }

        [Test]
        public void BetterPlayerAudio_LocalPlayerInSamePriorityZoneCantHearOtherInZone()
        {

            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);
            var voiceOverride3 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            voiceOverride2.udonDebug = _betterPlayerAudioGameObject.AddComponent<UdonDebug>();
            voiceOverride2.playerList = _betterPlayerAudioGameObject.AddComponent<PlayerList>();
            voiceOverride2.playerList.udonDebug = _voiceOverride.udonDebug;
            voiceOverride3.udonDebug = _betterPlayerAudioGameObject.AddComponent<UdonDebug>();
            voiceOverride3.playerList = _betterPlayerAudioGameObject.AddComponent<PlayerList>();
            voiceOverride3.playerList.udonDebug = _voiceOverride.udonDebug;

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();

            _voiceOverride.priority = 0;
            _voiceOverride.privacyChannelId = 3;
            _voiceOverride.muteOutsiders = true;

            voiceOverride2.priority = 0;
            voiceOverride2.privacyChannelId = 3;
            voiceOverride2.muteOutsiders = false;
            voiceOverride2.disallowListeningToChannel = true;
            
            _voiceOverride.AddPlayer(_player2);
            voiceOverride2.AddPlayer(_player1);

            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_player2));
            Assert.AreEqual(voiceOverride2, _betterPlayerAudio.GetMaxPriorityOverride(_player1));

            Assert.False(_betterPlayerAudio.OtherPlayerWithOverrideCanBeHeard(_voiceOverride, true,
                voiceOverride2.privacyChannelId, voiceOverride2.muteOutsiders,
                voiceOverride2.disallowListeningToChannel));
            Assert.True(_betterPlayerAudio.OtherPlayerWithOverrideCanBeHeard(_voiceOverride, true,
                voiceOverride2.privacyChannelId, voiceOverride2.muteOutsiders, false));
        }

        [Test]
        public void BetterPlayerAudio_GetMaxPriorityOverride_LowerPriorityOverrideIsNotMaxPriorityOverride()
        {
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);
            voiceOverride2.udonDebug = _voiceOverride.udonDebug;
            voiceOverride2.playerList = _betterPlayerAudioGameObject.AddComponent<PlayerList>();
            voiceOverride2.playerList.udonDebug = _voiceOverride.udonDebug;

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            _voiceOverride.priority = 1;
            voiceOverride2.priority = 2;

            voiceOverride2.AddPlayer(_player1);
            _voiceOverride.AddPlayer(_player1);
            Assert.AreNotEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_player1));
            Assert.AreEqual(voiceOverride2, _betterPlayerAudio.GetMaxPriorityOverride(_player1));
        }

        [Test]
        public void ListObject_AddOverride_OverrideCanBeAddedToList()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.False(listObject.AddOverride(null));
        }

        [Test]
        public void ListObject_AddOverride_SingleOverrideCanBeRetrieved()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            Assert.AreEqual(null, listObject.Get(0));
            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.AreEqual(_voiceOverride, listObject.Get(0));
            Assert.AreEqual(null, listObject.Get(1));
        }

        [Test]
        public void ListObject_AddOverride_MultipleOverrideWithSamePriorityCanBeAddedAndRetrieved()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.AreEqual(voiceOverride2, listObject.Get(0));
            Assert.AreEqual(_voiceOverride, listObject.Get(1));
        }

        [Test]
        public void ListObject_AddOverride_SameOverrideWithSamePriorityCanBeAddedMultipleTimesAndRetrieved()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.False(listObject.AddOverride(_voiceOverride));
            Assert.AreEqual(_voiceOverride, listObject.Get(1));
            Assert.AreEqual(voiceOverride2, listObject.Get(0));
            Assert.AreEqual(null, listObject.Get(2));
        }

        [Test]
        public void ListObject_AddOverride_LowerPriorityIsNotAtFirstPositionWhenAddedAfterHighPriority()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            _voiceOverride.priority = 0;
            voiceOverride2.priority = 1;

            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.True(listObject.AddOverride(_voiceOverride));

            Assert.AreEqual(voiceOverride2, listObject.Get(0));
            Assert.AreEqual(_voiceOverride, listObject.Get(1));
        }

        [Test]
        public void ListObject_AddOverride_AddSingleOverride()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            _voiceOverride.priority = 0;
            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.AreEqual(_voiceOverride, listObject.Get(0));
            Assert.AreEqual(null, listObject.Get(1));
        }

        [Test]
        public void ListObject_AddOverride_AddSingleOverridesTwice()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            _voiceOverride.priority = 0;
            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.False(listObject.AddOverride(_voiceOverride));
            Assert.AreEqual(_voiceOverride, listObject.Get(0));
            Assert.AreEqual(null, listObject.Get(1));
        }

        [Test]
        public void ListObject_AddOverride_AddTwoOverrides()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.AreEqual(_voiceOverride, listObject.Get(0));
            Assert.AreEqual(null, listObject.Get(1));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.AreEqual(voiceOverride2, listObject.Get(0));
            Assert.AreEqual(_voiceOverride, listObject.Get(1));
            Assert.AreEqual(null, listObject.Get(2));
        }

        [Test]
        public void ListObject_InsertNewOverride_InsertAddedOverride()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            var originalList = new BetterPlayerAudioOverride[0];
            var tempList = new BetterPlayerAudioOverride[0];
            tempList = listObject.InsertNewOverride(_voiceOverride, 0, tempList, originalList);
            Assert.AreEqual(1, tempList.Length);
            Assert.AreEqual(_voiceOverride, tempList[0]);

            originalList = new BetterPlayerAudioOverride[1];
            tempList = new BetterPlayerAudioOverride[1];
            tempList = listObject.InsertNewOverride(_voiceOverride, 0, tempList, originalList);
            Assert.AreEqual(1, tempList.Length);
            Assert.AreEqual(_voiceOverride, tempList[0]);

            originalList = new[] {_voiceOverride};
            tempList = new BetterPlayerAudioOverride[1];
            tempList = listObject.InsertNewOverride(_voiceOverride, 0, tempList, originalList);
            Assert.AreEqual(1, tempList.Length);
            Assert.AreEqual(_voiceOverride, tempList[0]);

            originalList = new BetterPlayerAudioOverride[3];
            tempList = new BetterPlayerAudioOverride[3];
            tempList = listObject.InsertNewOverride(_voiceOverride, 1, tempList, originalList);
            Assert.AreEqual(3, tempList.Length);
            Assert.AreEqual(null, tempList[0]);
            Assert.AreEqual(_voiceOverride, tempList[1]);
            Assert.AreEqual(null, tempList[2]);

            originalList = new BetterPlayerAudioOverride[2];
            tempList = new BetterPlayerAudioOverride[2];
            tempList = listObject.InsertNewOverride(_voiceOverride, 2, tempList, originalList);
            Assert.AreEqual(3, tempList.Length);
            Assert.AreEqual(null, tempList[0]);
            Assert.AreEqual(null, tempList[1]);
            Assert.AreEqual(_voiceOverride, tempList[2]);
        }

        [Test]
        public void ListObject_AddOverride_LowerPriorityIsAtSecondPositionWhenAddedAfterHighPriorityAndSamePriority()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);
            var voiceOverride3 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            _voiceOverride.priority = 0;
            voiceOverride2.priority = 1;
            voiceOverride3.priority = 0;

            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.True(listObject.AddOverride(voiceOverride3));

            Assert.AreEqual(voiceOverride2, listObject.Get(0));
            Assert.AreEqual(voiceOverride3, listObject.Get(1));
            Assert.AreEqual(_voiceOverride, listObject.Get(2));
        }


        [Test]
        public void ListObject_GetInsertIndex_InsertPositionOfInvalidParametersIsNegative1()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            var overrides = new BetterPlayerAudioOverride[0];
            Assert.AreEqual(-1, listObject.GetInsertIndex(null, null));
            Assert.AreEqual(-1, listObject.GetInsertIndex(null, _voiceOverride));
            Assert.AreEqual(-1, listObject.GetInsertIndex(overrides, null));
        }

        [Test]
        public void ListObject_GetInsertIndex_EmptyOverrideArrayHasInsertIndex0()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            var overrides = new BetterPlayerAudioOverride[0];
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, _voiceOverride));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithInvalidOverrideHasInsertIndex0()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            var overrides = new BetterPlayerAudioOverride[1];
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, _voiceOverride));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithSameOverrideHasInsertIndex0()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            var overrides = new[] {_voiceOverride};
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, _voiceOverride));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithDifferentOverrideButSamePriorityHasInsertIndex0()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            var overrides = new[] {_voiceOverride};
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithDifferentOverrideButSameDifferentPriorityHasInsertIndex0()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            _voiceOverride.priority = 1;
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);
            voiceOverride2.priority = 1;

            var overrides = new[] {_voiceOverride};
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithDifferentOverrideButHigherPriorityHasInsertIndex0()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            _voiceOverride.priority = 0;
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);
            voiceOverride2.priority = 1;

            var overrides = new[] {_voiceOverride};
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithDifferentOverrideButLowerPriorityHasInsertIndex1()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            _voiceOverride.priority = 1;
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            var overrides = new[] {_voiceOverride};
            Assert.AreEqual(1, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithValidAndInvalidOverrideButLowerPriorityHasInsertIndex1()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            _voiceOverride.priority = 1;
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            var overrides = new[] {_voiceOverride, null};
            Assert.AreEqual(1, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_Consolidate()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);
            var voiceOverride3 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            Assert.AreEqual(0, listObject.Consolidate(null));

            var list = new BetterPlayerAudioOverride[0];
            Assert.AreEqual(0, listObject.Consolidate(list));
            Assert.AreEqual(0, list.Length);

            list = new BetterPlayerAudioOverride[] {null};
            Assert.AreEqual(0, listObject.Consolidate(list));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(null, list[0]);

            list = new[] {_voiceOverride};
            Assert.AreEqual(1, listObject.Consolidate(list));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(_voiceOverride, list[0]);

            list = new[] {_voiceOverride, null};
            Assert.AreEqual(1, listObject.Consolidate(list));
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(_voiceOverride, list[0]);
            Assert.AreEqual(null, list[1]);

            list = new[] {null, _voiceOverride};
            Assert.AreEqual(1, listObject.Consolidate(list));
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(_voiceOverride, list[0]);
            Assert.AreEqual(null, list[1]);

            list = new BetterPlayerAudioOverride[] {null, null};
            Assert.AreEqual(0, listObject.Consolidate(list));
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(null, list[0]);
            Assert.AreEqual(null, list[1]);

            list = new BetterPlayerAudioOverride[] {null, null, null};
            Assert.AreEqual(0, listObject.Consolidate(list));
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(null, list[0]);
            Assert.AreEqual(null, list[1]);
            Assert.AreEqual(null, list[2]);

            list = new[] {_voiceOverride, null, voiceOverride2};
            Assert.AreEqual(2, listObject.Consolidate(list));
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(_voiceOverride, list[0]);
            Assert.AreEqual(voiceOverride2, list[1]);
            Assert.AreEqual(null, list[2]);

            list = new[] {null, null, voiceOverride2};
            Assert.AreEqual(1, listObject.Consolidate(list));
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(voiceOverride2, list[0]);
            Assert.AreEqual(null, list[1]);
            Assert.AreEqual(null, list[2]);

            list = new[] {null, _voiceOverride, voiceOverride2};
            Assert.AreEqual(2, listObject.Consolidate(list));
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(_voiceOverride, list[0]);
            Assert.AreEqual(voiceOverride2, list[1]);
            Assert.AreEqual(null, list[2]);

            list = new[] {_voiceOverride, voiceOverride2, voiceOverride3};
            Assert.AreEqual(3, listObject.Consolidate(list));
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(_voiceOverride, list[0]);
            Assert.AreEqual(voiceOverride2, list[1]);
            Assert.AreEqual(voiceOverride3, list[2]);
        }

        [Test]
        public void ListObject_Remove()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            Assert.False(listObject.Remove(null, null));

            var list = new BetterPlayerAudioOverride[0];
            Assert.False(listObject.Remove(list, null));
            Assert.False(listObject.Remove(list, _voiceOverride));
            Assert.AreEqual(0, list.Length);

            list = new BetterPlayerAudioOverride[1];
            Assert.False(listObject.Remove(list, _voiceOverride));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(null, list[0]);

            list = new[] {voiceOverride2};
            Assert.False(listObject.Remove(list, _voiceOverride));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(voiceOverride2, list[0]);

            list = new[] {_voiceOverride};
            Assert.True(listObject.Remove(list, _voiceOverride));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(null, list[0]);

            list = new[] {_voiceOverride, _voiceOverride};
            Assert.True(listObject.Remove(list, _voiceOverride));
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(null, list[0]);
            Assert.AreEqual(null, list[1]);

            list = new[] {voiceOverride2, _voiceOverride};
            Assert.True(listObject.Remove(list, voiceOverride2));
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(null, list[0]);
            Assert.AreEqual(_voiceOverride, list[1]);
        }

        [Test]
        public void Refresh()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();

            _voiceOverride.playerList.players = new[] {0, 1};
            _voiceOverride.Refresh();
            Assert.True(_betterPlayerAudio.HasVoiceOverrides(_localPlayer));
            Assert.True(_betterPlayerAudio.HasVoiceOverrides(_player1));
        }

        [Test]
        public void ListObject_RemoveOverride()
        {
            var listObject = _betterPlayerAudioGameObject.AddComponent<BetterPlayerAudioOverrideList>();
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(_betterPlayerAudioGameObject, _betterPlayerAudio);

            Assert.AreEqual(0, listObject.RemoveOverride(null));
            Assert.AreEqual(0, listObject.RemoveOverride(_voiceOverride));

            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.AreEqual(0, listObject.RemoveOverride(_voiceOverride));
            Assert.AreEqual(null, listObject.Get(0));

            Assert.True(listObject.AddOverride(_voiceOverride));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.AreEqual(1, listObject.RemoveOverride(_voiceOverride));
            Assert.AreEqual(voiceOverride2, listObject.Get(0));
        }

        [Test]
        public void EnableDisableComponent()
        {
            _voiceOverride.AddPlayer(_localPlayer);
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));

            _voiceOverride.enabled = false;
            _voiceOverride.OnDisable();
            Assert.Null(_betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));

            _voiceOverride.enabled = true;
            _voiceOverride.OnEnable();
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));

            _voiceOverride.enabled = false;
            _voiceOverride.OnDisable();
            _voiceOverride.AddPlayer(_player1);
            Assert.Null(_betterPlayerAudio.GetMaxPriorityOverride(_player1));

            _voiceOverride.enabled = true;
            _voiceOverride.OnEnable();
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_player1));
        }

        private class LocalPlayerChangeEventListener : UdonSharpBehaviour
        {
            public bool added;
            public bool removed;

            public void LocalPlayerAdded()
            {
                added = true;
            }

            public void LocalPlayerRemoved()
            {
                removed = true;
            }
        }

        [Test]
        public void EnableDisableGameobject()
        {
            var voiceOverrideGameobject = new GameObject();
            _voiceOverride = CreateBetterPlayerVoiceOverride(voiceOverrideGameobject, _betterPlayerAudio);
            _voiceOverride.playerList.udonDebug = voiceOverrideGameobject.AddComponent<UdonDebug>();
            var changeListener = voiceOverrideGameobject.AddComponent<LocalPlayerChangeEventListener>();
            _voiceOverride.localPlayerAddedListeners = new[] {(UdonSharpBehaviour) changeListener};
            _voiceOverride.localPlayerRemovedListeners = new[] {(UdonSharpBehaviour) changeListener};

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();

            _voiceOverride.AddPlayer(_localPlayer);
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.True(changeListener.added);

            _voiceOverride.gameObject.SetActive(false);
            _voiceOverride.OnDisable();
            Assert.Null(_betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.True(changeListener.removed);

            changeListener.added = false;
            changeListener.removed = false;

            _voiceOverride.gameObject.SetActive(true);
            _voiceOverride.OnEnable();
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.True(changeListener.added);

            _voiceOverride.gameObject.SetActive(false);
            _voiceOverride.enabled = false;
            _voiceOverride.OnDisable();
            _voiceOverride.AddPlayer(_player1);
            
            Assert.Null(_betterPlayerAudio.GetMaxPriorityOverride(_player1));
            Assert.True(changeListener.removed);

            changeListener.added = false;
            changeListener.removed = false;

            _voiceOverride.gameObject.SetActive(true);
            _voiceOverride.OnEnable();
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_player1));
            Assert.True(changeListener.added);
        }

        [Test]
        public void ClearReverb()
        {
            _betterPlayerAudio.mainAudioReverbFilter = null;
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+mainAudioReverbFilter invalid.", RegexOptions.Singleline));
#endif
            _betterPlayerAudio.UseReverbSettings(null);

            // add the reverb filter (usually done by user in editor)
            var audioReverbObject = new GameObject();
            audioReverbObject.AddComponent<AudioListener>();
            _betterPlayerAudio.mainAudioReverbFilter = audioReverbObject.AddComponent<AudioReverbFilter>();
            Assert.True(_betterPlayerAudio.enabled);

            // test clearing the reverb settings
            _betterPlayerAudio.mainAudioReverbFilter.reverbPreset = AudioReverbPreset.Alley;
            _betterPlayerAudio.UseReverbSettings(null);
            Assert.NotNull(_betterPlayerAudio.mainAudioReverbFilter);
            Assert.True(_betterPlayerAudio.mainAudioReverbFilter.enabled);
            Assert.AreEqual(AudioReverbPreset.Off, _betterPlayerAudio.mainAudioReverbFilter.reverbPreset);
        }

        [Test]
        public void UseReverbSettings()
        {
            _betterPlayerAudio.mainAudioReverbFilter = null;
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+mainAudioReverbFilter invalid.", RegexOptions.Singleline));
#endif
            _betterPlayerAudio.UseReverbSettings(null);

            // add the reverb filter (usually done by user in editor)
            var audioReverbObject = new GameObject();
            var audioReverbObject2 = new GameObject();
            audioReverbObject.AddComponent<AudioListener>();
            _betterPlayerAudio.mainAudioReverbFilter = audioReverbObject.AddComponent<AudioReverbFilter>();

            audioReverbObject2.AddComponent<AudioListener>();
            var toCopyFrom = audioReverbObject2.AddComponent<AudioReverbFilter>();
            toCopyFrom.reverbPreset = AudioReverbPreset.Arena;

            // "old" settings
            _betterPlayerAudio.mainAudioReverbFilter.reverbPreset = AudioReverbPreset.Alley;

            // copy the reverb settings from the provided component
            _betterPlayerAudio.UseReverbSettings(toCopyFrom);
            Assert.AreEqual(AudioReverbPreset.Arena, _betterPlayerAudio.mainAudioReverbFilter.reverbPreset);

            // copy custom preset
            toCopyFrom.reverbPreset = AudioReverbPreset.User;
            toCopyFrom.density = 0;
            toCopyFrom.diffusion = 0;
            toCopyFrom.room = 0;
            toCopyFrom.roomHF = 0;
            toCopyFrom.roomLF = 0;
            toCopyFrom.decayTime = 0;
            toCopyFrom.dryLevel = 0;
            toCopyFrom.hfReference = 0;
            toCopyFrom.lfReference = 0;
            toCopyFrom.reflectionsDelay = 0;
            toCopyFrom.reflectionsLevel = 0;
            toCopyFrom.reverbDelay = 0;
            toCopyFrom.reverbLevel = 0;
            toCopyFrom.decayHFRatio = 0;

            _betterPlayerAudio.UseReverbSettings(toCopyFrom);
            Assert.AreEqual(AudioReverbPreset.User, _betterPlayerAudio.mainAudioReverbFilter.reverbPreset);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.density);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.diffusion);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.room);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.roomHF);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.roomLF);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.decayTime);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.dryLevel);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.hfReference);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.lfReference);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.reflectionsDelay);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.reflectionsLevel);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.reverbDelay);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.reverbLevel);
            Assert.AreEqual(0, _betterPlayerAudio.mainAudioReverbFilter.decayHFRatio);
        }

        [Test]
        public void UpdateAudioFilters_BothNull()
        {
            Assert.Null(_betterPlayerAudio.UpdateAudioFilters(null, null));
        }

        [Test]
        public void UpdateAudioFilters_BothNotNullEqual()
        {
Assert.AreEqual(_voiceOverride,
                _betterPlayerAudio.UpdateAudioFilters(_voiceOverride, _voiceOverride));
        }

        [Test]
        public void UpdateAudioFilters_OldNullNewValid()
        {
            _betterPlayerAudio.mainAudioReverbFilter.reverbPreset = AudioReverbPreset.Off;
            var expected = AudioReverbPreset.Auditorium;
            _voiceOverride.optionalReverb.reverbPreset = expected;

            Assert.AreEqual(_voiceOverride,
                _betterPlayerAudio.UpdateAudioFilters(_voiceOverride, null));
            Assert.AreEqual(expected, _betterPlayerAudio.mainAudioReverbFilter.reverbPreset);
        }

        [Test]
        public void UpdateAudioFilters_OldValidNewNull()
        {

            _betterPlayerAudio.mainAudioReverbFilter.reverbPreset = AudioReverbPreset.Off;
            _voiceOverride.optionalReverb.reverbPreset = AudioReverbPreset.Auditorium;

            Assert.Null(_betterPlayerAudio.UpdateAudioFilters(null, _voiceOverride));
            Assert.AreEqual(AudioReverbPreset.Off, _betterPlayerAudio.mainAudioReverbFilter.reverbPreset);
        }
        
        [Test]
        public void UpdateAudioFilters_BothValidDifferent()
        {
            var betterPlayerAudioOverride2 = CreateBetterPlayerVoiceOverride(new GameObject(),
                _betterPlayerAudio);

            _betterPlayerAudio.mainAudioReverbFilter.reverbPreset = AudioReverbPreset.Off;
            _voiceOverride.optionalReverb.reverbPreset = AudioReverbPreset.Auditorium;
            betterPlayerAudioOverride2.optionalReverb.reverbPreset = AudioReverbPreset.Bathroom;

            Assert.AreEqual(betterPlayerAudioOverride2,
                _betterPlayerAudio.UpdateAudioFilters(betterPlayerAudioOverride2, _voiceOverride));
            Assert.AreEqual(AudioReverbPreset.Bathroom, _betterPlayerAudio.mainAudioReverbFilter.reverbPreset);
        }
        
        [Test]
        public void UpdatePlayer_NewValid()
        {
            _betterPlayerAudio.mainAudioReverbFilter.reverbPreset = AudioReverbPreset.Off;
            _voiceOverride.optionalReverb.reverbPreset = AudioReverbPreset.Auditorium;

            _voiceOverride.AddPlayer(_localPlayer);

            Assert.AreNotEqual(AudioReverbPreset.Auditorium, _betterPlayerAudio.mainAudioReverbFilter.reverbPreset);
            _betterPlayerAudio.UpdateOtherPlayer(_localPlayer,_player2);
            Assert.AreEqual(AudioReverbPreset.Auditorium, _betterPlayerAudio.mainAudioReverbFilter.reverbPreset);
        }


        #region Test Utils

        public static BetterPlayerAudioOverride CreateBetterPlayerVoiceOverride(GameObject go,
            BetterPlayerAudio betterPlayerAudio)
        {
            var voiceOverride = go.AddComponent<BetterPlayerAudioOverride>();
            go.AddComponent<AudioListener>();

            voiceOverride.optionalReverb = go.AddComponent<AudioReverbFilter>();
            voiceOverride.playerList = go.AddComponent<PlayerList>();
            voiceOverride.betterPlayerAudio = betterPlayerAudio;
            voiceOverride.udonDebug = go.AddComponent<UdonDebug>();
            return voiceOverride;
        }

        public static BetterPlayerAudio CreateBetterPlayerAudio(GameObject go)
        {
            var betterPlayerAudio = go.AddComponent<BetterPlayerAudio>();
            Assert.True(Utilities.IsValid(betterPlayerAudio));

            go.AddComponent<AudioListener>();
            betterPlayerAudio.mainAudioReverbFilter = go.AddComponent<AudioReverbFilter>();

            var udonDebug = go.AddComponent<UdonDebug>();
            Assert.True(Utilities.IsValid(betterPlayerAudio));
            betterPlayerAudio.udonDebug = udonDebug;

            var listPrefab = new GameObject("ListPrefab");
            betterPlayerAudio.cloneablePlayerList = listPrefab.AddComponent<BetterPlayerAudioOverrideList>();

            var localList = new GameObject("LocalPlayerOverrideList");
            betterPlayerAudio.localPlayerOverrideList = localList.AddComponent<BetterPlayerAudioOverrideList>();

            betterPlayerAudio.OnEnable();
            betterPlayerAudio.Start();

            return betterPlayerAudio;
        }

        #endregion
    }
}