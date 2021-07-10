using System;
using System.Collections;
using System.Collections.Generic;
using Guribo.UdonBetterAudio.Runtime.Examples;
using Guribo.UdonUtils.Runtime.Common;
using Guribo.UdonUtils.Tests.Editor.Utils;
using NUnit.Framework;
using UdonSharp;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Guribo.UdonBetterAudio.Tests.Runtime.BetterPlayerAudio
{
    public class PlaymodeTestPlayerEars
    {
        private class TestObstacle : MonoBehaviour
        {
            public bool teleportingIn;
            public bool teleportingOut;
            public bool entered;
            public bool exited;

            private void OnTriggerEnter(Collider other)
            {
                Debug.Log("Enter");
                teleportingIn = other.GetComponent<PlayerEars>().Teleporting();
                entered = true;
            }

            private void OnTriggerExit(Collider other)
            {
                Debug.Log("Exit");
                teleportingOut = other.GetComponent<PlayerEars>().Teleporting();
                exited = true;
            }
        }

        [UnityTest]
        public IEnumerator Test_FollowLocalPlayerHead_TriggerOverlapEnterAndExit_Teleporting()
        {
            
            var ears = CreatePlayerEars(out var playerEars);
            var earsTransform = ears.transform;
            playerEars.earsTrigger = ears.GetComponent<SphereCollider>();
            playerEars.earsRigidBody = ears.GetComponent<Rigidbody>();
            playerEars.teleportDetectionThreshold = 11f;
            
            yield return null;
            playerEars.Initialize();
            ears.layer = LayerMask.NameToLayer("Default");
            playerEars.earsTrigger.isTrigger = false;
            
            VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            var localPlayer = UdonTestUtils.CreateLocalPlayer(0);


            // var obstacle = new GameObject("Obstacle") {layer = LayerMask.NameToLayer("PlayerLocal")};
            var obstacle = new GameObject("Obstacle");
            obstacle.transform.position = Vector3.forward * 5f;
            var trigger = obstacle.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            obstacle.AddComponent<Rigidbody>().isKinematic = true;
            obstacle.layer = LayerMask.NameToLayer("Default");

            var testObstacle = obstacle.AddComponent<TestObstacle>();

            yield return null;

            VRCPlayerApi._GetTrackingData =
                (player, type) => new VRCPlayerApi.TrackingData(Vector3.forward * 5f, Quaternion.identity);
            
            Assert.AreEqual(obstacle.layer, ears.layer);
            
            Assert.True(playerEars.FollowLocalPlayerHead());
            Assert.AreEqual(Vector3.forward * 5f, playerEars.transform.position);
            
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            

            Assert.True(testObstacle.entered);
            Assert.True(testObstacle.exited);
            Assert.True(testObstacle.teleportingIn);
            Assert.True(testObstacle.teleportingOut);

            Object.DestroyImmediate(testObstacle);
            testObstacle = obstacle.AddComponent<TestObstacle>();

            yield return null;

            playerEars.teleportDetectionThreshold = 20f;
            VRCPlayerApi._GetTrackingData =
                (player, type) => new VRCPlayerApi.TrackingData(Vector3.zero, Quaternion.identity);

            Assert.True(playerEars.FollowLocalPlayerHead());
            Assert.AreEqual(Vector3.zero, earsTransform.position);

            Assert.True(testObstacle.entered);
            Assert.True(testObstacle.exited);
            Assert.False(testObstacle.teleportingIn);
            Assert.False(testObstacle.teleportingOut);
            
        }

        private static GameObject CreatePlayerEars(out PlayerEars playerEars)
        {
            var ears = new GameObject("Ears");
            playerEars = ears.AddComponent<PlayerEars>();
            playerEars.udonDebug = ears.AddComponent<UdonDebug>();
            return ears;
        }
    }
}