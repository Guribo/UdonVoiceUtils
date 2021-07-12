#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Guribo.UdonBetterAudio.Editor
{
    /// <summary>
    /// Adds the given define symbols to PlayerSettings define symbols.
    /// Just add your own define symbols to the Symbols property at the below.
    ///
    /// Original available under MIT License @
    /// https://github.com/UnityCommunity/UnityLibrary/blob/ac3ae833ee4b1636c521ca01b7e2d0c452fe37e7/Assets/Scripts/Editor/AddDefineSymbols.cs
    /// </summary>
    [InitializeOnLoad]
    public class BetterAudioSymbols : UnityEditor.Editor
    {
        public static readonly string[] Symbols =
        {
            "GURIBO_BA",
            "GURIBO_BPA"
        };


        /// <summary>
        /// Add define symbols as soon as Unity gets done compiling.
        /// </summary>
        static BetterAudioSymbols()
        {
            var definesString =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var allDefines = definesString.Split(';').ToList();

            allDefines.AddRange(Symbols.Except(allDefines));
            var uniqueEntries = new HashSet<string>(allDefines);
            allDefines = uniqueEntries.ToList();
            allDefines.Sort();

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", allDefines.ToArray()));
        }
    }
}
#endif