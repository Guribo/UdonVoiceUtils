#if !COMPILER_UDONSHARP && UNITY_EDITOR
using Guribo.UdonBetterAudio.Runtime;
using Guribo.UdonUtils.Editor;
using UnityEditor;

namespace Guribo.UdonBetterAudio.Editor
{
    [CustomEditor(typeof(BetterPlayerAudio))]
    public class BetterPlayerAudioEditor : UdonLibraryEditor
    {
        protected override string GetSymbolName()
        {
            return "betterPlayerAudio";
        }
    }
}
#endif
