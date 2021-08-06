#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Guribo.UdonUtils.Editor;
using UnityEditor;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

namespace Guribo.UdonBetterAudio.Editor
{
    public class BetterAudioPackageExporter : PackageExporter
    {
        [MenuItem("Guribo/BetterAudioPackageExporter/Export/Unity Package")]
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
            var editor = new List<string>()
            {
                // udon utils
                "Assets/Guribo/UdonUtils/Editor/CompileSymbols.cs",
                "Assets/Guribo/UdonUtils/Editor/GuriboUdonUtils.cs",
                "Assets/Guribo/UdonUtils/Editor/OwnershipTransferEditor.cs",
                "Assets/Guribo/UdonUtils/Editor/UdonBehaviourExtensions.cs",
                "Assets/Guribo/UdonUtils/Editor/UdonCommonEditor.cs",
                "Assets/Guribo/UdonUtils/Editor/UdonDebugEditor.cs",
                "Assets/Guribo/UdonUtils/Editor/UdonLibraryEditor.cs",
                "Assets/Guribo/UdonUtils/Editor/UdonMathEditor.cs",

                // better player audio
                "Assets/Guribo/UdonBetterAudio/Editor/BetterAudioSymbols.cs",
                "Assets/Guribo/UdonBetterAudio/Editor/BetterPlayerAudioEditor.cs",
                "Assets/Guribo/UdonBetterAudio/Editor/VoiceOverrideDoorEditor.cs",
                "Assets/Guribo/UdonBetterAudio/Editor/VoiceOverrideRoomEditor.cs",
                "Assets/Guribo/UdonBetterAudio/Editor/VoiceOverrideRoomEnterButtonEditor.cs",
                "Assets/Guribo/UdonBetterAudio/Editor/VoiceOverrideRoomExitButtonEditor.cs",
            };
            
            var examples = new List<string>()
            {
                "Assets/Guribo/UdonBetterAudio/Examples"
            };

            var materials = new List<string>()
            {
                "Assets/Guribo/UdonBetterAudio/Materials/MetalBlack.mat",
                "Assets/Guribo/UdonBetterAudio/Materials/PlasticWhite.mat",
                "Assets/Guribo/UdonBetterAudio/Materials/TriggerAreaMatBlue.mat",
                "Assets/Guribo/UdonBetterAudio/Materials/TriggerAreaMatGreen.mat",
                "Assets/Guribo/UdonBetterAudio/Materials/TriggerAreaMatOrange.mat",
                "Assets/Guribo/UdonBetterAudio/Materials/TriggerAreaMatRed.mat",
                "Assets/Guribo/UdonBetterAudio/Materials/TriggerAreaMatYellow.mat"
            };

            var postProcessing = new List<string>()
            {
                "Assets/Guribo/UdonBetterAudio/PostProcessing/PPUnderWater.asset"
            };

            var prefabs = new List<string>()
            {
                // udon utils
                "Assets/Guribo/UdonUtils/Prefabs/UdonCommon.prefab",

                // better player audio
                "Assets/Guribo/UdonBetterAudio/Prefabs/Examples/Bathroom.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/Examples/ConcertHall.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/Examples/OpenDoorExample.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/Examples/PickupMicrophone.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/Examples/Pool.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/Examples/VoiceOverrideRoom.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/Examples/VoiceOverrideTriggerZone.prefab",

                "Assets/Guribo/UdonBetterAudio/Prefabs/AutoPlayerRange.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/BetterPlayerAudio.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/BetterPlayerAudioController.prefab",
                "Assets/Guribo/UdonBetterAudio/Prefabs/BetterPlayerAudioMenu.prefab",
            };
            
            var runtime = new List<string>()
            {
                // udon utils
                "Assets/Guribo/UdonUtils/Runtime/Common/Networking/OwnerOnly.asset",
                "Assets/Guribo/UdonUtils/Runtime/Common/Networking/OwnerOnly.cs",
                "Assets/Guribo/UdonUtils/Runtime/Common/Networking/OwnershipTransfer.asset",
                "Assets/Guribo/UdonUtils/Runtime/Common/Networking/OwnershipTransfer.cs",
                "Assets/Guribo/UdonUtils/Runtime/Common/Networking/SyncedInteger.asset",
                "Assets/Guribo/UdonUtils/Runtime/Common/Networking/SyncedInteger.cs",
                "Assets/Guribo/UdonUtils/Runtime/Common/Networking/SyncedIntegerArray.asset",
                "Assets/Guribo/UdonUtils/Runtime/Common/Networking/SyncedIntegerArray.cs",
                "Assets/Guribo/UdonUtils/Runtime/Common/PlayerList.asset",
                "Assets/Guribo/UdonUtils/Runtime/Common/PlayerList.cs",
                "Assets/Guribo/UdonUtils/Runtime/Common/ReflectionProbeController.asset",
                "Assets/Guribo/UdonUtils/Runtime/Common/ReflectionProbeController.cs",
                "Assets/Guribo/UdonUtils/Runtime/Common/UdonCommon.asset",
                "Assets/Guribo/UdonUtils/Runtime/Common/UdonCommon.cs",
                "Assets/Guribo/UdonUtils/Runtime/Common/UdonDebug.asset",
                "Assets/Guribo/UdonUtils/Runtime/Common/UdonDebug.cs",
                "Assets/Guribo/UdonUtils/Runtime/Common/UdonMath.asset",
                "Assets/Guribo/UdonUtils/Runtime/Common/UdonMath.cs",

                // better player audio
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/AutoPlayerRange.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/AutoPlayerRange.cs",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/PickupMicrophone.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/PickupMicrophone.cs",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideDoor.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideDoor.cs",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideRoom.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideRoom.cs",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideRoomEnterButton.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideRoomEnterButton.cs",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideRoomExitButton.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideRoomExitButton.cs",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideTriggerZone.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/Examples/VoiceOverrideTriggerZone.cs",

                "Assets/Guribo/UdonBetterAudio/Runtime/BetterPlayerAudio.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/BetterPlayerAudio.cs",
                "Assets/Guribo/UdonBetterAudio/Runtime/BetterPlayerAudioOverride.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/BetterPlayerAudioOverride.cs",
                "Assets/Guribo/UdonBetterAudio/Runtime/BetterPlayerAudioOverrideList.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/BetterPlayerAudioOverrideList.cs",
                "Assets/Guribo/UdonBetterAudio/Runtime/BetterPlayerAudioUiController.asset",
                "Assets/Guribo/UdonBetterAudio/Runtime/BetterPlayerAudioUiController.cs",
            };

            var etc = new List<string>()
            {
                // udon utils
                "Assets/Guribo/UdonUtils/LICENSE",
                "Assets/Guribo/UdonUtils/package.json",
                "Assets/Guribo/UdonUtils/Version.txt",

                // better player audio
                "Assets/Guribo/UdonBetterAudio/LICENSE",
                "Assets/Guribo/UdonBetterAudio/package.json",
                "Assets/Guribo/UdonBetterAudio/README.md",
                "Assets/Guribo/UdonBetterAudio/Version.txt",
            };

            return materials
                .Append(editor)
                .Append(postProcessing)
                .Append(prefabs)
                .Append(runtime)
                .Append(examples)
                .Append(etc)
                .ToArray();
        }
    }
}
#endif