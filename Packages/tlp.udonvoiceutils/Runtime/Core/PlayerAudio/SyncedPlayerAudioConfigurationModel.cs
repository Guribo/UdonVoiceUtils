using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class SyncedPlayerAudioConfigurationModel : PlayerAudioConfigurationModel
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioConfigurationModel.ExecutionOrder + 1;

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            // ensuring that ownership always remains with master
            if (!this._TransferOwnershipToMaster()) {
                _Error("Failed to transfer ownership to current master");
                return false;
            }

            return true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnOwnershipTransferred)} to '{player._DisplayNameSafe()}'");
#endif
            #endregion

            base.OnOwnershipTransferred(player);

            if (player._IsMasterSafe()) {
                return;
            }

            _Warn("Not owned by master, attempting to return ownership to current master");
            if (!this._TransferOwnershipToMaster()) {
                _Error("Failed to transfer ownership to current master");
            }
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner) {
            return requestedOwner._IsMasterSafe();
        }
    }
}