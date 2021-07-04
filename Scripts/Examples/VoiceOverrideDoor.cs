using Guribo.UdonUtils.Scripts.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Scripts.Examples
{
    /// <summary>
    /// 
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VoiceOverrideDoor : UdonSharpBehaviour
    {
        public UdonDebug udonDebug;
        public OverrideZoneActivator overrideZoneActivator;

        private Vector3 _enterPosition;

        public void Start()
        {
            // prevent ui raycast from being blocked by the door
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            var trigger = gameObject.GetComponent<BoxCollider>();
            if (!udonDebug.Assert(Utilities.IsValid(trigger), "Missing a box collider", this))
            {
                return;
            }

            if (!udonDebug.Assert(trigger.isTrigger, "Box collider must be a trigger", this))
            {
                return;
            }

            if (!udonDebug.Assert(trigger.center == Vector3.zero, "Box collider center must be 0,0,0", this))
            {
                return;
            }
        }

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

            _enterPosition = transform.InverseTransformPoint(player.GetPosition());
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

            var exitPosition = transform.InverseTransformPoint(player.GetPosition());
            var enterPosition = _enterPosition;
            _enterPosition = Vector3.zero;

            if (HasEntered(enterPosition, exitPosition))
            {
                Debug.Log("HasEntered");
                if (!udonDebug.Assert(Utilities.IsValid(overrideZoneActivator), "Failed adding player to override",
                    this))
                {
                    return;
                }

                overrideZoneActivator.EnterZone(player);
            }

            if (HasExited(enterPosition, exitPosition))
            {
                Debug.Log("HasExited");
                if (!udonDebug.Assert(Utilities.IsValid(overrideZoneActivator), "Failed removing player from override",
                    this))
                {
                    return;
                }

                overrideZoneActivator.ExitZone(player, null);
            }
        }

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

        public bool LocalPlayerInside()
        {
            return _enterPosition != Vector3.zero;
        }

        public bool HasEntered(Vector3 enterPosition, Vector3 exitPosition)
        {
            if (enterPosition == Vector3.zero || exitPosition == Vector3.zero)
            {
                return false;
            }

            return enterPosition.z > 0 && exitPosition.z < 0
                   || enterPosition.z < 0 && exitPosition.z < 0;
        }

        public bool HasExited(Vector3 enterPosition, Vector3 exitPosition)
        {
            if (enterPosition == Vector3.zero || exitPosition == Vector3.zero)
            {
                return false;
            }

            return enterPosition.z < 0 && exitPosition.z > 0
                   || enterPosition.z > 0 && exitPosition.z > 0;
        }
    }
}