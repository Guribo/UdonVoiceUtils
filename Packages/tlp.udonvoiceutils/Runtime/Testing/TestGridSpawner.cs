using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class TestGridSpawner : TlpBaseBehaviour
    {
        #region ExecutionOrder
        
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultStart + 1;
        #endregion
        
        public GameObject Prefab;

        public float Spacing = 10f;
        public int SizeX = 10;
        public int SizeY;
        public int SizeZ = 10;

        private int _x;

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            _x = 0;
            SendCustomEventDelayedFrames(nameof(_Delayed_SpawnBatch), 10);

            return true;
        }

        public void _Delayed_SpawnBatch() {
            for (int y = 0; y < SizeY; y++) {
                for (int z = 0; z < SizeZ; z++) {
                    var spawnPoint = transform.position + new Vector3(
                            _x * Spacing,
                            y * Spacing,
                            z * Spacing);
                    var instance = Instantiate(Prefab, spawnPoint, Quaternion.identity);
                    instance.SetActive(true);
                }
            }

            ++_x;
            if (_x < SizeX) {
                SendCustomEventDelayedFrames(nameof(_Delayed_SpawnBatch), 1);
            }
        }
    }
}