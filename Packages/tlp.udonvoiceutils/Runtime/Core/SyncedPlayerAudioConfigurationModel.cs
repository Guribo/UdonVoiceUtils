using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class SyncedPlayerAudioConfigurationModel : PlayerAudioConfigurationModel
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerAudioConfigurationModel.ExecutionOrder + 1;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            // ensuring that ownership always remains with master
            if (!this.TransferOwnershipToMaster()) {
                Error("Failed to transfer ownership to current master");
                return false;
            }

            return true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnOwnershipTransferred)} to '{player.DisplayNameSafe()}'");
#endif
            #endregion

            base.OnOwnershipTransferred(player);

            if (player.IsMasterSafe()) {
                return;
            }

            Warn("Not owned by master, attempting to return ownership to current master");
            if (!this.TransferOwnershipToMaster()) {
                Error("Failed to transfer ownership to current master");
            }
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner) {
            return requestedOwner.IsMasterSafe();
        }
    }
}