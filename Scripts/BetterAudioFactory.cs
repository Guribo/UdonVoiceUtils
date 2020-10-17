using UdonSharp;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterAudioFactory : UdonSharpBehaviour
    {
        public GameObject audioSourcePrefab;

        public GameObject CreateAudioSourceInstance()
        {
            Debug.Log("BetterAudioFactory.CreateAudioSourceInstance");
            if (audioSourcePrefab.activeSelf)
            {
                Debug.LogError("BetterAudioFactory.CreateAudioSourceInstance: audioSourcePrefab prefab must be inactive");
                return null;
            }
            
            return VRCInstantiate(audioSourcePrefab);
        }
    }
}