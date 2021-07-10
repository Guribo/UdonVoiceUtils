using System;
using Guribo.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    [DefaultExecutionOrder(9990)]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class PlayerEars : UdonSharpBehaviour
    {
        [Tooltip("Distance that the player head moved during an update that is considered teleporting")]
        public float teleportDetectionThreshold = 5f;
        
        #region Mandatory references

        [Header("Mandatory references")]
        public Rigidbody earsRigidBody;
        public SphereCollider earsTrigger;
        public UdonDebug udonDebug;
        
        #endregion

        private bool _teleporting;

        public void Start()
        {
            udonDebug.Assert(Initialize(), "Initialization of PlayerEars failed", this);
        }

        public bool Initialize()
        {
            if (!udonDebug.Assert(Utilities.IsValid(gameObject), "GameObject invalid", this)
            || !udonDebug.Assert(Utilities.IsValid(earsRigidBody), "earsRigidBody invalid", this)
            || !udonDebug.Assert(Utilities.IsValid(earsTrigger), "earsTrigger invalid", this))
            {
                return false;
            }
            gameObject.layer = LayerMask.NameToLayer("PlayerLocal");

            earsTrigger.center = Vector3.zero;
            earsTrigger.radius = 0.1f;
            earsTrigger.isTrigger = true;
            
            earsRigidBody.isKinematic = false;
            earsRigidBody.useGravity = false;
            earsRigidBody.interpolation = RigidbodyInterpolation.None;
            earsRigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return true;
        }
        

        /// <summary>
        /// </summary>
        /// <param name="localPlayer">Must be valid!!!</param>
        /// <returns>the player head position that can be used as ear position</returns>
        public Vector3 GetPlayerHeadPosition(VRCPlayerApi localPlayer)
        {
            return localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        }

        public bool IsTeleporting(Vector3 start, Vector3 end, float threshold)
        {
            return Vector3.Distance(start, end) > threshold;
        }

        public bool FollowLocalPlayerHead()
        {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer))
            {
                return false;
            }

            var newPosition = GetPlayerHeadPosition(localPlayer);
            _teleporting = IsTeleporting(transform.position, newPosition, teleportDetectionThreshold);
            if (_teleporting)
            {
                Debug.Log("Teleporting");
                transform.position = newPosition;
            }
            else
            {
                earsRigidBody.MovePosition(newPosition);
            }

            _teleporting = false;

            return true;
        }

        public bool Teleporting()
        {
            return _teleporting;
        }
    }
}
