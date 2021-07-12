using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

namespace Guribo.UdonBetterAudio.Editor
{
    public class BetterAudioPackageExporterWithTests : BetterAudioPackageExporterWithAsmdef
    {
        [MenuItem("Guribo/BetterAudioPackageExporter/Export/Unity Package with Tests")]
        public new static void ExportPackage()
        {
            Export<BetterAudioPackageExporterWithTests>();
        }
        
        protected override string GetUnityPackage()
        {
            return "UdonBetterPlayerAudio_with_Tests";
        }

        protected override string[] GetExportAssets()
        {
            var runtime = new List<string>()
            {
                // udon utils
                "Assets/Guribo/UdonUtils/Runtime/Testing/TestController.asset",
                "Assets/Guribo/UdonUtils/Runtime/Testing/TestController.cs",
                "Assets/Guribo/UdonUtils/Runtime/Testing/TestTemplate.asset",
                "Assets/Guribo/UdonUtils/Runtime/Testing/TestTemplate.cs",
                
                "Assets/Guribo/UdonUtils/Runtime/AssemblyInfo.cs",

                // better player audio
                "Assets/Guribo/UdonBetterAudio/Runtime/AssemblyInfo.cs",
            };
            
            var tests = new List<string>()
            {
                // udon utils
                "Assets/Guribo/UdonUtils/Tests/Editor/Guribo.UdonUtils.Tests.Editor.asmdef",
                "Assets/Guribo/UdonUtils/Tests/Editor/TestPlayerList.cs",
                
                "Assets/Guribo/UdonUtils/Tests/Runtime/Utils/UdonTestUtils.cs",
                "Assets/Guribo/UdonUtils/Tests/Runtime/Guribo.UdonUtils.Tests.Runtime.asmdef",
                
                // better player audio
                "Assets/Guribo/UdonBetterAudio/Tests/Editor/Guribo.UdonBetterAudio.Tests.Editor.asmdef",
                
                "Assets/Guribo/UdonBetterAudio/Tests/Editor/TestBetterPlayerAudio.cs",
                "Assets/Guribo/UdonBetterAudio/Tests/Editor/TestOverrideZoneEnterExit.cs",
                
                "Assets/Guribo/UdonBetterAudio/Tests/Runtime/BetterPlayerAudio/BetterPlayerAudioVoiceFalloffTest.asset",
                "Assets/Guribo/UdonBetterAudio/Tests/Runtime/BetterPlayerAudio/BetterPlayerAudioVoiceFalloffTest.cs",
                
                "Assets/Guribo/UdonBetterAudio/Tests/Runtime/BetterPlayerAudio/Guribo.UdonBetterAudio.Tests.Runtime.BetterPlayerAudio.asmdef",
                "Assets/Guribo/UdonBetterAudio/Tests/Runtime/BetterPlayerAudio/TestZoneEnteringNetworked.asset",
                "Assets/Guribo/UdonBetterAudio/Tests/Runtime/BetterPlayerAudio/TestZoneEnteringNetworked.cs",
            };

            return base.GetExportAssets().ToList()
                .Append(runtime)
                .Append(tests)
                .ToArray();
        }
    }
}