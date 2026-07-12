using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class TestGridSpawner : UdonSharpBehaviour
    {
        public WorldAudioController WorldAudioController;
        public GameObject Prefab;

        public float Spacing = 10f;
        public int SizeX = 10;
        public int SizeY = 0;
        public int SizeZ = 10;

        private int _x;
        
        private void Start() {
            _x = 0;
            SendCustomEventDelayedFrames(nameof(Delayed_SpawnBatch), 10);
        }

        public void Delayed_SpawnBatch() {
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
                SendCustomEventDelayedFrames(nameof(Delayed_SpawnBatch), 1);
            }
        }
    }
}