using System;
using Guribo.UdonBetterAudio.Runtime.Examples;
using Guribo.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;
using VRC.Udon.Common.Interfaces;

namespace Guribo.UdonBetterAudio.Tests.Runtime.BetterPlayerAudio
{
    /// <summary>
    /// Component which implements the base of a test case, includes preparation, execution and cleanup methods
    /// to be copied to new test scripts and filled for each individual test case.
    /// 
    /// Behaviour sync mode can be changed depending on the test performed, default is no variable sync
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TestZoneEnteringNetworked : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [NonSerialized] public TestController testController;

        public void Initialize()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp", this);
            CleanUpTest();
        }

        #endregion

        #region EDIT HERE

        public Transform enter;
        public Transform exit;

        public VoiceOverrideRoom voiceOverrideRoom;
        public VoiceOverrideRoom[] allRooms;

        [UdonSynced]
        public int teleportTime;

        private int _initialPlayerCount;

        private void InitializeTest()
        {
            // TODO your init behaviour here
            // ...

            _initialPlayerCount = VRCPlayerApi.GetPlayerCount();

            teleportTime = Networking.GetServerTimeInMilliseconds() + 10000;
            RequestSerialization();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PrepareLocalPlayer));

            // whenever the test is ready to be started call TestController.TestInitialized,
            // can be later in update or whenever but MUST be called at some point
            testController.TestInitialized(true);
        }

        public override void OnDeserialization()
        {
            PrepareLocalPlayer();
        }

        public void PrepareLocalPlayer()
        {
            var enterTime = teleportTime - Networking.GetServerTimeInMilliseconds();
            if (enterTime < 0)
            {
                return;
            }

            foreach (var overrideRoom in allRooms)
            {
                if (overrideRoom.Contains(Networking.LocalPlayer))
                {
                    overrideRoom.ExitRoom(Networking.LocalPlayer, null);
                }
            }
            
            Networking.LocalPlayer.TeleportTo(Vector3.down * 10f,  exit.rotation,
                VRC_SceneDescriptor.SpawnOrientation.Default, false);
            
            Networking.LocalPlayer.Immobilize(true);


            
            while (true)
            {
                var betterPlayerAudioOverride =
                    voiceOverrideRoom.playerOverride.betterPlayerAudio.GetMaxPriorityOverride(
                        Networking.LocalPlayer);
                if (Utilities.IsValid(betterPlayerAudioOverride))
                {
                    betterPlayerAudioOverride.RemovePlayer(Networking.LocalPlayer);
                    continue;
                }

                break;
            }


            SendCustomEventDelayedSeconds(nameof(Enter), enterTime * 0.001f);
        }

        public void Enter()
        {
            voiceOverrideRoom.EnterRoom(Networking.LocalPlayer, enter);
        }

        private void RunTest()
        {
            // TODO your test behaviour here
            // ...

            // whenever the test is completed call TestController.TestCompleted,
            // can be later in update or whenever but MUST be called at some point

            SendCustomEventDelayedSeconds(nameof(CheckDelayed), 70f);
        }

        public void CheckDelayed()
        {
            var playerCount = VRCPlayerApi.GetPlayerCount();
            var players = new VRCPlayerApi[playerCount];
            VRCPlayerApi.GetPlayers(players);

            var successCount = 0;
            foreach (var vrcPlayerApi in players)
            {
                if (Utilities.IsValid(vrcPlayerApi))
                {
                    successCount +=
                        voiceOverrideRoom.playerOverride.betterPlayerAudio.GetMaxPriorityOverride(vrcPlayerApi) ==
                        voiceOverrideRoom.playerOverride
                            ? 1
                            : 0;
                }
            }

            Debug.Log($"successCount == _initialPlayerCount? : {successCount} == {_initialPlayerCount}");

            testController.TestCompleted(successCount == _initialPlayerCount);
        }

        private void CleanUpTest()
        {
            // TODO your clean up behaviour here
            // ...

            // whenever the test is cleaned up call TestController.TestCleanedUp,
            // can be later in update or whenever but MUST be called at some point

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(LeaveZone));
            testController.TestCleanedUp(true);
        }

        public void LeaveZone()
        {
            voiceOverrideRoom.ExitRoom(Networking.LocalPlayer, exit);
            Networking.LocalPlayer.Immobilize(false);
        }

        private bool Assert(bool condition, string message)
        {
            if (!condition)
            {
                if (Utilities.IsValid(this))
                {
                    Debug.LogError(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Assertion failed : '" +
                        GetType() + " : " + message + "'", this);
                }
                else
                {
                    Debug.LogError(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Assertion failed :  'UNKNOWN TYPE: " +
                        message + "'");
                }

                return false;
            }

            Debug.Assert(condition, message);
            return true;
        }

        #endregion
    }
}