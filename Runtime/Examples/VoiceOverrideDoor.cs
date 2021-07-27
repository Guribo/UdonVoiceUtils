using Guribo.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Runtime.Examples
{
    [RequireComponent(typeof(BoxCollider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VoiceOverrideDoor : UdonSharpBehaviour
    {
        #region Settings

        [Header("Settings")]
        [Tooltip(
            "Direction vector in this GameObjects local space in which the player has to pass through the trigger to leave the override zone")]
        public Vector3 exitDirection = Vector3.forward;
        [Tooltip(
            "When set to true merely coming into contact with the trigger is enough to leave the zone, useful for e.g. a water surface")]
        public bool leaveOnTouch;

        #endregion

        #region Mandatory references

        [Header("Mandatory references")]
        public UdonDebug udonDebug;
        public VoiceOverrideRoom voiceOverrideRoom;

        #endregion

        private Vector3 _enterPosition;

        // used to allow proper enter/exit detection for horizontal triggers (e.g. a water surface)
        private readonly Vector3 _playerColliderGroundOffset = new Vector3(0, 0.2f, 0);

        #region Udon Lifecycle

        public void Start()
        {
            // prevent ui raycast from being blocked by the door
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            var trigger = gameObject.GetComponent<BoxCollider>();
            if (!udonDebug.Assert(Utilities.IsValid(trigger), "Missing a box collider", this)
                || !udonDebug.Assert(trigger.isTrigger, "Box collider must be a trigger", this)
                || !udonDebug.Assert(trigger.center == Vector3.zero, "Box collider center must be 0,0,0", this))
            {
                return;
            }
        }
        
        #endregion

        #region Local Player behaviour

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!udonDebug.Assert(Utilities.IsValid(player), "Invalid player left", this))
            {
                return;
            }

            if (!player.isLocal)
            {
                return;
            }
            
            _enterPosition = Vector3.zero;
        }
        

        #endregion
        
        #region Player collision detection

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!udonDebug.Assert(Utilities.IsValid(player), "Invalid player entered", this))
            {
                return;
            }

            if (!player.isLocal)
            {
                return;
            }

            _enterPosition = transform.InverseTransformPoint(player.GetPosition() + _playerColliderGroundOffset);

            if (leaveOnTouch)
            {
                if (!udonDebug.Assert(Utilities.IsValid(voiceOverrideRoom), "Failed removing player from override",
                    this))
                {
                    return;
                }

                if (voiceOverrideRoom.Contains(player))
                {
                    voiceOverrideRoom.ExitRoom(player, null);
                }
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!udonDebug.Assert(Utilities.IsValid(player), "Invalid player left", this))
            {
                return;
            }

            if (!player.isLocal)
            {
                return;
            }

            if (_enterPosition == Vector3.zero)
            {
                return;
            }

            var exitPosition = transform.InverseTransformPoint(player.GetPosition() + _playerColliderGroundOffset);
            var enterPosition = _enterPosition;
            _enterPosition = Vector3.zero;

            if (HasExited(enterPosition, exitPosition, exitDirection))
            {
                if (!udonDebug.Assert(Utilities.IsValid(voiceOverrideRoom), "Failed removing player from override",
                    this))
                {
                    return;
                }

                if (voiceOverrideRoom.Contains(player))
                {
                    voiceOverrideRoom.ExitRoom(player, null);
                }
                return;
            }
            
            var enterDirection = -exitDirection;
            if (HasEntered(enterPosition, exitPosition, enterDirection))
            {
                if (!udonDebug.Assert(Utilities.IsValid(voiceOverrideRoom), "Failed adding player to override",
                    this))
                {
                    return;
                }

                voiceOverrideRoom.EnterRoom(player, null);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if the local player is currently in contact with the trigger</returns>
        public bool LocalPlayerInTrigger()
        {
            return _enterPosition != Vector3.zero;
        }
        
        #endregion

        #region internal

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enterPosition"></param>
        /// <param name="exitPosition"></param>
        /// <param name="enterDirection">direction in local space which determines whether
        /// the positions indicate entering.
        /// If set to 0,0,0 false is returned.</param>
        /// <returns></returns>
        internal bool HasEntered(Vector3 enterPosition, Vector3 exitPosition, Vector3 enterDirection)
        {
            if (enterPosition == Vector3.zero
                || exitPosition == Vector3.zero
                || enterDirection == Vector3.zero)
            {
                return false;
            }

            var enterDirectionNormalized = enterDirection.normalized;
            var enterPositionNormalized = enterPosition.normalized;

            var enterOutside = Vector3.Dot(enterPositionNormalized, enterDirectionNormalized) < 0;
            var exitInside = Vector3.Dot(exitPosition, enterDirectionNormalized) > 0;
            if (enterOutside && exitInside)
            {
                return true;
            }

            var enterInside = Vector3.Dot(enterPositionNormalized, enterDirectionNormalized) > 0;
            return enterInside && exitInside;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enterPosition"></param>
        /// <param name="exitPosition"></param>
        /// <param name="leaveDirection">direction in local space which determines whether
        /// the positions indicate exiting.
        /// If set to 0,0,0 false is returned.</param>
        /// <returns></returns>
        internal bool HasExited(Vector3 enterPosition, Vector3 exitPosition, Vector3 leaveDirection)
        {
            if (enterPosition == Vector3.zero
                || exitPosition == Vector3.zero
                || leaveDirection == Vector3.zero)
            {
                return false;
            }

            var leaveDirectionNormalized = leaveDirection.normalized;
            var enterPositionNormalized = enterPosition.normalized;

            var enterOutside = Vector3.Dot(enterPositionNormalized, leaveDirectionNormalized) > 0;
            var exitOutside = Vector3.Dot(exitPosition.normalized, leaveDirectionNormalized) > 0;
            if (enterOutside && exitOutside)
            {
                return true;
            }

            var enterInside = Vector3.Dot(enterPositionNormalized, leaveDirectionNormalized) < 0;
            return enterInside && exitOutside;
        }
        
        #endregion
    }
}