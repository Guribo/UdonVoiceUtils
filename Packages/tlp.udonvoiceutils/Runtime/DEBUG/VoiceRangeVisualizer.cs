using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Adapters.Cyan;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Player;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonVoiceUtils.Runtime.Debugging
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class VoiceRangeVisualizer : CyanPooledObject
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioController.ExecutionOrder + 1;

        #region State
        private TrackingDataFollowerUI _playerFollower;
        #endregion


        [SerializeField]
        private Transform Near;

        [SerializeField]
        private Transform Far;

        [SerializeField]
        private Transform NearField;

        private PlayerAudioController _playerAudioController;
        private int _playerId = -1;
        public bool Initialized { private set; get; }

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            _playerFollower = GetComponent<TrackingDataFollowerUI>();
            if (!Utilities.IsValid(_playerFollower)) {
                Error($"{nameof(TrackingDataFollowerUI)} component missing");
                return false;
            }

            _playerAudioController = VoiceUtils.FindPlayerAudioController();
            if (!Utilities.IsValid(_playerAudioController)) {
                Error($"{nameof(_playerAudioController)} not found");
                return false;
            }

            Initialized = true;
            return true;
        }

        public override void _OnOwnerSet() {
            base._OnOwnerSet();

            if (!Initialized && !SetupAndValidate()) {
                Error("Not initialized");
                return;
            }

            _playerId = Owner.PlayerIdSafe();
            if (!Utilities.IsValid(Owner)) {
                Error($"{nameof(Owner)} is invalid");
                return;
            }

            if (!Utilities.IsValid(_playerAudioController)) {
                Error($"{nameof(_playerAudioController)} is invalid");
                return;
            }

            _playerAudioController.AddPlayerUpdateListener(this, _playerId);
            gameObject.GetComponent<TrackingDataFollower>().Player = Owner;
        }

        public override void _OnCleanup() {
            base._OnCleanup();
            if (!Initialized) {
                Error("Not initialized");
                return;
            }

            gameObject.SetActive(false);
            if (!Utilities.IsValid(_playerAudioController)) {
                Error($"{nameof(_playerAudioController)} is invalid");
                return;
            }

            if (!_playerAudioController.PlayerUpdateListeners.Remove(_playerId)) {
                Error($"Failed to remove listener of player {_playerId}");
            }

            _playerId = -1;
        }

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(VoiceValuesUpdate):
                    VoiceValuesUpdate();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Events
        private void VoiceValuesUpdate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(VoiceValuesUpdate));
#endif
            #endregion

            if (!Utilities.IsValid(Owner)) {
                return;
            }

            gameObject.SetActive(!Owner.isLocal);

            if (Near) {
                float voiceDistanceNear = Owner.GetVoiceDistanceNear();
                Near.localScale = new Vector3(voiceDistanceNear, voiceDistanceNear, voiceDistanceNear);
            }

            if (Far) {
                float voiceDistanceFar = Owner.GetVoiceDistanceFar();
                Far.localScale = new Vector3(voiceDistanceFar, voiceDistanceFar, voiceDistanceFar);
            }

            if (NearField) {
                float voiceVolumetricRadius = Owner.GetVoiceVolumetricRadius();
                NearField.localScale = new Vector3(voiceVolumetricRadius, voiceVolumetricRadius, voiceVolumetricRadius);
            }
        }
        #endregion
    }
}