using TLP.UdonVoiceUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    [CustomEditor(typeof(VoiceOverrideRoomEnterButton))]
    public class VoiceOverrideEnterButtonEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Example implementation of a door which adds a player to " +
                   "a VoiceOverrideRoom when interacting with the button.\n\n" +
                   "Pitfall: If the entering player carries another player, " +
                   "then that player will not be added to the VoiceOverrideRoom!";
        }

        public void OnSceneGUI() {
            var voiceOverrideRoomEnterButton = (VoiceOverrideRoomEnterButton)target;
            if (!Utilities.IsValid(voiceOverrideRoomEnterButton)) {
                return;
            }

            var doorTransform = voiceOverrideRoomEnterButton.transform;
            var doorPosition = doorTransform.position;

            var guiEvent = Event.current;

            switch (guiEvent.type) {
                case EventType.Repaint:
                {
                    if (Utilities.IsValid(voiceOverrideRoomEnterButton.VoiceOverrideRoom)) {
                        var roomPosition = voiceOverrideRoomEnterButton.VoiceOverrideRoom.transform.position;

                        Handles.color = Color.white;
                        Handles.DrawDottedLine(doorPosition, roomPosition, 2);

                        GUI.color = Color.white;
                        Handles.Label(roomPosition, voiceOverrideRoomEnterButton.VoiceOverrideRoom.gameObject.name);
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


        private void HandleInput(Event guiEvent, VoiceOverrideRoomEnterButton voiceOverrideRoomEnterButton) {
            var mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            var mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None) {
                HandleLeftMouseDown(mousePosition, voiceOverrideRoomEnterButton);
            }
        }

        private void HandleLeftMouseDown(
                Vector3 mousePosition,
                VoiceOverrideRoomEnterButton voiceOverrideRoomEnterButton
        ) {
            if (Utilities.IsValid(voiceOverrideRoomEnterButton.VoiceOverrideRoom)) {
                var roomGuiPosition =
                        HandleUtility.WorldToGUIPoint(
                                voiceOverrideRoomEnterButton.VoiceOverrideRoom.transform.position);
                var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
                bool clickCloseToRoomGameObject = Vector2.Distance(roomGuiPosition, mouseGuiPosition) < 10f;
                if (clickCloseToRoomGameObject) {
                    Selection.SetActiveObjectWithContext(
                            voiceOverrideRoomEnterButton.VoiceOverrideRoom.gameObject,
                            voiceOverrideRoomEnterButton.VoiceOverrideRoom
                    );
                    EditorGUIUtility.PingObject(voiceOverrideRoomEnterButton.VoiceOverrideRoom);
                }
            }
        }
    }
}