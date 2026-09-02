using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    [CustomEditor(typeof(VoiceOverrideRoomEnterButton))]
    public class VoiceOverrideRoomEnterButtonEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
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
                    EditorGUIHelper._HandleInput(guiEvent, voiceOverrideRoomEnterButton, _SelectRoom);
                }
                    break;
            }
        }

        private static void _SelectRoom(Vector3 mousePosition, UdonSharpBehaviour destination) {
            var enterButton = (VoiceOverrideRoomEnterButton)destination;
            if (Utilities.IsValid(enterButton.VoiceOverrideRoom)) {
                EditorGUIHelper._TrySelectAtClick(mousePosition, enterButton.VoiceOverrideRoom);
            }
        }
    }
}