using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Guribo.UdonBetterAudio.Runtime.Examples;
using Guribo.UdonUtils.Runtime.Common;
using Guribo.UdonUtils.Tests.Editor.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Tests.Editor
{
    public class EditorTestPlayerEars
    {
        [Test]
        public void PlayerEars_Initialize()
        {
            var ears = CreatePlayerEars(out var playerEars);

            LogAssert.Expect(LogType.Error,
                new Regex(".+earsRigidBody invalid", RegexOptions.Singleline));
            Assert.False(playerEars.Initialize());

            playerEars.earsRigidBody = ears.GetComponent<Rigidbody>();
            LogAssert.Expect(LogType.Error,
                new Regex(".+earsTrigger invalid", RegexOptions.Singleline));
            Assert.False(playerEars.Initialize());

            playerEars.earsTrigger = ears.GetComponent<SphereCollider>();
            Assert.True(playerEars.Initialize());

            Assert.AreEqual(LayerMask.NameToLayer("PlayerLocal"), ears.layer);

            Assert.NotNull(playerEars.earsTrigger);
            Assert.AreEqual(Vector3.zero, playerEars.earsTrigger.center);
            Assert.AreEqual(0.1f, playerEars.earsTrigger.radius);

            Assert.NotNull(playerEars.earsRigidBody);
            Assert.True(playerEars.earsTrigger.isTrigger);
            Assert.False(playerEars.earsRigidBody.isKinematic);
            Assert.False(playerEars.earsRigidBody.useGravity);
            Assert.AreEqual(RigidbodyInterpolation.None, playerEars.earsRigidBody.interpolation);
            Assert.AreEqual(CollisionDetectionMode.ContinuousDynamic,
                playerEars.earsRigidBody.collisionDetectionMode);
        }

        private static GameObject CreatePlayerEars(out PlayerEars playerEars)
        {
            var ears = new GameObject("Ears");
            playerEars = ears.AddComponent<PlayerEars>();
            playerEars.udonDebug = ears.AddComponent<UdonDebug>();
            return ears;
        }

        [Test]
        public void PlayerEars_GetPlayerHeadPosition()
        {
            var ears = CreatePlayerEars(out var playerEars);

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var localPlayer = UdonTestUtils.CreateLocalPlayer(0);

            VRCPlayerApi._GetTrackingData =
                (player, type) => new VRCPlayerApi.TrackingData(Vector3.up, Quaternion.identity);
            Assert.AreEqual(Vector3.up, playerEars.GetPlayerHeadPosition(localPlayer));

            VRCPlayerApi._GetTrackingData = (player, type) =>
                new VRCPlayerApi.TrackingData(Vector3.down, Quaternion.identity);
            Assert.AreEqual(Vector3.down, playerEars.GetPlayerHeadPosition(localPlayer));

            Assert.Throws<NullReferenceException>(() => playerEars.GetPlayerHeadPosition(null));
        }

        [Test]
        public void PlayerEars_IsTeleporting()
        {
            var ears = CreatePlayerEars(out var playerEars);

            Assert.True(playerEars.IsTeleporting(Vector3.zero, Vector3.up * 6f, 5f));
            Assert.False(playerEars.IsTeleporting(Vector3.zero, Vector3.up * 5f, 5f));
            Assert.False(playerEars.IsTeleporting(Vector3.zero, Vector3.up * 4f, 5f));
        }

        [Test]
        public void Test_FollowLocalPlayerHead()
        {
            var ears = CreatePlayerEars(out var playerEars);
            playerEars.earsTrigger = ears.GetComponent<SphereCollider>();
            playerEars.earsRigidBody = ears.GetComponent<Rigidbody>();
            playerEars.Initialize();
            
            var earsTransform = ears.transform;

            Networking._LocalPlayer = () => null;
            Assert.False(playerEars.FollowLocalPlayerHead());

            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var localPlayer = UdonTestUtils.CreateLocalPlayer(0);
            Assert.True(playerEars.FollowLocalPlayerHead());
            Assert.AreEqual(Vector3.zero, earsTransform.position);

            var expected = Vector3.up * 10f;
            VRCPlayerApi._GetTrackingData =
                (player, type) => new VRCPlayerApi.TrackingData(expected, Quaternion.identity);
            Assert.True(playerEars.FollowLocalPlayerHead());
            Assert.AreEqual(expected, earsTransform.position);
        }
    }
}