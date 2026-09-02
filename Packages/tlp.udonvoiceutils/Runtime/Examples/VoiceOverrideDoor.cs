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
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [TlpDefaultExecutionOrder(typeof(VoiceOverrideDoor), ExecutionOrder)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class VoiceOverrideDoor : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

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

        #endregion

        #region Local Player behaviour
        public override void OnPlayerRespawn(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnPlayerRespawn)}: {player._ToStringSafe()}");
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(OnPlayerRespawn)}: Not initialized");
                return;
            }

            if (!player._IsLocalSafe()) {
#if TLP_DEBUG
                _Warn($"Ignore respawn of irrelevant {player._ToStringSafe()}");
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
            _DebugLog($"{nameof(OnPlayerTriggerEnter)}: {player._ToStringSafe()}");
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(OnPlayerTriggerEnter)}: Not initialized");
                return;
            }

            if (!player._IsLocalSafe()) {
#if TLP_DEBUG
                _DebugLog($"{nameof(OnPlayerTriggerEnter)}: ignoring {player._ToStringSafe()}");
#endif
                return;
            }

            _enterPosition = transform.InverseTransformPoint(player.GetPosition() + _playerColliderGroundOffset);

            if (!LeaveOnTouch) {
                return;
            }

            if (VoiceOverrideRoom._Contains(player)) {
                VoiceOverrideRoom._ExitRoom(player, null);
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnPlayerTriggerExit)}: {player._ToStringSafe()}");
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(OnPlayerTriggerExit)}: Not initialized");
                return;
            }
            if (!player._IsLocalSafe()) {
#if TLP_DEBUG
                _DebugLog($"{nameof(OnPlayerTriggerExit)}: ignoring {player._ToStringSafe()}");
#endif
                return;
            }

            if (_enterPosition == Vector3.zero) {
                _Error(
                        $"{player._ToStringSafe()} was not touching and is being ignored. Udon missed {nameof(OnPlayerTriggerEnter)} event!?");
                return;
            }

            var exitPosition = transform.InverseTransformPoint(player.GetPosition() + _playerColliderGroundOffset);
            var enterPosition = _enterPosition;
            _enterPosition = Vector3.zero;

            if (_HasExitedDoor(enterPosition, exitPosition, ExitDirection)) {
                _RemovePlayerFromRoom(player);
                return;
            }

            _AddPlayerToRoom(player);
        }


        /// <summary>
        ///
        /// </summary>
        /// <returns>true if the local player is currently in contact with the trigger</returns>
        public bool _IsLocalPlayerTouching() {
            return _enterPosition != Vector3.zero;
        }
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(VoiceOverrideRoom)) {
                _Error($"{nameof(VoiceOverrideRoom)} not set");
                return false;
            }

            // prevent ui raycast from being blocked by the door
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            var trigger = gameObject.GetComponent<BoxCollider>();
            if (!Utilities.IsValid(trigger)) {
                _Error($"{nameof(BoxCollider)} missing on {transform._GetPathInScene()}");
                return false;
            }

            if (!trigger.isTrigger) {
                trigger.isTrigger = true;
                _Warn($"{nameof(BoxCollider)} was changed to be trigger");
            }

            if (trigger.center != Vector3.zero) {
                trigger.center = Vector3.zero;
                _Warn($"{nameof(BoxCollider)} center was changed to Vector3.zero");
            }

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
        internal bool _HasExitedDoor(Vector3 enterPosition, Vector3 exitPosition, Vector3 leaveDirection) {
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

        private void _AddPlayerToRoom(VRCPlayerApi player) {
            if (VoiceOverrideRoom._Contains(player)) {
                _Warn($"{player._ToStringSafe()} already in {VoiceOverrideRoom._GetScriptPathInScene()}");
                return;
            }

            if (!VoiceOverrideRoom._EnterRoom(player, null)) {
                _Error($"Failed adding {player._ToStringSafe()} to {VoiceOverrideRoom._GetScriptPathInScene()}");
            }
        }

        private void _RemovePlayerFromRoom(VRCPlayerApi player) {
            if (!VoiceOverrideRoom._Contains(player)
                || !VoiceOverrideRoom._ExitRoom(player, null)) {
                _Warn($"{player._ToStringSafe()} was not in the {VoiceOverrideRoom._GetScriptPathInScene()}");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{player._ToStringSafe()} has successfully left the {VoiceOverrideRoom._GetScriptPathInScene()}");
#endif
            #endregion
        }
        #endregion
    }
}