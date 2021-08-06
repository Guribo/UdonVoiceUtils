using Guribo.UdonBetterAudio.Runtime.Examples;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Editor
{
    [CustomEditor(typeof(VoiceOverrideRoomEnterButton))]
    public class VoiceOverrideEnterButtonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();
        }

        public void OnSceneGUI()
        {
            var voiceOverrideRoomEnterButton = (VoiceOverrideRoomEnterButton) target;
            if (!Utilities.IsValid(voiceOverrideRoomEnterButton))
            {
                return;
            }

            var doorTransform = voiceOverrideRoomEnterButton.transform;
            var doorPosition = doorTransform.position;

            Event guiEvent = Event.current;

            switch (guiEvent.type)
            {
                case EventType.Repaint:
                {
                    if (Utilities.IsValid(voiceOverrideRoomEnterButton.voiceOverrideRoom))
                    {
                        var roomPosition = voiceOverrideRoomEnterButton.voiceOverrideRoom.transform.position;

                        Handles.color = Color.white;
                        Handles.DrawDottedLine(doorPosition, roomPosition, 2);
                        
                        GUI.color = Color.white;
                        Handles.Label(roomPosition, voiceOverrideRoomEnterButton.voiceOverrideRoom.gameObject.name);
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
                    HandleInput(guiEvent, voiceOverrideRoomEnterButton);
                }
                    break;
            }
        }


        void HandleInput(Event guiEvent, VoiceOverrideRoomEnterButton voiceOverrideRoomEnterButton)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            Vector3 mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftMouseDown(mousePosition, voiceOverrideRoomEnterButton);
            }
        }

        void HandleLeftMouseDown(Vector3 mousePosition, VoiceOverrideRoomEnterButton voiceOverrideRoomEnterButton)
        {
            if (Utilities.IsValid(voiceOverrideRoomEnterButton.voiceOverrideRoom))
            {
                var roomGuiPosition =
                    HandleUtility.WorldToGUIPoint(voiceOverrideRoomEnterButton.voiceOverrideRoom.transform.position);
                var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
                var clickCloseToRoomGameObject = Vector2.Distance(roomGuiPosition, mouseGuiPosition) < 10f;
                if (clickCloseToRoomGameObject)
                {
                    Selection.SetActiveObjectWithContext(voiceOverrideRoomEnterButton.voiceOverrideRoom.gameObject,
                        voiceOverrideRoomEnterButton.voiceOverrideRoom);
                    EditorGUIUtility.PingObject(voiceOverrideRoomEnterButton.voiceOverrideRoom);
                }
            }
        }
    }
}