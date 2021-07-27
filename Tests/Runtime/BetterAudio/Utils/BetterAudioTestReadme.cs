#if UNITY_EDITOR
using UnityEngine;

namespace Guribo.UdonBetterAudio.Tests.Runtime.BetterAudio.Utils
{
    public class BetterAudioTestReadme : MonoBehaviour
    {
        [TextArea(3, 20)] [Tooltip("README")]
        public string information = "README";
    }
}

#endif