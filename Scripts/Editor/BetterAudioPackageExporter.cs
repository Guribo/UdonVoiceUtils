#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Guribo.UdonBetterAudio.Scripts.Editor
{
    public class BetterAudioPackageExporter : MonoBehaviour
    {
        private const string RepositoryPath = "./Assets/Guribo/UdonBetterAudio";
        private const string ExportPath = "./Assets/Guribo/UdonBetterAudio/Releases/";
        private const string ReleaseVersion = "Version.txt";

        private const string UnityPackage = "UdonBetterPlayerAudio";

        private const string UnityPackageExtension = "unitypackage";

        private static readonly string[] ExportAssets =
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

            "Assets/Guribo/UdonBetterAudio/Scripts/Examples/PickupMicrophone.asset",
            "Assets/Guribo/UdonBetterAudio/Scripts/Examples/PickupMicrophone.cs"
        };

        /// <summary>
        /// exports the unity package after successfully building
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pathToBuiltProject"></param>
        /// <exception cref="Exception"></exception>
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            ExportBetterAudioPackage(pathToBuiltProject);
        }

        [MenuItem("Guribo/UDON/BetterPlayerAudio/Export Unity Package")]
        public static void ExportBetterPlayerAudioPackage()
        {
            try
            {
                ExportBetterAudioPackage(ExportPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        public static string GetCommitHash()
        {
            var cmd = new Process();
            var processStartInfo = cmd.StartInfo;
            processStartInfo.FileName = "cmd.exe";
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            cmd.Start();
            var cmdStandardInput = cmd.StandardInput;
            cmdStandardInput.Flush();
            cmdStandardInput.WriteLine($"cd {RepositoryPath} && git rev-parse --short HEAD");
            cmdStandardInput.Flush();
            cmdStandardInput.Close();
            cmd.WaitForExit();
            var cmdStandardOutput = cmd.StandardOutput;

            // ignore the first 4 lines
            for (var i = 0; i < 4; ++i)
            {
                cmdStandardOutput.ReadLine();
            }

            // get the commit hash
            var commitHash = cmdStandardOutput.ReadLine();
            
            // ignore the rest
            cmdStandardOutput.ReadToEnd();
            
            // verify we actually succeeded
            if (cmd.ExitCode != 0 || string.IsNullOrWhiteSpace(commitHash))
            {
                throw new Exception("Failed to get git hash for repository '{RepositoryPath}'");
            }

            var trimmedHash = commitHash.Trim();
            Debug.Log($"Found git hash '{trimmedHash}'");

            return trimmedHash;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathToBuiltProject"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        private static void ExportBetterAudioPackage(string pathToBuiltProject)
        {
            Debug.Assert(!string.IsNullOrEmpty(pathToBuiltProject)
                         && !string.IsNullOrWhiteSpace(pathToBuiltProject),
                $"Invalid path {pathToBuiltProject}");
            // absolute path of built project
            var buildDirectory = Directory.GetParent(@pathToBuiltProject).FullName;

            // absolute path of unity project
            var workingDirectory = Path.GetFullPath(@Environment.CurrentDirectory);

            Debug.Log($"Exporting BetterPlayerAudio Unity Package to '{buildDirectory}' from '{workingDirectory}'");

            var versionFilePath = $"{RepositoryPath}/{ReleaseVersion}";
            var version = File.ReadLines(versionFilePath).First();
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new Exception($"Failed to get version from '{versionFilePath}'");
            }

            var packageTargetPath = Path.Combine(buildDirectory,
                $"{UnityPackage}_{version.Trim()}_{GetCommitHash()}.{UnityPackageExtension}");

            Debug.Log($"Exporting to '{packageTargetPath}'");

            foreach (var exportAsset in ExportAssets)
            {
                if (!IsValidFileName(exportAsset))
                {
                    throw new ArgumentException($"Invalid file path: '{exportAsset}'");
                }
            }

            // AssetDatabase.ExportPackage(ExportAssets, packageTargetPath,ExportPackageOptions.Recurse | ExportPackageOptions.Interactive | ExportPackageOptions.IncludeDependencies);
            AssetDatabase.ExportPackage(ExportAssets, packageTargetPath,ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
            if (!File.Exists(packageTargetPath))
            {
                throw new Exception($"Failed to export to '{packageTargetPath}'");
            }
        }

        /// <summary>
        /// <remarks>Source: https://stackoverflow.com/questions/422090/in-c-sharp-check-that-filename-is-possibly-valid-not-that-it-exists</remarks>
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(fileName);
            }
            catch (Exception)
            {
                // ignored
            }

            return !ReferenceEquals(fileInfo, null);
        }
    }
}

#endif