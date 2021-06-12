#if !COMPILER_UDONSHARP && UNITY_EDITOR
using Guribo.UdonUtils.Scripts.Common.Editor;
using UnityEditor;

namespace Guribo.UdonBetterAudio.Scripts.Editor
{
    [CustomEditor(typeof(BetterPlayerAudio))]
    public class BetterPlayerAudioEditor : UdonLibraryEditor
    {
        protected override string GetSymbolName()
        {
            return "playerAudio";
        }
    }
}

#endif