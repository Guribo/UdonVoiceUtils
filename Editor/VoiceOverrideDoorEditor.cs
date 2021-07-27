using System;
using Guribo.UdonBetterAudio.Runtime.Examples;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Editor
{
    [CustomEditor(typeof(VoiceOverrideDoor))]
    public class VoiceOverrideDoorEditor : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();
        }
        
        public void OnSceneGUI()
        {
            var voiceOverrideDoor = (VoiceOverrideDoor) target;
            if (!Utilities.IsValid(voiceOverrideDoor))
            {
                return;
            }
            
            var doorTransform = voiceOverrideDoor.transform;
            var doorPosition = doorTransform.position;
            var exitDirection = doorTransform.TransformDirection(voiceOverrideDoor.exitDirection).normalized;
            
            Handles.color = Color.red;
            Handles.DrawDottedLine(doorPosition, doorPosition + exitDirection, 2);

            GUI.color = Color.red;
            Handles.Label(doorPosition + exitDirection, "Outside");
            
            Handles.color = Color.green;
            Handles.DrawDottedLine(doorPosition, doorPosition - exitDirection, 2);
            
            GUI.color = Color.green;
            Handles.Label(doorPosition - exitDirection, "Inside");
        }
    }
}