using System.Collections.Generic;
using System.Text.RegularExpressions;
using Guribo.UdonBetterAudio.Runtime;
using Guribo.UdonBetterAudio.Runtime.Examples;
using Guribo.UdonUtils.Runtime.Common;
using Guribo.UdonUtils.Runtime.Common.Networking;
using Guribo.UdonUtils.Tests.Runtime.Utils;
using NUnit.Framework;
using UdonSharp;
using UdonSharpEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Tests.Editor
{
    public class TestOverrideZoneEnterExit
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
        public void OverrideZoneActivator_Interact()
        {
            var overrideZoneActivatorGameobject = new GameObject();
            var syncedIntegerArrayGameObject = new GameObject();
            var playerListGameobject = new GameObject();
            var udonDebug = new GameObject();
            var enterButtonGo = new GameObject();

            var voiceOverrideRoom = overrideZoneActivatorGameobject.AddUdonSharpComponent<VoiceOverrideRoom>();
            Assert.True(Utilities.IsValid(voiceOverrideRoom));

            voiceOverrideRoom.playerOverride = _voiceOverride;

            _voiceOverride.playerList = playerListGameobject.AddUdonSharpComponent<PlayerList>();

            voiceOverrideRoom.syncedIntegerArray = syncedIntegerArrayGameObject.AddComponent<SyncedIntegerArray>();
            voiceOverrideRoom.syncedIntegerArray.targetBehaviour = _voiceOverride.playerList;
            voiceOverrideRoom.syncedIntegerArray.changeEventListeners = new[] {(UdonSharpBehaviour) voiceOverrideRoom};
            voiceOverrideRoom.syncedIntegerArray.targetChangeEvent = nameof(VoiceOverrideRoom.RefreshPlayersInZone);
            voiceOverrideRoom.syncedIntegerArray.targetVariable = nameof(PlayerList.players);

            voiceOverrideRoom.syncedIntegerArray.udonDebug = udonDebug.AddComponent<UdonDebug>();
            voiceOverrideRoom.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            _voiceOverride.playerList.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;

            var enterButton = enterButtonGo.AddComponent<VoiceOverrideRoomEnterButton>();
            enterButton.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            enterButton.voiceOverrideRoom = voiceOverrideRoom;

            // no player yet in zone
            Assert.False(_voiceOverride.playerList.Contains(_localPlayer));
            Assert.False(voiceOverrideRoom.Contains(_localPlayer));
            Assert.IsNull(_betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.False(voiceOverrideRoom.IsInZone);

            // local player enters
            enterButton.Interact();
            Assert.True(voiceOverrideRoom.IsInZone);
            Assert.True(_voiceOverride.playerList.Contains(_localPlayer));
            Assert.True(voiceOverrideRoom.Contains(_localPlayer));
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.AreEqual(voiceOverrideRoom.syncedIntegerArray.syncedValue, _voiceOverride.playerList.players);

            // try adding player again
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Player 0 already in list.", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Error, new Regex(".+player already affected.", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Error,
                new Regex(".+Adding player to player list failed.", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Error, new Regex(".+Failed adding player to zone.", RegexOptions.Singleline));
#endif
            voiceOverrideRoom.EnterRoom(Networking.LocalPlayer, null);
            enterButton.Interact();
            voiceOverrideRoom.syncedIntegerArray.syncedValue = new int[_voiceOverride.playerList.players.Length];
            _voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);

            Assert.True(_voiceOverride.playerList.Contains(_localPlayer));
            Assert.True(voiceOverrideRoom.IsInZone);
            Assert.True(voiceOverrideRoom.Contains(_localPlayer));
            Assert.AreEqual(1, voiceOverrideRoom.syncedIntegerArray.syncedValue.Length);

            Assert.AreEqual(_voiceOverride.playerList.players,
                voiceOverrideRoom.syncedIntegerArray.syncedValue);
            Assert.AreEqual(_localPlayer.playerId, voiceOverrideRoom.syncedIntegerArray.syncedValue[0]);
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.localPlayerOverrideList.GetMaxPriority());
        }

        [Test]
        public void OverrideZoneExit_InteractAndRespawn()
        {
            #region Test init

            var overrideZoneActivatorGameobject = new GameObject();
            var overrideZoneExitGameobject = new GameObject();
            var syncedIntegerArrayGameObject = new GameObject();
            var playerListGameobject = new GameObject();
            var udonDebug = new GameObject();

            var voiceOverrideRoom = overrideZoneActivatorGameobject.AddUdonSharpComponent<VoiceOverrideRoom>();
            var overrideZoneExit = overrideZoneExitGameobject.AddUdonSharpComponent<VoiceOverrideRoomExitButton>();

            _voiceOverride.betterPlayerAudio = _betterPlayerAudio;

            _voiceOverride.playerList = playerListGameobject.AddUdonSharpComponent<PlayerList>();

            voiceOverrideRoom.syncedIntegerArray =
                syncedIntegerArrayGameObject.AddUdonSharpComponent<SyncedIntegerArray>();
            voiceOverrideRoom.syncedIntegerArray.targetBehaviour = _voiceOverride.playerList;
            voiceOverrideRoom.syncedIntegerArray.changeEventListeners = new[] {(UdonSharpBehaviour) voiceOverrideRoom};
            voiceOverrideRoom.syncedIntegerArray.targetChangeEvent = nameof(VoiceOverrideRoom.RefreshPlayersInZone);
            voiceOverrideRoom.syncedIntegerArray.targetVariable = nameof(PlayerList.players);

            voiceOverrideRoom.syncedIntegerArray.udonDebug = udonDebug.AddComponent<UdonDebug>();
            voiceOverrideRoom.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            voiceOverrideRoom.playerOverride = _voiceOverride;
            voiceOverrideRoom.playerOverride.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            _voiceOverride.playerList.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;

            overrideZoneExit.voiceOverrideRoom = voiceOverrideRoom;
            overrideZoneExit.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;

            var enterButtonGo = new GameObject();
            var enterButton = enterButtonGo.AddComponent<VoiceOverrideRoomEnterButton>();
            enterButton.udonDebug = voiceOverrideRoom.syncedIntegerArray.udonDebug;
            enterButton.voiceOverrideRoom = voiceOverrideRoom;

            #endregion

            // no player override added yet
            Assert.AreEqual(0, _voiceOverride.playerList.DiscardInvalid());
            Assert.Null(_betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));

            // add single override to local player
            enterButton.Interact();

            // check single player added to player list
            Assert.True(_voiceOverride.playerList.Contains(_localPlayer));

            // synced array contains player id

            #region TEMP

            // TODO remove temp workaround
            voiceOverrideRoom.syncedIntegerArray.syncedValue =
                new int[_voiceOverride.playerList.players.Length];
            _voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);
            voiceOverrideRoom.RefreshPlayersInZone();

            #endregion

            Assert.NotNull(voiceOverrideRoom.syncedIntegerArray);
            Assert.NotNull(voiceOverrideRoom.syncedIntegerArray.syncedValue);
            Assert.AreEqual(1, voiceOverrideRoom.syncedIntegerArray.syncedValue.Length);
            Assert.AreEqual(_localPlayer.playerId, voiceOverrideRoom.syncedIntegerArray.syncedValue[0]);

            // check single player added to override
            Assert.True(_voiceOverride.IsAffected(_localPlayer));

            // better player audio contains local player in local override list
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.localPlayerOverrideList.GetMaxPriority());

            // better player audio returns override for local player
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.True(_betterPlayerAudio.HasVoiceOverrides(_localPlayer));

            // remove player from override
            overrideZoneExit.Interact();

            // list no longer contains local player
            Assert.False(_voiceOverride.playerList.Contains(_localPlayer));

            #region TEMP

            // TODO remove temp workaround
            voiceOverrideRoom.syncedIntegerArray.syncedValue = new int[_voiceOverride.playerList.players.Length];
            _voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);
            voiceOverrideRoom.RefreshPlayersInZone();

            #endregion

            Assert.NotNull(voiceOverrideRoom.syncedIntegerArray);
            Assert.NotNull(voiceOverrideRoom.syncedIntegerArray.syncedValue);
            Assert.AreEqual(0, voiceOverrideRoom.syncedIntegerArray.syncedValue.Length);

            // player is no longer affected
            Assert.False(_voiceOverride.IsAffected(_localPlayer));

            // localPlayerOverrideList contains no longer the override
            Assert.Null(_betterPlayerAudio.localPlayerOverrideList.GetMaxPriority());

            // GetMaxPriorityOverride also no longer provides the override
            Assert.Null(_betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            
            // player no longer has overrides
            Assert.False(_betterPlayerAudio.HasVoiceOverrides(_localPlayer));

            voiceOverrideRoom.exitZoneOnRespawn = false;

            enterButton.Interact();

            #region TEMP

            // TODO remove temp workaround
            voiceOverrideRoom.syncedIntegerArray.syncedValue =
                new int[_voiceOverride.playerList.players.Length];
            _voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);
            voiceOverrideRoom.RefreshPlayersInZone();

            #endregion

            Assert.AreEqual(1, _voiceOverride.playerList.DiscardInvalid());
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));
            Assert.True(voiceOverrideRoom.IsInZone);

            overrideZoneExit.OnPlayerRespawn(_localPlayer);
            Assert.AreEqual(1, _voiceOverride.playerList.DiscardInvalid());
            Assert.AreEqual(_voiceOverride, _betterPlayerAudio.GetMaxPriorityOverride(_localPlayer));

            voiceOverrideRoom.exitZoneOnRespawn = true;
            voiceOverrideRoom.OnPlayerRespawn(_localPlayer);

            #region TEMP

            // TODO remove temp workaround
            voiceOverrideRoom.syncedIntegerArray.syncedValue =
                new int[_voiceOverride.playerList.players.Length];
            _voiceOverride.playerList.players.CopyTo(voiceOverrideRoom.syncedIntegerArray.syncedValue, 0);
            voiceOverrideRoom.RefreshPlayersInZone();

            #endregion

            Assert.AreEqual(0, _voiceOverride.playerList.DiscardInvalid());
            Assert.Null(_betterPlayerAudio.GetMaxPriorityOverride(_player1));
            Assert.False(_voiceOverride.IsAffected(_player1));
            Assert.False(voiceOverrideRoom.IsInZone);
        }

        [Test]
        public void VoiceOverrideDoor_Start()
        {
            var door = new GameObject("Door");
            var frontTrigger = new GameObject("FrontTrigger");

            var udonDebug = door.AddComponent<UdonDebug>();

            var voiceOverrideDoor = frontTrigger.AddComponent<VoiceOverrideDoor>();

            voiceOverrideDoor.udonDebug = udonDebug;

            Assert.NotNull(voiceOverrideDoor.gameObject.GetComponent<BoxCollider>());

#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Box collider must be a trigger.", RegexOptions.Singleline));
#endif
            voiceOverrideDoor.Start();

            voiceOverrideDoor.gameObject.GetComponent<BoxCollider>().center = Random.insideUnitSphere;
            voiceOverrideDoor.gameObject.GetComponent<BoxCollider>().isTrigger = true;

#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Box collider center must be 0,0,0.", RegexOptions.Singleline));
#endif
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
            var door = new GameObject("Door");
            var frontTrigger = new GameObject("FrontTrigger");
            var udonDebug = door.AddComponent<UdonDebug>();

            var voiceOverrideDoor = frontTrigger.AddComponent<VoiceOverrideDoor>();
            voiceOverrideDoor.udonDebug = udonDebug;

            Assert.False(voiceOverrideDoor.LocalPlayerInTrigger());

            // enter invalid player
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Invalid player entered.", RegexOptions.Singleline));
#endif
            voiceOverrideDoor.OnPlayerTriggerEnter(null);
            Assert.False(voiceOverrideDoor.LocalPlayerInTrigger());

            _localPlayer.gameObject.transform.position = Vector3.forward;
            voiceOverrideDoor.OnPlayerTriggerEnter(_localPlayer);
            Assert.True(voiceOverrideDoor.LocalPlayerInTrigger());

            _localPlayer.gameObject.transform.position = Vector3.back;
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Failed adding player to override.", RegexOptions.Singleline));
#endif
            voiceOverrideDoor.OnPlayerTriggerExit(_localPlayer);


            _localPlayer.gameObject.transform.position = Vector3.back;
            voiceOverrideDoor.OnPlayerTriggerEnter(_localPlayer);
            Assert.True(voiceOverrideDoor.LocalPlayerInTrigger());

            _localPlayer.gameObject.transform.position = Vector3.back;
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Failed adding player to override.", RegexOptions.Singleline));
#endif
            voiceOverrideDoor.OnPlayerTriggerExit(_localPlayer);
        }

        [Test]
        public void VoiceOverrideDoor_Exit()
        {
            var door = new GameObject("Door");
            var doorTrigger = new GameObject("FrontTrigger");
            var udonDebug = door.AddComponent<UdonDebug>();

            var voiceOverrideDoor = doorTrigger.AddComponent<VoiceOverrideDoor>();

            voiceOverrideDoor.udonDebug = udonDebug;

            _localPlayer.gameObject.transform.position = Vector3.back;
            voiceOverrideDoor.OnPlayerTriggerEnter(_localPlayer);
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".+Invalid player left.", RegexOptions.Singleline));
#endif
            voiceOverrideDoor.OnPlayerTriggerExit(null);

            _localPlayer.gameObject.transform.position = Vector3.forward;

#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error,
                new Regex(".+Failed removing player from override.", RegexOptions.Singleline));
#endif
            voiceOverrideDoor.OnPlayerTriggerExit(_localPlayer);
            Assert.False(voiceOverrideDoor.LocalPlayerInTrigger());

            _player1.gameObject.transform.position = Vector3.forward;
            voiceOverrideDoor.OnPlayerTriggerEnter(_localPlayer);

            _localPlayer.gameObject.transform.position = Vector3.forward;

#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error,
                new Regex(".+Failed removing player from override.", RegexOptions.Singleline));
#endif
            voiceOverrideDoor.OnPlayerTriggerExit(_localPlayer);
        }

        private VoiceOverrideRoomExitButton CreateVoiceOverrideRoomExitButton()
        {
            var go = new GameObject();
            var result = go.AddComponent<VoiceOverrideRoomExitButton>();
            result.udonDebug = go.AddComponent<UdonDebug>();
            return result;
        }
        
        [Test]
        public void ExitRoom_InvalidPlayer()
        {
            var voiceOverrideRoomExitButton = CreateVoiceOverrideRoomExitButton();

#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error,
                new Regex(".+player invalid.", RegexOptions.Singleline));
#endif
            voiceOverrideRoomExitButton.ExitRoom(null);
        }
        
        [Test]
        public void ExitRoom_PlayerNotLocal()
        {
            var voiceOverrideRoomExitButton = CreateVoiceOverrideRoomExitButton();
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error,
                new Regex(".+player not local.", RegexOptions.Singleline));
#endif
            voiceOverrideRoomExitButton.ExitRoom(_player1);
        }
        
        [Test]
        public void ExitRoom_InvalidVoiceOverrideRoom()
        {
            var voiceOverrideRoomExitButton = CreateVoiceOverrideRoomExitButton();
#if GURIBO_DEBUG
            LogAssert.Expect(LogType.Error,
                new Regex(".+voiceOverrideRoom invalid.", RegexOptions.Singleline));
#endif
            voiceOverrideRoomExitButton.ExitRoom(_localPlayer);
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