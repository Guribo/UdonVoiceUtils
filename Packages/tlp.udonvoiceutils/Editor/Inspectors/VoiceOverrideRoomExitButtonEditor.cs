using TLP.UdonVoiceUtils.Runtime.Examples;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
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
            var voiceOverrideRoomExitButton = (VoiceOverrideRoomExitButton)target;
            if (!Utilities.IsValid(voiceOverrideRoomExitButton))
            {
                return;
            }

            var doorTransform = voiceOverrideRoomExitButton.transform;
            var doorPosition = doorTransform.position;

            var guiEvent = Event.current;

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


        private void HandleInput(Event guiEvent, VoiceOverrideRoomExitButton voiceOverrideRoomExitButton)
        {
            var mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            var mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftMouseDown(mousePosition, voiceOverrideRoomExitButton);
            }
        }

        private void HandleLeftMouseDown(Vector3 mousePosition, VoiceOverrideRoomExitButton voiceOverrideRoomExitButton)
        {
            if (Utilities.IsValid(voiceOverrideRoomExitButton.voiceOverrideRoom))
            {
                var roomGuiPosition =
                    HandleUtility.WorldToGUIPoint(voiceOverrideRoomExitButton.voiceOverrideRoom.transform.position);
                var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
                bool clickCloseToRoomGameObject = Vector2.Distance(roomGuiPosition, mouseGuiPosition) < 10f;
                if (clickCloseToRoomGameObject)
                {
                    Selection.SetActiveObjectWithContext(
                        voiceOverrideRoomExitButton.voiceOverrideRoom.gameObject,
                        voiceOverrideRoomExitButton.voiceOverrideRoom
                    );
                    EditorGUIUtility.PingObject(voiceOverrideRoomExitButton.voiceOverrideRoom);
                }
            }
        }
    }
}