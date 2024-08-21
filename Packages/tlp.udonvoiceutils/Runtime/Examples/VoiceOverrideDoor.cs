using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{
    [RequireComponent(typeof(BoxCollider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class VoiceOverrideDoor : TlpBaseBehaviour
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VoiceOverrideRoom.ExecutionOrder + 1;
        #endregion

        #region Settings
        [FormerlySerializedAs("exitDirection")]
        [Header("Settings")]
        [Tooltip(
                "Direction vector in this GameObjects local space in which the player has to pass through the trigger to leave the override zone"
        )]
        public Vector3 ExitDirection = Vector3.forward;

        [FormerlySerializedAs("leaveOnTouch")]
        [Tooltip(
                "When set to true merely coming into contact with the trigger is enough to leave the zone, useful for e.g. a water surface"
        )]
        public bool LeaveOnTouch;
        #endregion

        #region Mandatory references
        [FormerlySerializedAs("voiceOverrideRoom")]
        [Header("Mandatory references")]
        public VoiceOverrideRoom VoiceOverrideRoom;
        #endregion

        #region State
        private Vector3 _enterPosition;

        // used to allow proper enter/exit detection for horizontal triggers (e.g. a water surface)
        private readonly Vector3 _playerColliderGroundOffset = new Vector3(0, 0.2f, 0);

        internal bool Initialized { get; private set; }
        #endregion

        #region Local Player behaviour
        public override void OnPlayerRespawn(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerRespawn)}: {player.ToStringSafe()}");
#endif
            #endregion

            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            if (!player.IsLocalSafe()) {
#if TLP_DEBUG
                Warn($"Ignore respawn of irrelevant {player.ToStringSafe()}");
#endif
                return;
            }

            _enterPosition = Vector3.zero;
        }
        #endregion

        #region Player collision detection
        public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerTriggerEnter)}: {player.ToStringSafe()}");
#endif
            #endregion

            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            if (!player.IsLocalSafe()) {
#if TLP_DEBUG
                DebugLog($"{nameof(OnPlayerTriggerEnter)}: ignoring {player.ToStringSafe()}");
#endif
                return;
            }

            _enterPosition = transform.InverseTransformPoint(player.GetPosition() + _playerColliderGroundOffset);

            if (!LeaveOnTouch) {
                return;
            }

            if (VoiceOverrideRoom.Contains(player)) {
                VoiceOverrideRoom.ExitRoom(player, null);
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerTriggerExit)}: {player.ToStringSafe()}");
#endif
            #endregion

            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            if (!player.IsLocalSafe()) {
#if TLP_DEBUG
                DebugLog($"{nameof(OnPlayerTriggerExit)}: ignoring {player.ToStringSafe()}");
#endif
                return;
            }

            if (_enterPosition == Vector3.zero) {
                Error(
                        $"{player.ToStringSafe()} was not touching and is being ignored. Udon missed {nameof(OnPlayerTriggerEnter)} event!?");
                return;
            }

            var exitPosition = transform.InverseTransformPoint(player.GetPosition() + _playerColliderGroundOffset);
            var enterPosition = _enterPosition;
            _enterPosition = Vector3.zero;

            if (HasExitedDoor(enterPosition, exitPosition, ExitDirection)) {
                RemovePlayerFromRoom(player);
                return;
            }

            AddPlayerToRoom(player);
        }


        /// <summary>
        ///
        /// </summary>
        /// <returns>true if the local player is currently in contact with the trigger</returns>
        public bool IsLocalPlayerTouching() {
            return _enterPosition != Vector3.zero;
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(VoiceOverrideRoom)) {
                Error($"{nameof(VoiceOverrideRoom)} not set");
                return false;
            }

            // prevent ui raycast from being blocked by the door
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            var trigger = gameObject.GetComponent<BoxCollider>();
            if (!Utilities.IsValid(trigger)) {
                Error($"{nameof(BoxCollider)} missing on {transform.GetPathInScene()}");
                return false;
            }

            if (!trigger.isTrigger) {
                trigger.isTrigger = true;
                Warn($"{nameof(BoxCollider)} was changed to be trigger");
            }

            if (trigger.center != Vector3.zero) {
                trigger.center = Vector3.zero;
                Warn($"{nameof(BoxCollider)} center was changed to Vector3.zero");
            }

            Initialized = true;
            return true;
        }
        #endregion

        #region internal
        /// <param name="enterPosition">in local space</param>
        /// <param name="exitPosition">in local space</param>
        /// <param name="leaveDirection">direction in local space which determines whether
        /// the positions indicate exiting the room.
        /// If set to 0,0,0 false is returned.</param>
        /// <returns>true if positions indicate that player passed through the door to the outside</returns>
        internal bool HasExitedDoor(Vector3 enterPosition, Vector3 exitPosition, Vector3 leaveDirection) {
            if (enterPosition == Vector3.zero
                || exitPosition == Vector3.zero
                || leaveDirection == Vector3.zero) {
                return false;
            }

            var leaveDirectionNormalized = leaveDirection.normalized;
            var enterPositionNormalized = enterPosition.normalized;

            bool enterOutside = Vector3.Dot(enterPositionNormalized, leaveDirectionNormalized) > 0;
            bool exitOutside = Vector3.Dot(exitPosition.normalized, leaveDirectionNormalized) > 0;
            if (enterOutside && exitOutside) {
                return true;
            }

            bool enterInside = Vector3.Dot(enterPositionNormalized, leaveDirectionNormalized) < 0;
            return enterInside && exitOutside;
        }

        private void AddPlayerToRoom(VRCPlayerApi player) {
            if (VoiceOverrideRoom.Contains(player)) {
                Warn($"{player.ToStringSafe()} already in {VoiceOverrideRoom.GetScriptPathInScene()}");
                return;
            }

            if (!VoiceOverrideRoom.EnterRoom(player, null)) {
                Error($"Failed adding {player.ToStringSafe()} to {VoiceOverrideRoom.GetScriptPathInScene()}");
            }
        }

        private void RemovePlayerFromRoom(VRCPlayerApi player) {
            if (!VoiceOverrideRoom.Contains(player)
                || !VoiceOverrideRoom.ExitRoom(player, null)) {
                Warn($"{player.ToStringSafe()} was not in the {VoiceOverrideRoom.GetScriptPathInScene()}");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{player.ToStringSafe()} has successfully left the {VoiceOverrideRoom.GetScriptPathInScene()}");
#endif
            #endregion
        }
        #endregion
    }
}