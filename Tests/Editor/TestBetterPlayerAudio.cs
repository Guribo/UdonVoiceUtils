using System.Collections.Generic;
using System.Text.RegularExpressions;
using Guribo.UdonBetterAudio.Runtime;
using Guribo.UdonUtils.Runtime.Common;
using Guribo.UdonUtils.Tests.Editor;
using Guribo.UdonUtils.Tests.Editor.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Tests.Editor
{
    public class TestBetterPlayerAudio
    {
        [Test]
        public void VoiceOverride_AffectedPlayers_EmptyVoiceOverrideDoesNotAffectPlayer()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var player1 = UdonTestUtils.CreatePlayer(0);

            Assert.AreEqual(0, voiceOverride.GetAffectedPlayers().Length);
            Assert.False(voiceOverride.IsAffected(player1));
            Assert.False(voiceOverride.RemovePlayer(player1));
        }

        [Test]
        public void VoiceOverride_AffectedPlayers_PlayerCanBeAddedToAndRemovedFromOverride()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            UdonTestUtils.CreateLocalPlayer(0);
            var player1 = UdonTestUtils.CreatePlayer(1);

            // add
            Assert.True(voiceOverride.AddPlayer(player1));
            Assert.AreEqual(1, voiceOverride.GetAffectedPlayers().Length);
            Assert.True(voiceOverride.IsAffected(player1));
            Assert.AreEqual(player1.playerId, voiceOverride.GetAffectedPlayers()[0]);

            // remove
            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            Assert.True(voiceOverride.RemovePlayer(player1));
            Assert.AreEqual(0, voiceOverride.GetAffectedPlayers().Length);
            Assert.False(voiceOverride.IsAffected(player1));
        }

        [Test]
        public void VoiceOverride_AffectedPlayers_MultiplePlayersCanBeAddedToAndRemovedFromOverride()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            UdonTestUtils.CreateLocalPlayer(0);
            var player1 = UdonTestUtils.CreatePlayer(1);
            var player2 = UdonTestUtils.CreatePlayer(2);

            Assert.True(voiceOverride.AddPlayer(player1));
            Assert.AreEqual(1, voiceOverride.GetAffectedPlayers().Length);
            Assert.AreEqual(player1.playerId, voiceOverride.GetAffectedPlayers()[0]);

            Assert.True(voiceOverride.AddPlayer(player2));
            Assert.AreEqual(2, voiceOverride.GetAffectedPlayers().Length);
            Assert.AreEqual(player1.playerId, voiceOverride.GetAffectedPlayers()[0]);
            Assert.AreEqual(player2.playerId, voiceOverride.GetAffectedPlayers()[1]);

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            Assert.True(voiceOverride.RemovePlayer(player1));
            Assert.AreEqual(1, voiceOverride.GetAffectedPlayers().Length);
            Assert.AreEqual(player2.playerId, voiceOverride.GetAffectedPlayers()[0]);

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            Assert.True(voiceOverride.RemovePlayer(player2));
            Assert.AreEqual(0, voiceOverride.GetAffectedPlayers().Length);

            Assert.True(voiceOverride.AddPlayer(player1));
            Assert.True(voiceOverride.AddPlayer(player2));
            Assert.AreEqual(2, voiceOverride.GetAffectedPlayers().Length);
            Assert.AreEqual(player1.playerId, voiceOverride.GetAffectedPlayers()[0]);
            Assert.AreEqual(player2.playerId, voiceOverride.GetAffectedPlayers()[1]);
            
            LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            Assert.True(voiceOverride.RemovePlayer(player1));
            LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            Assert.True(voiceOverride.RemovePlayer(player2));
            Assert.AreEqual(0, voiceOverride.GetAffectedPlayers().Length);
        }

        [Test]
        public void VoiceOverride_AffectedPlayers_MultiplePlayersAddedToOverrideAreSorted()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var player0 = UdonTestUtils.CreateLocalPlayer(0);
            var player1 = UdonTestUtils.CreatePlayer(1);
            var player2 = UdonTestUtils.CreatePlayer(2);
            var player3 = UdonTestUtils.CreatePlayer(3);

            Assert.True(voiceOverride.AddPlayer(player1));
            Assert.True(voiceOverride.AddPlayer(player2));
            Assert.True(voiceOverride.AddPlayer(player3));
            
            
            Assert.AreEqual(3, voiceOverride.GetAffectedPlayers().Length);
            Assert.AreEqual(player1.playerId, voiceOverride.GetAffectedPlayers()[0]);
            Assert.AreEqual(player2.playerId, voiceOverride.GetAffectedPlayers()[1]);
            Assert.AreEqual(player3.playerId, voiceOverride.GetAffectedPlayers()[2]);

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            Assert.True(voiceOverride.RemovePlayer(player1));
            Assert.AreEqual(2, voiceOverride.GetAffectedPlayers().Length);
            Assert.AreEqual(player2.playerId, voiceOverride.GetAffectedPlayers()[0]);
            Assert.AreEqual(player3.playerId, voiceOverride.GetAffectedPlayers()[1]);
        }

        [Test]
        public void BetterPlayerAudio_IgnorePlayer_PlayersCanBeIgnored()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var player1 = UdonTestUtils.CreatePlayer(0);

            Assert.False(betterPlayerAudio.IsIgnored(player1));

            betterPlayerAudio.IgnorePlayer(player1);
            Assert.True(betterPlayerAudio.IsIgnored(player1));

            betterPlayerAudio.UnIgnorePlayer(player1);
            Assert.False(betterPlayerAudio.IsIgnored(player1));
        }

        [Test]
        public void BetterPlayerAudio_IgnorePlayer_IgnoredPlayersDontUseGlobalSettings()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var player1 = UdonTestUtils.CreatePlayer(0);

            Assert.True(betterPlayerAudio.UsesDefaultEffects(player1));

            betterPlayerAudio.IgnorePlayer(player1);
            Assert.False(betterPlayerAudio.UsesDefaultEffects(player1));

            betterPlayerAudio.UnIgnorePlayer(player1);
            Assert.True(betterPlayerAudio.UsesDefaultEffects(player1));
        }

        [Test]
        public void VoiceOverride_UsesDefaultEffects_PlayersWithOverrideDontUseGlobalSettings()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            UdonTestUtils.CreateLocalPlayer(0);
            var player1 = UdonTestUtils.CreatePlayer(1);

            Assert.True(betterPlayerAudio.UsesDefaultEffects(player1));

            voiceOverride.AddPlayer(player1);
            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            Assert.False(betterPlayerAudio.UsesDefaultEffects(player1));

            voiceOverride.RemovePlayer(player1);
            Assert.True(betterPlayerAudio.UsesDefaultEffects(player1));
        }

        [Test]
        public void BetterPlayerAudio_HasVoiceOverrides_PlayerWithOverrideHasOverrides()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            UdonTestUtils.CreateLocalPlayer(0);
            var player1 = UdonTestUtils.CreatePlayer(1);

            LogAssert.Expect(LogType.Error, new Regex(".+Player invalid.", RegexOptions.Singleline));
            Assert.False(betterPlayerAudio.HasVoiceOverrides(null));
            Assert.False(betterPlayerAudio.HasVoiceOverrides(player1));

            voiceOverride.AddPlayer(player1);

            LogAssert.Expect(LogType.Error, new Regex(".+Player invalid.", RegexOptions.Singleline));
            Assert.False(betterPlayerAudio.HasVoiceOverrides(null));
            Assert.True(betterPlayerAudio.HasVoiceOverrides(player1));

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            voiceOverride.RemovePlayer(player1);

            LogAssert.Expect(LogType.Error, new Regex(".+Player invalid.", RegexOptions.Singleline));
            Assert.False(betterPlayerAudio.HasVoiceOverrides(null));
            Assert.False(betterPlayerAudio.HasVoiceOverrides(player1));
        }

        [Test]
        public void BetterPlayerAudio_HasOverrides_IgnoredPlayersWithOverrideStayIgnored()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            UdonTestUtils.CreateLocalPlayer(0);
            var player1 = UdonTestUtils.CreatePlayer(1);

            Assert.False(betterPlayerAudio.HasVoiceOverrides(player1));
            Assert.False(betterPlayerAudio.UsesVoiceOverride(player1));
            Assert.True(betterPlayerAudio.UsesDefaultEffects(player1));

            betterPlayerAudio.IgnorePlayer(player1);
            voiceOverride.AddPlayer(player1);
            Assert.True(betterPlayerAudio.HasVoiceOverrides(player1));
            Assert.False(betterPlayerAudio.UsesVoiceOverride(player1));
            Assert.False(betterPlayerAudio.UsesDefaultEffects(player1));

            betterPlayerAudio.UnIgnorePlayer(player1);
            Assert.True(betterPlayerAudio.HasVoiceOverrides(player1));
            Assert.True(betterPlayerAudio.UsesVoiceOverride(player1));
            Assert.False(betterPlayerAudio.UsesDefaultEffects(player1));

            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            voiceOverride.RemovePlayer(player1);
            Assert.False(betterPlayerAudio.HasVoiceOverrides(player1));
            Assert.False(betterPlayerAudio.UsesVoiceOverride(player1));
            Assert.True(betterPlayerAudio.UsesDefaultEffects(player1));
        }

        [Test]
        public void BetterPlayerAudio_CreateOverrideSlotForPlayer()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var player1 = UdonTestUtils.CreatePlayer(0);
            var player3 = UdonTestUtils.CreatePlayer(1);
            var player2 = UdonTestUtils.CreatePlayer(2);

            Assert.AreEqual(0, betterPlayerAudio.PlayersWithOverridesCount());
            var ids = betterPlayerAudio.GetPlayersWithOverrides();
            Assert.AreEqual(0, ids.Length);
            Assert.AreEqual(1, betterPlayerAudio.CreateOverrideSlotForPlayer(player1));
            ids = betterPlayerAudio.GetPlayersWithOverrides();
            Assert.AreEqual(1, ids.Length);
            Assert.AreEqual(0, ids[0]);
            Assert.AreEqual(1, betterPlayerAudio.PlayersWithOverridesCount());
            Assert.AreEqual(2, betterPlayerAudio.CreateOverrideSlotForPlayer(player2));
            ids = betterPlayerAudio.GetPlayersWithOverrides();
            Assert.AreEqual(2, ids.Length);
            Assert.AreEqual(0, ids[0]);
            Assert.AreEqual(2, ids[1]);
            Assert.AreEqual(2, betterPlayerAudio.PlayersWithOverridesCount());
            Assert.AreEqual(3, betterPlayerAudio.CreateOverrideSlotForPlayer(player3));
            Assert.AreEqual(3, betterPlayerAudio.PlayersWithOverridesCount());
            ids = betterPlayerAudio.GetPlayersWithOverrides();
            Assert.AreEqual(3, ids.Length);
            Assert.AreEqual(0, ids[0]);
            Assert.AreEqual(1, ids[1]);
            Assert.AreEqual(2, ids[2]);
        }

        [Test]
        public void BetterPlayerAudio_OverridePlayerSettings()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var player1 = UdonTestUtils.CreatePlayer(0);

            Assert.False(betterPlayerAudio.OverridePlayerSettings(null, null));
            Assert.False(betterPlayerAudio.OverridePlayerSettings(voiceOverride, null));
            Assert.False(betterPlayerAudio.OverridePlayerSettings(null, player1));

            Assert.True(betterPlayerAudio.OverridePlayerSettings(voiceOverride, player1));
        }

        [Test]
        public void BetterPlayerAudio_GetMaxPriorityOverride_PlayerWithOverrideHasHighPriorityOverride()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var player1 = UdonTestUtils.CreateLocalPlayer(0);
            var player2 = UdonTestUtils.CreatePlayer(1);

            Assert.IsNull(betterPlayerAudio.GetMaxPriorityOverride(null));
            Assert.IsNull(betterPlayerAudio.GetMaxPriorityOverride(player2));
            Assert.True(voiceOverride.AddPlayer(player2));
            Assert.True(voiceOverride.IsAffected(player2));
            Assert.AreEqual(1, voiceOverride.GetAffectedPlayers().Length);
            Assert.AreEqual(voiceOverride, betterPlayerAudio.GetMaxPriorityOverride(player2));
            LogAssert.Expect(LogType.Error,
                "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nAlso think twice if you really want to destroy something in edit mode. Since this will destroy objects permanently.");
            Assert.True(voiceOverride.RemovePlayer(player2));
            Assert.IsNull(betterPlayerAudio.GetMaxPriorityOverride(player2));
            Assert.False(voiceOverride.IsAffected(player2));
            Assert.AreEqual(0, voiceOverride.GetAffectedPlayers().Length);
            
            Assert.True(voiceOverride.AddPlayer(player1));
            Assert.AreEqual(1, voiceOverride.GetAffectedPlayers().Length);
            Assert.AreEqual(voiceOverride, betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.AreEqual(voiceOverride, betterPlayerAudio.localPlayerOverrideList.Get(0));
        }

        [Test]
        public void BetterPlayerAudio_GetMaxPriorityOverride_HighPriorityOverrideIsMaxPriorityOverride()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);

            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            UdonTestUtils.CreateLocalPlayer(0);
            var player1 = UdonTestUtils.CreatePlayer(1);

            voiceOverride1.priority = 1;
            voiceOverride2.priority = 2;

            voiceOverride1.AddPlayer(player1);
            voiceOverride2.AddPlayer(player1);
            Assert.AreNotEqual(voiceOverride1, betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.AreEqual(voiceOverride2, betterPlayerAudio.GetMaxPriorityOverride(player1));
        }

        [Test]
        public void BetterPlayerAudio_OtherPlayerWithOverrideCanBeHeard()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);

            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride3 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            UdonTestUtils.CreatePlayer(0);
            var player2 = UdonTestUtils.CreatePlayer(1);

            voiceOverride1.priority = 0;
            voiceOverride1.privacyChannelId = 0;
            voiceOverride2.priority = 2;
            voiceOverride1.privacyChannelId = -1;
            voiceOverride3.priority = 1;
            voiceOverride1.privacyChannelId = 1;

            voiceOverride1.AddPlayer(player2);
            Assert.AreEqual(voiceOverride1, betterPlayerAudio.GetMaxPriorityOverride(player2));
            voiceOverride2.AddPlayer(player2);
            Assert.AreEqual(voiceOverride2, betterPlayerAudio.GetMaxPriorityOverride(player2));
            voiceOverride3.AddPlayer(player2);
            Assert.AreEqual(voiceOverride2, betterPlayerAudio.GetMaxPriorityOverride(player2));
            Assert.True(betterPlayerAudio.OtherPlayerWithOverrideCanBeHeard(voiceOverride2, false, -1, false));
        }

        [Test]
        public void BetterPlayerAudio_GetMaxPriorityOverride_LowerPriorityOverrideIsNotMaxPriorityOverride()
        {
            var go = new GameObject();

            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            UdonTestUtils.CreateLocalPlayer(0);
            var player1 = UdonTestUtils.CreatePlayer(1);

            voiceOverride1.priority = 1;
            voiceOverride2.priority = 2;

            voiceOverride2.AddPlayer(player1);
            voiceOverride1.AddPlayer(player1);
            Assert.AreNotEqual(voiceOverride1, betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.AreEqual(voiceOverride2, betterPlayerAudio.GetMaxPriorityOverride(player1));
        }

        [Test]
        public void ListObject_AddOverride_CreateOverrideListForPlayer()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            Assert.True(Utilities.IsValid(listObject));
        }

        [Test]
        public void ListObject_AddOverride_OverrideCanBeAddedToList()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.False(listObject.AddOverride(null));
        }

        [Test]
        public void ListObject_AddOverride_SingleOverrideCanBeRetrieved()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            Assert.AreEqual(null, listObject.Get(0));
            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.AreEqual(voiceOverride1, listObject.Get(0));
            Assert.AreEqual(null, listObject.Get(1));
        }

        [Test]
        public void ListObject_AddOverride_MultipleOverrideWithSamePriorityCanBeAddedAndRetrieved()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.AreEqual(voiceOverride2, listObject.Get(0));
            Assert.AreEqual(voiceOverride1, listObject.Get(1));
        }

        [Test]
        public void ListObject_AddOverride_SameOverrideWithSamePriorityCanBeAddedMultipleTimesAndRetrieved()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.AreEqual(voiceOverride1, listObject.Get(0));
            Assert.AreEqual(voiceOverride2, listObject.Get(1));
            Assert.AreEqual(null, listObject.Get(2));
        }

        [Test]
        public void ListObject_AddOverride_LowerPriorityIsNotAtFirstPositionWhenAddedAfterHighPriority()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            voiceOverride1.priority = 0;
            voiceOverride2.priority = 1;

            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.True(listObject.AddOverride(voiceOverride1));

            Assert.AreEqual(voiceOverride2, listObject.Get(0));
            Assert.AreEqual(voiceOverride1, listObject.Get(1));
        }

        [Test]
        public void ListObject_CopyHighPriorityOverridesToNewList_TestCopyingNoOverridesToNewList()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var originalList = new BetterPlayerAudioOverride[0];
            var newList = new BetterPlayerAudioOverride[0];

            Assert.AreEqual(0, listObject.GetInsertIndex(originalList, voiceOverride1));
            Assert.AreEqual(0, listObject.CopyHighPriorityOverridesToNewList(originalList, newList, 0));
        }

        [Test]
        public void ListObject_CopyHighPriorityOverridesToNewList_TestCopying1OverrideToNewList()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            voiceOverride2.priority = -1;
            var originalList = new[] {voiceOverride1};
            var newList = new BetterPlayerAudioOverride[1];

            Assert.AreEqual(1, listObject.GetInsertIndex(originalList, voiceOverride2));
            Assert.AreEqual(1, listObject.CopyHighPriorityOverridesToNewList(originalList, newList, 1));
            Assert.AreEqual(voiceOverride1, newList[0]);
        }


        [Test]
        public void ListObject_CopyHighPriorityOverridesToNewList_TestCopyingValidOverridesToNewList()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride3 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            voiceOverride1.priority = 2;
            voiceOverride2.priority = 1;
            voiceOverride3.priority = 0;

            var originalList = new[] {voiceOverride1, voiceOverride2};
            var newList = new BetterPlayerAudioOverride[2];

            Assert.AreEqual(2, listObject.GetInsertIndex(originalList, voiceOverride3));
            Assert.AreEqual(2, listObject.CopyHighPriorityOverridesToNewList(originalList, newList, 2));
            Assert.AreEqual(voiceOverride1, newList[0]);
            Assert.AreEqual(voiceOverride2, newList[1]);
        }

        [Test]
        public void ListObject_CopyHighPriorityOverridesToNewList_TestCopyingValidWithInvalidInBetweenToNewList()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride3 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            voiceOverride1.priority = 2;
            voiceOverride2.priority = 1;
            voiceOverride3.priority = 0;

            var originalList = new[] {voiceOverride1, null, voiceOverride2};
            var newList = new BetterPlayerAudioOverride[3];

            Assert.AreEqual(2, listObject.GetInsertIndex(originalList, voiceOverride3));
            Assert.AreEqual(3, listObject.CopyHighPriorityOverridesToNewList(originalList, newList, 2));
            Assert.AreEqual(voiceOverride1, newList[0]);
            Assert.AreEqual(voiceOverride2, newList[1]);
            Assert.AreEqual(null, newList[2]);
        }

        [Test]
        public void ListObject_AddOverride_AddSingleOverride()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            voiceOverride1.priority = 0;
            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.AreEqual(voiceOverride1, listObject.Get(0));
            Assert.AreEqual(null, listObject.Get(1));
        }

        [Test]
        public void ListObject_AddOverride_AddSingleOverridesTwice()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            voiceOverride1.priority = 0;
            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.AreEqual(voiceOverride1, listObject.Get(0));
            Assert.AreEqual(null, listObject.Get(1));
        }

        [Test]
        public void ListObject_AddOverride_AddTwoOverrides()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.AreEqual(voiceOverride1, listObject.Get(0));
            Assert.AreEqual(null, listObject.Get(1));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.AreEqual(voiceOverride2, listObject.Get(0));
            Assert.AreEqual(voiceOverride1, listObject.Get(1));
            Assert.AreEqual(null, listObject.Get(2));
        }

        [Test]
        public void ListObject_InsertNewOverride_InsertAddedOverride()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            var originalList = new BetterPlayerAudioOverride[0];
            var tempList = new BetterPlayerAudioOverride[0];
            tempList = listObject.InsertNewOverride(voiceOverride1, 0, tempList, originalList);
            Assert.AreEqual(1, tempList.Length);
            Assert.AreEqual(voiceOverride1, tempList[0]);

            originalList = new BetterPlayerAudioOverride[1];
            tempList = new BetterPlayerAudioOverride[1];
            tempList = listObject.InsertNewOverride(voiceOverride1, 0, tempList, originalList);
            Assert.AreEqual(1, tempList.Length);
            Assert.AreEqual(voiceOverride1, tempList[0]);

            originalList = new[] {voiceOverride1};
            tempList = new BetterPlayerAudioOverride[1];
            tempList = listObject.InsertNewOverride(voiceOverride1, 0, tempList, originalList);
            Assert.AreEqual(1, tempList.Length);
            Assert.AreEqual(voiceOverride1, tempList[0]);

            originalList = new BetterPlayerAudioOverride[3];
            tempList = new BetterPlayerAudioOverride[3];
            tempList = listObject.InsertNewOverride(voiceOverride1, 1, tempList, originalList);
            Assert.AreEqual(3, tempList.Length);
            Assert.AreEqual(null, tempList[0]);
            Assert.AreEqual(voiceOverride1, tempList[1]);
            Assert.AreEqual(null, tempList[2]);

            originalList = new BetterPlayerAudioOverride[2];
            tempList = new BetterPlayerAudioOverride[2];
            tempList = listObject.InsertNewOverride(voiceOverride1, 2, tempList, originalList);
            Assert.AreEqual(3, tempList.Length);
            Assert.AreEqual(null, tempList[0]);
            Assert.AreEqual(null, tempList[1]);
            Assert.AreEqual(voiceOverride1, tempList[2]);
        }

        [Test]
        public void ListObject_AddOverride_LowerPriorityIsAtSecondPositionWhenAddedAfterHighPriorityAndSamePriority()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride3 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            voiceOverride1.priority = 0;
            voiceOverride2.priority = 1;
            voiceOverride3.priority = 0;

            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.True(listObject.AddOverride(voiceOverride3));

            Assert.AreEqual(voiceOverride2, listObject.Get(0));
            Assert.AreEqual(voiceOverride3, listObject.Get(1));
            Assert.AreEqual(voiceOverride1, listObject.Get(2));
        }


        [Test]
        public void ListObject_GetInsertIndex_InsertPositionOfInvalidParametersIsNegative1()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            var overrides = new BetterPlayerAudioOverride[0];
            Assert.AreEqual(-1, listObject.GetInsertIndex(null, null));
            Assert.AreEqual(-1, listObject.GetInsertIndex(null, voiceOverride1));
            Assert.AreEqual(-1, listObject.GetInsertIndex(overrides, null));
        }

        [Test]
        public void ListObject_GetInsertIndex_EmptyOverrideArrayHasInsertIndex0()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            var overrides = new BetterPlayerAudioOverride[0];
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, voiceOverride1));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithInvalidOverrideHasInsertIndex0()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            var overrides = new BetterPlayerAudioOverride[1];
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, voiceOverride1));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithSameOverrideHasInsertIndex0()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            var overrides = new[] {voiceOverride1};
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, voiceOverride1));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithDifferentOverrideButSamePriorityHasInsertIndex0()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            var overrides = new[] {voiceOverride1};
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithDifferentOverrideButSameDifferentPriorityHasInsertIndex0()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            voiceOverride1.priority = 1;
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            voiceOverride2.priority = 1;

            var overrides = new[] {voiceOverride1};
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithDifferentOverrideButHigherPriorityHasInsertIndex0()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            voiceOverride1.priority = 0;
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            voiceOverride2.priority = 1;

            var overrides = new[] {voiceOverride1};
            Assert.AreEqual(0, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithDifferentOverrideButLowerPriorityHasInsertIndex1()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            voiceOverride1.priority = 1;
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            var overrides = new[] {voiceOverride1};
            Assert.AreEqual(1, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_GetInsertIndex_ArrayWithValidAndInvalidOverrideButLowerPriorityHasInsertIndex1()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            voiceOverride1.priority = 1;
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            var overrides = new[] {voiceOverride1, null};
            Assert.AreEqual(1, listObject.GetInsertIndex(overrides, voiceOverride2));
        }

        [Test]
        public void ListObject_Consolidate()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride3 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            Assert.AreEqual(0, listObject.Consolidate(null));

            var list = new BetterPlayerAudioOverride[0];
            Assert.AreEqual(0, listObject.Consolidate(list));
            Assert.AreEqual(0, list.Length);

            list = new BetterPlayerAudioOverride[] {null};
            Assert.AreEqual(0, listObject.Consolidate(list));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(null, list[0]);

            list = new[] {voiceOverride1};
            Assert.AreEqual(1, listObject.Consolidate(list));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(voiceOverride1, list[0]);

            list = new[] {voiceOverride1, null};
            Assert.AreEqual(1, listObject.Consolidate(list));
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(voiceOverride1, list[0]);
            Assert.AreEqual(null, list[1]);

            list = new[] {null, voiceOverride1};
            Assert.AreEqual(1, listObject.Consolidate(list));
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(voiceOverride1, list[0]);
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

            list = new[] {voiceOverride1, null, voiceOverride2};
            Assert.AreEqual(2, listObject.Consolidate(list));
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(voiceOverride1, list[0]);
            Assert.AreEqual(voiceOverride2, list[1]);
            Assert.AreEqual(null, list[2]);

            list = new[] {null, null, voiceOverride2};
            Assert.AreEqual(1, listObject.Consolidate(list));
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(voiceOverride2, list[0]);
            Assert.AreEqual(null, list[1]);
            Assert.AreEqual(null, list[2]);

            list = new[] {null, voiceOverride1, voiceOverride2};
            Assert.AreEqual(2, listObject.Consolidate(list));
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(voiceOverride1, list[0]);
            Assert.AreEqual(voiceOverride2, list[1]);
            Assert.AreEqual(null, list[2]);

            list = new[] {voiceOverride1, voiceOverride2, voiceOverride3};
            Assert.AreEqual(3, listObject.Consolidate(list));
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(voiceOverride1, list[0]);
            Assert.AreEqual(voiceOverride2, list[1]);
            Assert.AreEqual(voiceOverride3, list[2]);
        }

        [Test]
        public void ListObject_Remove()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            Assert.False(listObject.Remove(null, null));

            var list = new BetterPlayerAudioOverride[0];
            Assert.False(listObject.Remove(list, null));
            Assert.False(listObject.Remove(list, voiceOverride1));
            Assert.AreEqual(0, list.Length);

            list = new BetterPlayerAudioOverride[1];
            Assert.False(listObject.Remove(list, voiceOverride1));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(null, list[0]);

            list = new[] {voiceOverride2};
            Assert.False(listObject.Remove(list, voiceOverride1));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(voiceOverride2, list[0]);

            list = new[] {voiceOverride1};
            Assert.True(listObject.Remove(list, voiceOverride1));
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(null, list[0]);

            list = new[] {voiceOverride1, voiceOverride1};
            Assert.True(listObject.Remove(list, voiceOverride1));
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(null, list[0]);
            Assert.AreEqual(null, list[1]);

            list = new[] {voiceOverride2, voiceOverride1};
            Assert.True(listObject.Remove(list, voiceOverride2));
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(null, list[0]);
            Assert.AreEqual(voiceOverride1, list[1]);
        }

        [Test]
        public void ListObject_RemoveOverride()
        {
            var go = new GameObject();
            var listObject = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudio = CreateBetterPlayerAudio(go);
            var voiceOverride1 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);
            var voiceOverride2 = CreateBetterPlayerVoiceOverride(go, betterPlayerAudio);

            Assert.AreEqual(0, listObject.RemoveOverride(null));
            Assert.AreEqual(0, listObject.RemoveOverride(voiceOverride1));

            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.AreEqual(0, listObject.RemoveOverride(voiceOverride1));
            Assert.AreEqual(null, listObject.Get(0));

            Assert.True(listObject.AddOverride(voiceOverride1));
            Assert.True(listObject.AddOverride(voiceOverride2));
            Assert.AreEqual(1, listObject.RemoveOverride(voiceOverride1));
            Assert.AreEqual(voiceOverride2, listObject.Get(0));
        }

        #region Test Utils

        public static BetterPlayerAudioOverride CreateBetterPlayerVoiceOverride(GameObject go,
            BetterPlayerAudio betterPlayerAudio)
        {
            var voiceOverride = go.AddComponent<BetterPlayerAudioOverride>();
            Assert.True(Utilities.IsValid(voiceOverride));

            voiceOverride.betterPlayerAudio = betterPlayerAudio;
            Assert.True(Utilities.IsValid(voiceOverride.betterPlayerAudio));
            
            voiceOverride.udonDebug = go.AddComponent<UdonDebug>();
            Assert.True(Utilities.IsValid(voiceOverride.udonDebug));
            return voiceOverride;
        }

        public static BetterPlayerAudio CreateBetterPlayerAudio(GameObject go)
        {
            var betterPlayerAudio = go.AddComponent<BetterPlayerAudio>();
            Assert.True(Utilities.IsValid(betterPlayerAudio));

            var udonDebug = go.AddComponent<UdonDebug>();
            Assert.True(Utilities.IsValid(betterPlayerAudio));
            betterPlayerAudio.udonDebug = udonDebug;

            var listPrefab = new GameObject("ListPrefab");
            betterPlayerAudio.cloneablePlayerList = listPrefab.AddComponent<BetterPlayerAudioOverrideList>();;

            var localList = new GameObject("LocalPlayerOverrideList");
            betterPlayerAudio.localPlayerOverrideList = localList.AddComponent<BetterPlayerAudioOverrideList>();

            betterPlayerAudio.OnEnable();
            betterPlayerAudio.Start();

            return betterPlayerAudio;
        }

        #endregion
    }
}