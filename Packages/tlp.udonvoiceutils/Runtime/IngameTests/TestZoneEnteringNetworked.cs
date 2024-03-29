using TLP.UdonUtils.Testing;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonVoiceUtils.Runtime.IngameTests
{
    /// <summary>
    /// Component which implements the base of a test case, includes preparation, execution and cleanup methods
    /// to be copied to new test scripts and filled for each individual test case.
    /// 
    /// Behaviour sync mode can be changed depending on the test performed, default is no variable sync
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TestZoneEnteringNetworked : TestCase
    {
        [SerializeField]
        private Transform Enter;

        [SerializeField]
        private Transform Exit;

        [SerializeField]
        private VoiceOverrideRoom VoiceOverrideRoom;

        [SerializeField]
        private VoiceOverrideRoom[] AllRooms;

        [UdonSynced, HideInInspector]
        public int TeleportTimeMs = int.MinValue;

        private int _initialPlayerCount;

        protected override void InitializeTest()
        {
            _initialPlayerCount = VRCPlayerApi.GetPlayerCount();

            var localPlayer = Networking.LocalPlayer;

            Networking.SetOwner(localPlayer, gameObject);

            TeleportTimeMs = Networking.GetServerTimeInMilliseconds() + 10000;
            foreach (var room in AllRooms)
            {
                if (!Utilities.IsValid(room))
                {
                    Error($"{nameof(AllRooms)} contains invalid room");
                    TestController.TestInitialized(false);
                    return;
                }
            }

            RequestSerialization();
            MarkNetworkDirty();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PrepareLocalPlayer));

            TestController.TestInitialized(Networking.IsOwner(localPlayer, gameObject));
        }

        public override void OnDeserialization(DeserializationResult deserializationResult)
        {
            PrepareLocalPlayer();
        }

        public void PrepareLocalPlayer()
        {
            if (TeleportTimeMs < Networking.GetServerTimeInMilliseconds())
            {
                return;
            }

            foreach (var overrideRoom in AllRooms)
            {
                if (!Utilities.IsValid(overrideRoom)) continue;
                if (overrideRoom.Contains(Networking.LocalPlayer))
                {
                    overrideRoom.ExitRoom(Networking.LocalPlayer, null);
                }
            }

           //Networking.LocalPlayer.TeleportTo(
           //    Vector3.down * 10f,
           //    Exit.rotation,
           //    VRC_SceneDescriptor.SpawnOrientation.Default,
           //    false
           //);

            Networking.LocalPlayer.Immobilize(true);


            while (true)
            {
                var betterPlayerAudioOverride =
                    VoiceOverrideRoom.PlayerAudioOverride.PlayerAudioController.GetMaxPriorityOverride(
                        Networking.LocalPlayer
                    );
                if (Utilities.IsValid(betterPlayerAudioOverride))
                {
                    betterPlayerAudioOverride.RemovePlayer(Networking.LocalPlayer);
                    continue;
                }

                break;
            }

            SendCustomEventDelayedSeconds(nameof(EnterDelayed), (TeleportTimeMs - Networking.GetServerTimeInMilliseconds()) * 0.001f);
        }

        public void EnterDelayed()
        {
            VoiceOverrideRoom.EnterRoom(Networking.LocalPlayer, Enter);
        }

        protected override void RunTest()
        {
            SendCustomEventDelayedSeconds(nameof(CheckDelayed), 70f);
        }

        public void CheckDelayed()
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            var players = new VRCPlayerApi[playerCount];
            VRCPlayerApi.GetPlayers(players);

            int successCount = 0;
            foreach (var vrcPlayerApi in players)
            {
                if (Utilities.IsValid(vrcPlayerApi))
                {
                    successCount +=
                        VoiceOverrideRoom.PlayerAudioOverride.PlayerAudioController
                            .GetMaxPriorityOverride(vrcPlayerApi) ==
                        VoiceOverrideRoom.PlayerAudioOverride
                            ? 1
                            : 0;
                }
            }

            DebugLog($"successCount == _initialPlayerCount? : {successCount} == {_initialPlayerCount}");

            TestController.TestCompleted(successCount == _initialPlayerCount);
        }

        protected override void CleanUpTest()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(LeaveZone));
            TestController.TestCleanedUp(true);
        }

        public void LeaveZone()
        {
            VoiceOverrideRoom.ExitRoom(Networking.LocalPlayer, Exit);
            Networking.LocalPlayer.Immobilize(false);
        }
    }
}