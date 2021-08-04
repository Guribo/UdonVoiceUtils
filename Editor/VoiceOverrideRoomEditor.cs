using System;
using Guribo.UdonBetterAudio.Runtime.Examples;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Guribo.UdonBetterAudio.Editor
{
    [CustomEditor(typeof(VoiceOverrideRoom))]
    public class VoiceOverrideRoomEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();
        }

        public void OnSceneGUI()
        {
            var voiceOverrideRoom = (VoiceOverrideRoom) target;
            if (!Utilities.IsValid(voiceOverrideRoom))
            {
                return;
            }

            Event guiEvent = Event.current;

            switch (guiEvent.type)
            {
                case EventType.Repaint:
                {
                    // draw lines to each door
                    foreach (var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                       var myComponents = rootGameObject.GetUdonSharpComponentsInChildren<VoiceOverrideDoor>();
                    
                        foreach (var voiceOverrideDoor in myComponents)
                        {
                            if (voiceOverrideDoor.voiceOverrideRoom != voiceOverrideRoom)
                            {
                                continue;
                            }
                        
                            VoiceOverrideDoorEditor.DrawDoorInfo(voiceOverrideDoor);
                        
                            if (Utilities.IsValid(voiceOverrideDoor))
                            {
                                var doorPosition = voiceOverrideDoor.transform.position;

                                Handles.color = Color.white;
                                Handles.DrawDottedLine(voiceOverrideRoom.transform.position, doorPosition, 2);
                                
                                GUI.color = Color.white;
                                Handles.Label(doorPosition, voiceOverrideDoor.gameObject.name);
                            }
                        }
                    }
                    
                }
                    break;
                case EventType.Layout:
                {
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                }
                    break;
                default:
                {
                    foreach (var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        var myComponents = rootGameObject.GetUdonSharpComponentsInChildren<VoiceOverrideDoor>();

                        foreach (var voiceOverrideDoor in myComponents)
                        {
                            if (voiceOverrideDoor.voiceOverrideRoom == voiceOverrideRoom)
                            {
                                HandleInput(guiEvent, voiceOverrideRoom, voiceOverrideDoor);
                            }
                        }
                    }
                }
                    break;
            }
        }

        void HandleInput(Event guiEvent, VoiceOverrideRoom voiceOverrideRoom,VoiceOverrideDoor voiceOverrideDoor)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            Vector3 mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftMouseDown(mousePosition, voiceOverrideDoor);
            }
        }

        void HandleLeftMouseDown(Vector3 mousePosition, VoiceOverrideDoor voiceOverrideDoor)
        {
            if (Utilities.IsValid(voiceOverrideDoor))
            {
                var roomGuiPosition =
                    HandleUtility.WorldToGUIPoint(voiceOverrideDoor.transform.position);
                var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
                var clickCloseToRoomGameObject = Vector2.Distance(roomGuiPosition, mouseGuiPosition) < 10f;
                if (clickCloseToRoomGameObject)
                {
                    Selection.SetActiveObjectWithContext(voiceOverrideDoor.gameObject,
                        voiceOverrideDoor);
                    EditorGUIUtility.PingObject(voiceOverrideDoor);
                }
            }
        }
    }
}