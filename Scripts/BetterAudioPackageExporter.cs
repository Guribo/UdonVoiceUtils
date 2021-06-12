#if UNITY_EDITOR
using System.Collections.Generic;
using Guribo.UdonUtils.Scripts;
using Guribo.UdonUtils.Scripts.Editor;
using UnityEditor;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterAudioPackageExporter : PackageExporter
    {
        [MenuItem("Guribo/UDON/BetterAudioPackageExporter/Export Unity Package")]
        public new static void ExportPackage()
        {
            Export<BetterAudioPackageExporter>();
        }

        protected override string GetRepositoryPath()
        {
            return "./Assets/Guribo/UdonBetterAudio";
        }

        protected override string GetExportPath()
        {
            return "./Assets/Guribo/UdonBetterAudio/Releases/";
        }

        protected override string GetReleaseVersion()
        {
            return "Version.txt";
        }

        protected override string GetUnityPackage()
        {
            return "UdonBetterPlayerAudio";
        }

        protected override string[] GetExportAssets()
        {
            return new[]
            {
                "Assets/Guribo/UdonBetterAudio/LICENSE",
                "Assets/Guribo/UdonBetterAudio/README.md",
                "Assets/Guribo/UdonBetterAudio/Version.txt",

                "Assets/Guribo/UdonBetterAudio/Materials/MetalBlack.mat",
                "Assets/Guribo/UdonBetterAudio/Materials/PlasticWhite.mat",

                "Assets/Guribo/UdonBetterAudio/Prefabs/BetterPlayerAudio.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/BetterPlayerAudioController.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/BetterPlayerAudioMenu.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/Examples/Microphone.prefab",

                "Assets/Guribo/UdonBetterAudio/Scenes/UdonBetterPlayerAudio",
                "Assets/Guribo/UdonBetterAudio/Scenes/UdonBetterPlayerAudio_MicPickup",
                "Assets/Guribo/UdonBetterAudio/Scenes/UdonBetterPlayerAudio_NoMenu",
                "Assets/Guribo/UdonBetterAudio/Scenes/UdonBetterPlayerAudio_NoMenu_MicPickup",

                "Assets/Guribo/UdonBetterAudio/Scenes/UdonBetterPlayerAudio.unity",
                "Assets/Guribo/UdonBetterAudio/Scenes/UdonBetterPlayerAudio_MicPickup.unity",
                "Assets/Guribo/UdonBetterAudio/Scenes/UdonBetterPlayerAudio_NoMenu.unity",
                "Assets/Guribo/UdonBetterAudio/Scenes/UdonBetterPlayerAudio_NoMenu_MicPickup.unity",

                "Assets/Guribo/UdonBetterAudio/Scripts/BetterPlayerAudio.asset",
                "Assets/Guribo/UdonBetterAudio/Scripts/BetterPlayerAudio.cs",
                "Assets/Guribo/UdonBetterAudio/Scripts/BetterPlayerAudioOverride.asset",
                "Assets/Guribo/UdonBetterAudio/Scripts/BetterPlayerAudioOverride.cs",
                "Assets/Guribo/UdonBetterAudio/Scripts/BetterPlayerAudioUiController.asset",
                "Assets/Guribo/UdonBetterAudio/Scripts/BetterPlayerAudioUiController.cs",
                "Assets/Guribo/UdonBetterAudio/Scripts/OwnershipTransfer.asset",
                "Assets/Guribo/UdonBetterAudio/Scripts/OwnershipTransfer.cs",

                "Assets/Guribo/UdonBetterAudio/Scripts/Examples",
                
                "Assets/Guribo/UdonBetterAudio/Tutorials",

                // UdonUtils
                "Assets/Guribo/UdonUtils/Scripts",
                "Assets/Guribo/UdonUtils/LICENSE",
                "Assets/Guribo/UdonUtils/Version.txt"
            };
        }
    }
}

#endif