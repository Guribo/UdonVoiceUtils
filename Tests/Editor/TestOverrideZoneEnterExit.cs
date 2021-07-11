using System.Collections.Generic;
using System.Text.RegularExpressions;
using Guribo.UdonBetterAudio.Runtime.Examples;
using Guribo.UdonUtils.Runtime.Common;
using Guribo.UdonUtils.Runtime.Common.Networking;
using Guribo.UdonUtils.Tests.Editor.Utils;
using NUnit.Framework;
using UdonSharp;
using UdonSharpEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Tests.Editor
{
    public class TestOverrideZoneEnterExit
    {
        [Test]
        public void OverrideZoneActivator_Interact()
        {
            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            VRCPlayerApi._IsOwner = (api, o) => true;
            var player1 = UdonTestUtils.CreateLocalPlayer(0);

            var overrideZoneActivatorGameobject = new GameObject();
            var syncedIntegerArrayGameObject = new GameObject();
            var playerListGameobject = new GameObject();
            var betterPlayerAudioGameobject = new GameObject();
            var betterPlayerVoiceOverrideGameobject = new GameObject();
            var udonDebug = new GameObject();
            var enterButtonGo = new GameObject();

            var betterPlayerAudio = TestBetterPlayerAudio.CreateBetterPlayerAudio(betterPlayerAudioGameobject);
            var voiceOverride =
                TestBetterPlayerAudio.CreateBetterPlayerVoiceOverride(betterPlayerVoiceOverrideGameobject,
                    betterPlayerAudio);
            voiceOverride.betterPlayerAudio = betterPlayerAudio;

            var voiceOverrideRoom = overrideZoneActivatorGameobject.AddUdonSharpComponent<VoiceOverrideRoom>();
            Assert.True(Utilities.IsValid(voiceOverrideRoom));

            voiceOverrideRoom.playerOverride = voiceOverride;

            voiceOverride.playerList = playerListGameobject.AddUdonSharpComponent<PlayerList>();
           

            voiceOverrideRoom.syncedIntegerArray = syncedIntegerArrayGameObject.AddComponent<SyncedIntegerArray>();
            voiceOverrideRoom.syncedIntegerArray.targetBehaviour = voiceOverride.playerList;
            voiceOverrideRoom.syncedIntegerArray.changeEventListeners = new[] {(UdonSharpBehaviour)voiceOverrideRoom};
            voiceOverrideRoom.syncedIntegerArray.targetChangeEvent = nameof(VoiceOverrideRoom.RefreshPlayersInZone);
            voiceOverrideRoom.syncedIntegerArray.targetVariable = nameof(PlayerList.players);
            
            voiceOverrideRoom.syncedIntegerArray.udonDebug = udonDebug.AddComponent<UdonDebug>();
            voiceOverrideRoom.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            voiceOverride.playerList.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            
            var enterButton = enterButtonGo.AddComponent<VoiceOverrideRoomEnterButton>();
            enterButton.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            enterButton.voiceOverrideRoom = voiceOverrideRoom;

            // no player yet in zone
            Assert.False(voiceOverride.playerList.Contains(player1));
            Assert.False(voiceOverrideRoom.Contains(player1));
            Assert.IsNull(betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.False(voiceOverrideRoom.IsInZone);

            
            // local player enters
            enterButton.Interact();
            Assert.True(voiceOverrideRoom.IsInZone);
            Assert.True(voiceOverride.playerList.Contains(player1));
            Assert.True(voiceOverrideRoom.Contains(player1));
            Assert.AreEqual(voiceOverride, betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.AreEqual(voiceOverrideRoom.syncedIntegerArray.syncedValue, voiceOverride.playerList.players);

            // try adding player again
            LogAssert.Expect(LogType.Error, new Regex(".+Player 0 already in list.", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Error, new Regex(".+player already affected.", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Error, new Regex(".+Adding player to player list failed.", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Error, new Regex(".+Failed adding player to zone.", RegexOptions.Singleline));
            voiceOverrideRoom.EnterRoom(Networking.LocalPlayer, null);
            enterButton.Interact();
            voiceOverrideRoom.syncedIntegerArray.syncedValue = new int[voiceOverride.playerList.players.Length];
            voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);

            Assert.True(voiceOverride.playerList.Contains(player1));
            Assert.True(voiceOverrideRoom.IsInZone);
            Assert.True(voiceOverrideRoom.Contains(player1));
            Assert.AreEqual(1, voiceOverrideRoom.syncedIntegerArray.syncedValue.Length);

            Assert.AreEqual(voiceOverride.playerList.players,
                voiceOverrideRoom.syncedIntegerArray.syncedValue);
            Assert.AreEqual(player1.playerId, voiceOverrideRoom.syncedIntegerArray.syncedValue[0]);
            Assert.AreEqual(voiceOverride, betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.AreEqual(voiceOverride, betterPlayerAudio.localPlayerOverrideList.Get(0));
        }

        [Test]
        public void OverrideZoneExit_InteractAndRespawn()
        {
            #region Test init

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            VRCPlayerApi._IsOwner = (api, o) => true;
            var player1 = UdonTestUtils.CreateLocalPlayer(0);

            var overrideZoneActivatorGameobject = new GameObject();
            var overrideZoneExitGameobject = new GameObject();
            var syncedIntegerArrayGameObject = new GameObject();
            var playerListGameobject = new GameObject();
            var betterPlayerAudioGameobject = new GameObject();
            var betterPlayerVoiceOverrideGameobject = new GameObject();
            var udonDebug = new GameObject();

            var betterPlayerAudio = TestBetterPlayerAudio.CreateBetterPlayerAudio(betterPlayerAudioGameobject);
            var voiceOverride =
                TestBetterPlayerAudio.CreateBetterPlayerVoiceOverride(betterPlayerVoiceOverrideGameobject,
                    betterPlayerAudio);

            var voiceOverrideRoom = overrideZoneActivatorGameobject.AddUdonSharpComponent<VoiceOverrideRoom>();
            var overrideZoneExit = overrideZoneExitGameobject.AddUdonSharpComponent<VoiceOverrideRoomExitButton>();

            voiceOverride.betterPlayerAudio = betterPlayerAudio;

            voiceOverride.playerList = playerListGameobject.AddUdonSharpComponent<PlayerList>();

            voiceOverrideRoom.syncedIntegerArray =
                syncedIntegerArrayGameObject.AddUdonSharpComponent<SyncedIntegerArray>();
            voiceOverrideRoom.syncedIntegerArray.targetBehaviour = voiceOverride.playerList;
            voiceOverrideRoom.syncedIntegerArray.changeEventListeners = new[] {(UdonSharpBehaviour)voiceOverrideRoom};
            voiceOverrideRoom.syncedIntegerArray.targetChangeEvent = nameof(VoiceOverrideRoom.RefreshPlayersInZone);
            voiceOverrideRoom.syncedIntegerArray.targetVariable = nameof(PlayerList.players);
            
            voiceOverrideRoom.syncedIntegerArray.udonDebug = udonDebug.AddComponent<UdonDebug>();
            voiceOverrideRoom.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            voiceOverrideRoom.playerOverride = voiceOverride;
            voiceOverrideRoom.playerOverride.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            voiceOverride.playerList.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;

            overrideZoneExit.voiceOverrideRoom = voiceOverrideRoom;
            overrideZoneExit.exitZoneOnRespawn = true;
            overrideZoneExit.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            
            var enterButtonGo = new GameObject();
            var enterButton = enterButtonGo.AddComponent<VoiceOverrideRoomEnterButton>();
            enterButton.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            enterButton.voiceOverrideRoom = voiceOverrideRoom;

            #endregion

            // no player override added yet
            Assert.AreEqual(0, voiceOverride.playerList.DiscardInvalid());
            Assert.Null(betterPlayerAudio.GetMaxPriorityOverride(player1));

            // add single override to local player
            enterButton.Interact();

            // check single player added to player list
            Assert.True(voiceOverride.playerList.Contains(player1));

            // synced array contains player id

            #region TEMP

            // TODO remove temp workaround
            voiceOverrideRoom.syncedIntegerArray.syncedValue =
                new int[voiceOverride.playerList.players.Length];
            voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);
            voiceOverrideRoom.RefreshPlayersInZone();

            #endregion

            Assert.NotNull(voiceOverrideRoom.syncedIntegerArray);
            Assert.NotNull(voiceOverrideRoom.syncedIntegerArray.syncedValue);
            Assert.AreEqual(1, voiceOverrideRoom.syncedIntegerArray.syncedValue.Length);
            Assert.AreEqual(player1.playerId, voiceOverrideRoom.syncedIntegerArray.syncedValue[0]);

            // check single player added to override
            Assert.True(voiceOverride.IsAffected(player1));

            // better player audio contains local player in local override list
            Assert.AreEqual(voiceOverride, betterPlayerAudio.localPlayerOverrideList.Get(0));

            // better player audio returns override for local player
            Assert.AreEqual(voiceOverride, betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.True(betterPlayerAudio.HasVoiceOverrides(player1));

            // remove player from override
            overrideZoneExit.Interact();

            // check single player added to player list
            Assert.False(voiceOverride.playerList.Contains(player1));

            // synced array contains player id

            #region TEMP

            // TODO remove temp workaround
            voiceOverrideRoom.syncedIntegerArray.syncedValue =
                new int[voiceOverride.playerList.players.Length];
            voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);
            voiceOverrideRoom.RefreshPlayersInZone();

            #endregion

            Assert.NotNull(voiceOverrideRoom.syncedIntegerArray);
            Assert.NotNull(voiceOverrideRoom.syncedIntegerArray.syncedValue);
            Assert.AreEqual(0, voiceOverrideRoom.syncedIntegerArray.syncedValue.Length);

            // check single player added to override
            Assert.False(voiceOverride.IsAffected(player1));

            // better player audio contains local player in local override list
            Assert.Null(betterPlayerAudio.localPlayerOverrideList.Get(0));

            // better player audio returns override for local player
            Assert.Null(betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.False(betterPlayerAudio.HasVoiceOverrides(player1));

            overrideZoneExit.exitZoneOnRespawn = false;

            enterButton.Interact();

            #region TEMP

            // TODO remove temp workaround
            voiceOverrideRoom.syncedIntegerArray.syncedValue =
                new int[voiceOverride.playerList.players.Length];
            voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);
            voiceOverrideRoom.RefreshPlayersInZone();

            #endregion

            Assert.AreEqual(1, voiceOverride.playerList.DiscardInvalid());
            Assert.AreEqual(voiceOverride, betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.True(voiceOverrideRoom.IsInZone);

            overrideZoneExit.OnPlayerRespawn(player1);
            Assert.AreEqual(1, voiceOverride.playerList.DiscardInvalid());
            Assert.AreEqual(voiceOverride, betterPlayerAudio.GetMaxPriorityOverride(player1));

            overrideZoneExit.exitZoneOnRespawn = true;

            voiceOverrideRoom.exitZoneOnRespawn = false;
            overrideZoneExit.exitZoneOnRespawn = true;
            overrideZoneExit.OnPlayerRespawn(player1);

            #region TEMP

            // TODO remove temp workaround
            voiceOverrideRoom.syncedIntegerArray.syncedValue =
                new int[voiceOverride.playerList.players.Length];
            voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);
            voiceOverrideRoom.RefreshPlayersInZone();

            #endregion

            Assert.AreEqual(0, voiceOverride.playerList.DiscardInvalid());
            Assert.Null(betterPlayerAudio.GetMaxPriorityOverride(player1));
            Assert.False(voiceOverride.IsAffected(player1));
            Assert.False(voiceOverrideRoom.IsInZone);
        }

        [Test]
        public void VoiceOverrideDoor_Start()
        {
            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            VRCPlayerApi._IsOwner = (api, o) => true;
            var player1 = UdonTestUtils.CreateLocalPlayer(0);

            var door = new GameObject("Door");
            var frontTrigger = new GameObject("FrontTrigger");

            var udonDebug = door.AddComponent<UdonDebug>();

            var voiceOverrideDoor = frontTrigger.AddComponent<VoiceOverrideDoor>();

            voiceOverrideDoor.udonDebug = udonDebug;

            Assert.NotNull(voiceOverrideDoor.gameObject.GetComponent<BoxCollider>());


            LogAssert.Expect(LogType.Error, new Regex(".+Box collider must be a trigger.", RegexOptions.Singleline));
            voiceOverrideDoor.Start();

            voiceOverrideDoor.gameObject.GetComponent<BoxCollider>().center = Random.insideUnitSphere;
            voiceOverrideDoor.gameObject.GetComponent<BoxCollider>().isTrigger = true;

            LogAssert.Expect(LogType.Error, new Regex(".+Box collider center must be 0,0,0.", RegexOptions.Singleline));
            voiceOverrideDoor.Start();
            var ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            Assert.AreEqual(ignoreRaycastLayer, voiceOverrideDoor.gameObject.layer);

            voiceOverrideDoor.gameObject.GetComponent<BoxCollider>().center = Vector3.zero;
            voiceOverrideDoor.Start();
        }

        private Vector3 _localPlayerPosition;

        [Test]
        public void VoiceOverrideDoor_Enter()
        {
            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            VRCPlayerApi._IsOwner = (api, o) => true;
            VRCPlayerApi._GetPosition = player => _localPlayerPosition;
            var player1 = UdonTestUtils.CreateLocalPlayer(0);

            var door = new GameObject("Door");
            var frontTrigger = new GameObject("FrontTrigger");
            var udonDebug = door.AddComponent<UdonDebug>();

            var voiceOverrideDoor = frontTrigger.AddComponent<VoiceOverrideDoor>();
            voiceOverrideDoor.udonDebug = udonDebug;

            Assert.False(voiceOverrideDoor.LocalPlayerInTrigger());

            // enter invalid player
            LogAssert.Expect(LogType.Error, new Regex(".+Invalid player entered.", RegexOptions.Singleline));
            voiceOverrideDoor.OnPlayerTriggerEnter(null);
            Assert.False(voiceOverrideDoor.LocalPlayerInTrigger());

            _localPlayerPosition = Vector3.forward;
            voiceOverrideDoor.OnPlayerTriggerEnter(player1);
            Assert.True(voiceOverrideDoor.LocalPlayerInTrigger());

            _localPlayerPosition = Vector3.back;
            LogAssert.Expect(LogType.Error, new Regex(".+Failed adding player to override.", RegexOptions.Singleline));
            voiceOverrideDoor.OnPlayerTriggerExit(player1);


            _localPlayerPosition = Vector3.back;
            voiceOverrideDoor.OnPlayerTriggerEnter(player1);
            Assert.True(voiceOverrideDoor.LocalPlayerInTrigger());

            _localPlayerPosition = Vector3.back;
            LogAssert.Expect(LogType.Error, new Regex(".+Failed adding player to override.", RegexOptions.Singleline));
            voiceOverrideDoor.OnPlayerTriggerExit(player1);
        }

        [Test]
        public void VoiceOverrideDoor_Exit()
        {
            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            VRCPlayerApi._IsOwner = (api, o) => true;
            VRCPlayerApi._GetPosition = player => _localPlayerPosition;
            var player1 = UdonTestUtils.CreateLocalPlayer(0);

            var door = new GameObject("Door");
            var frontTrigger = new GameObject("FrontTrigger");
            var udonDebug = door.AddComponent<UdonDebug>();

            var voiceOverrideDoor = frontTrigger.AddComponent<VoiceOverrideDoor>();

            voiceOverrideDoor.udonDebug = udonDebug;

            _localPlayerPosition = Vector3.back;
            voiceOverrideDoor.OnPlayerTriggerEnter(player1);

            LogAssert.Expect(LogType.Error, new Regex(".+Invalid player left.", RegexOptions.Singleline));
            voiceOverrideDoor.OnPlayerTriggerExit(null);

            _localPlayerPosition = Vector3.forward;
            LogAssert.Expect(LogType.Error,
                new Regex(".+Failed removing player from override.", RegexOptions.Singleline));
            voiceOverrideDoor.OnPlayerTriggerExit(player1);
            Assert.False(voiceOverrideDoor.LocalPlayerInTrigger());

            _localPlayerPosition = Vector3.forward;
            voiceOverrideDoor.OnPlayerTriggerEnter(player1);

            _localPlayerPosition = Vector3.forward;
            LogAssert.Expect(LogType.Error,
                new Regex(".+Failed removing player from override.", RegexOptions.Singleline));
            voiceOverrideDoor.OnPlayerTriggerExit(player1);
        }

        [Test]
        public void VoiceOverrideDoor_HasEntered()
        {
            var voiceOverrideDoor = new GameObject().AddComponent<VoiceOverrideDoor>();
            Assert.AreEqual(Vector3.forward, voiceOverrideDoor.exitDirection);

            var enterDirection = Vector3.zero;

            Assert.False(voiceOverrideDoor.HasEntered(Vector3.zero, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.back, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.forward, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.forward, Vector3.forward, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.back, Vector3.back, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.forward, Vector3.back, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.back, Vector3.forward, enterDirection));

            enterDirection = Vector3.back;

            Assert.False(voiceOverrideDoor.HasEntered(Vector3.zero, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.back, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.forward, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.forward, Vector3.forward, enterDirection));
            Assert.True(voiceOverrideDoor.HasEntered(Vector3.back, Vector3.back, enterDirection));
            Assert.True(voiceOverrideDoor.HasEntered(Vector3.forward, Vector3.back, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.back, Vector3.forward, enterDirection));

            enterDirection = Vector3.forward;

            Assert.False(voiceOverrideDoor.HasEntered(Vector3.zero, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.back, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.forward, Vector3.zero, enterDirection));
            Assert.True(voiceOverrideDoor.HasEntered(Vector3.forward, Vector3.forward, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.back, Vector3.back, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.forward, Vector3.back, enterDirection));
            Assert.True(voiceOverrideDoor.HasEntered(Vector3.back, Vector3.forward, enterDirection));

            enterDirection = Vector3.down;

            Assert.False(voiceOverrideDoor.HasEntered(Vector3.zero, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.down, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.up, Vector3.zero, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.up, Vector3.up, enterDirection));
            Assert.True(voiceOverrideDoor.HasEntered(Vector3.down, Vector3.down, enterDirection));
            Assert.True(voiceOverrideDoor.HasEntered(Vector3.up, Vector3.down, enterDirection));
            Assert.False(voiceOverrideDoor.HasEntered(Vector3.down, Vector3.up, enterDirection));
        }

        [Test]
        public void VoiceOverrideDoor_HasExited()
        {
            var voiceOverrideDoor = new GameObject().AddComponent<VoiceOverrideDoor>();

            var exitDirection = Vector3.zero;

            Assert.False(voiceOverrideDoor.HasExited(Vector3.zero, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.back, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.forward, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.forward, Vector3.forward, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.back, Vector3.back, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.forward, Vector3.back, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.back, Vector3.forward, exitDirection));

            exitDirection = Vector3.forward;

            Assert.False(voiceOverrideDoor.HasExited(Vector3.zero, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.back, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.forward, Vector3.zero, exitDirection));
            Assert.True(voiceOverrideDoor.HasExited(Vector3.forward, Vector3.forward, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.back, Vector3.back, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.forward, Vector3.back, exitDirection));
            Assert.True(voiceOverrideDoor.HasExited(Vector3.back, Vector3.forward, exitDirection));

            exitDirection = Vector3.back;

            Assert.False(voiceOverrideDoor.HasExited(Vector3.zero, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.back, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.forward, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.forward, Vector3.forward, exitDirection));
            Assert.True(voiceOverrideDoor.HasExited(Vector3.back, Vector3.back, exitDirection));
            Assert.True(voiceOverrideDoor.HasExited(Vector3.forward, Vector3.back, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.back, Vector3.forward, exitDirection));

            exitDirection = Vector3.up;

            Assert.False(voiceOverrideDoor.HasExited(Vector3.zero, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.down, Vector3.zero, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.up, Vector3.zero, exitDirection));
            Assert.True(voiceOverrideDoor.HasExited(Vector3.up, Vector3.up, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.down, Vector3.down, exitDirection));
            Assert.False(voiceOverrideDoor.HasExited(Vector3.up, Vector3.down, exitDirection));
            Assert.True(voiceOverrideDoor.HasExited(Vector3.down, Vector3.up, exitDirection));
        }
    }
}