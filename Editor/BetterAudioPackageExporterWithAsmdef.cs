using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

namespace Guribo.UdonBetterAudio.Editor
{
    public class BetterAudioPackageExporterWithAsmdef : BetterAudioPackageExporter
    {
        [MenuItem("Guribo/BetterAudioPackageExporter/Export/Unity Package with Assembly Definitions")]
        public new static void ExportPackage()
        {
            Export<BetterAudioPackageExporterWithAsmdef>();
        }
        
        protected override string GetUnityPackage()
        {
            return "UdonBetterPlayerAudio_with_AssemblyDefinitions";
        }

        protected override string[] GetExportAssets()
        {
            var editor = new List<string>()
            {
                // udon utils
                "Assets/Guribo/UdonUtils/Editor/Guribo.UdonUtils.Editor.asmdef",
                
                // better player audio
                "Assets/Guribo/UdonBetterAudio/Editor/Guribo.UdonBetterAudio.Editor.asmdef"
            };

            var runtime = new List<string>()
            {
                // udon utils
                "Assets/Guribo/UdonUtils/Runtime/Guribo.UdonUtils.Runtime.asmdef",
                
                // better player audio
                "Assets/Guribo/UdonBetterAudio/Runtime/Guribo.UdonBetterAudio.Runtime.asmdef",
            };


            return base.GetExportAssets().ToList()
                .Append(editor)
                .Append(runtime)
                .ToArray();
        }
    }
}