using TLP.UdonVoiceUtils.Runtime.Examples;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    [CustomEditor(typeof(VoiceOverrideDoor))]
    public class VoiceOverrideDoorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI() {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();
        }

        public void OnSceneGUI() {
            var voiceOverrideDoor = (VoiceOverrideDoor)target;
            if (!Utilities.IsValid(voiceOverrideDoor)) {
                return;
            }

            var doorTransform = voiceOverrideDoor.transform;
            var doorPosition = doorTransform.position;

            var guiEvent = Event.current;

            switch (guiEvent.type) {
                case EventType.Repaint:
                {
                    // draw
                    DrawDoorInfo(voiceOverrideDoor);

                    if (Utilities.IsValid(voiceOverrideDoor.voiceOverrideRoom)) {
                        var roomPosition = voiceOverrideDoor.voiceOverrideRoom.transform.position;

                        Handles.color = Color.white;
                        Handles.DrawDottedLine(doorPosition, roomPosition, 2);

                        GUI.color = Color.white;
                        Handles.Label(roomPosition, voiceOverrideDoor.voiceOverrideRoom.gameObject.name);
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
                    HandleInput(guiEvent, voiceOverrideDoor);
                }
                    break;
            }
        }

        public static void DrawDoorInfo(VoiceOverrideDoor voiceOverrideDoor) {
            if (!voiceOverrideDoor) {
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

        private void HandleInput(Event guiEvent, VoiceOverrideDoor voiceOverrideDoor) {
            var mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            var mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None) {
                HandleLeftMouseDown(mousePosition, voiceOverrideDoor);
            }
        }

        private void HandleLeftMouseDown(Vector3 mousePosition, VoiceOverrideDoor voiceOverrideDoor) {
            if (Utilities.IsValid(voiceOverrideDoor.voiceOverrideRoom)) {
                var roomGuiPosition =
                        HandleUtility.WorldToGUIPoint(voiceOverrideDoor.voiceOverrideRoom.transform.position);
                var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
                bool clickCloseToRoomGameObject = Vector2.Distance(roomGuiPosition, mouseGuiPosition) < 10f;
                if (clickCloseToRoomGameObject) {
                    Selection.SetActiveObjectWithContext(
                            voiceOverrideDoor.voiceOverrideRoom.gameObject,
                            voiceOverrideDoor.voiceOverrideRoom
                    );
                    EditorGUIUtility.PingObject(voiceOverrideDoor.voiceOverrideRoom);
                }
            }
        }
    }
}