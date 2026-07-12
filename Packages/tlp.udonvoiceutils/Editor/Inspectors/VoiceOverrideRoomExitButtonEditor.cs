using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    [CustomEditor(typeof(VoiceOverrideRoomExitButton))]
    public class VoiceOverrideRoomExitButtonEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Example implementation of a door which removes a player from " +
                   "a VoiceOverrideRoom when interacting with the button.\n\n" +
                   "Pitfall: If the entering player carries another player, " +
                   "then that player will not be removed from the VoiceOverrideRoom!";
        }

        public void OnSceneGUI() {
            var voiceOverrideRoomExitButton = (VoiceOverrideRoomExitButton)target;
            if (!Utilities.IsValid(voiceOverrideRoomExitButton)) {
                return;
            }

            var doorTransform = voiceOverrideRoomExitButton.transform;
            var doorPosition = doorTransform.position;

            var guiEvent = Event.current;

            switch (guiEvent.type) {
                case EventType.Repaint:
                {
                    if (Utilities.IsValid(voiceOverrideRoomExitButton.VoiceOverrideRoom)) {
                        var roomPosition = voiceOverrideRoomExitButton.VoiceOverrideRoom.transform.position;

                        Handles.color = Color.white;
                        Handles.DrawDottedLine(doorPosition, roomPosition, 2);

                        GUI.color = Color.white;
                        Handles.Label(roomPosition, voiceOverrideRoomExitButton.VoiceOverrideRoom.gameObject.name);
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
                    EditorGUIHelper.HandleInput(guiEvent, voiceOverrideRoomExitButton, SelectRoom);
                }
                    break;
            }
        }

        private static void SelectRoom(Vector3 mousePosition, UdonSharpBehaviour destination) {
            var exitButton = (VoiceOverrideRoomExitButton)destination;
            if (Utilities.IsValid(exitButton.VoiceOverrideRoom)) {
                EditorGUIHelper.TrySelectAtClick(mousePosition, exitButton.VoiceOverrideRoom);
            }
        }
    }
}