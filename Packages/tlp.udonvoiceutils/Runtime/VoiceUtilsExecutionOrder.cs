using TLP.UdonUtils;
using TLP.UdonUtils.Runtime;

namespace TLP.UdonVoiceUtils.Runtime
{
    public static class VoiceUtilsExecutionOrder
    {
        public const int VoiceUtilsOrderOffset = 0;
        public const int AudioStart = TlpExecutionOrder.AudioStart + VoiceUtilsOrderOffset;
    }
}