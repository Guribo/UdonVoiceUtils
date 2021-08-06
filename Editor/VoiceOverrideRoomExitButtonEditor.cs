using Guribo.UdonBetterAudio.Runtime.Examples;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Editor
{
    [CustomEditor(typeof(VoiceOverrideRoomExitButton))]
    public class VoiceOverrideRoomExitButtonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();
        }

        public void OnSceneGUI()
        {
            var voiceOverrideRoomExitButton = (VoiceOverrideRoomExitButton) target;
            if (!Utilities.IsValid(voiceOverrideRoomExitButton))
            {
                return;
            }

            var doorTransform = voiceOverrideRoomExitButton.transform;
            var doorPosition = doorTransform.position;

            Event guiEvent = Event.current;

            switch (guiEvent.type)
            {
                case EventType.Repaint:
                {
                    if (Utilities.IsValid(voiceOverrideRoomExitButton.voiceOverrideRoom))
                    {
                        var roomPosition = voiceOverrideRoomExitButton.voiceOverrideRoom.transform.position;

                        Handles.color = Color.white;
                        Handles.DrawDottedLine(doorPosition, roomPosition, 2);
                        
                        GUI.color = Color.white;
                        Handles.Label(roomPosition, voiceOverrideRoomExitButton.voiceOverrideRoom.gameObject.name);
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
                    HandleInput(guiEvent, voiceOverrideRoomExitButton);
                }
                    break;
            }
        }


        void HandleInput(Event guiEvent, VoiceOverrideRoomExitButton voiceOverrideRoomExitButton)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            Vector3 mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftMouseDown(mousePosition, voiceOverrideRoomExitButton);
            }
        }

        void HandleLeftMouseDown(Vector3 mousePosition, VoiceOverrideRoomExitButton voiceOverrideRoomExitButton)
        {
            if (Utilities.IsValid(voiceOverrideRoomExitButton.voiceOverrideRoom))
            {
                var roomGuiPosition =
                    HandleUtility.WorldToGUIPoint(voiceOverrideRoomExitButton.voiceOverrideRoom.transform.position);
                var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
                var clickCloseToRoomGameObject = Vector2.Distance(roomGuiPosition, mouseGuiPosition) < 10f;
                if (clickCloseToRoomGameObject)
                {
                    Selection.SetActiveObjectWithContext(voiceOverrideRoomExitButton.voiceOverrideRoom.gameObject,
                        voiceOverrideRoomExitButton.voiceOverrideRoom);
                    EditorGUIUtility.PingObject(voiceOverrideRoomExitButton.voiceOverrideRoom);
                }
            }
        }
    }
}