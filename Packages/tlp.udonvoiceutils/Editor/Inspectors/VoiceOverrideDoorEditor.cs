using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    [CustomEditor(typeof(VoiceOverrideDoor))]
    public class VoiceOverrideDoorEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Example implementation of a door which adds a player to " +
                   "a PlayerAudioOverride upon entering a room. " +
                   "This works by checking player-trigger interactions with the doors box collider (trigger) " +
                   "when the player walks through the door.";
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
                    _DrawDoorInfo(voiceOverrideDoor);

                    if (Utilities.IsValid(voiceOverrideDoor.VoiceOverrideRoom)) {
                        var roomPosition = voiceOverrideDoor.VoiceOverrideRoom.transform.position;

                        Handles.color = Color.white;
                        Handles.DrawDottedLine(doorPosition, roomPosition, 2);

                        GUI.color = Color.white;
                        Handles.Label(roomPosition, voiceOverrideDoor.VoiceOverrideRoom.gameObject.name);
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
                    EditorGUIHelper._HandleInput(guiEvent, voiceOverrideDoor, _SelectRoom);
                }
                    break;
            }
        }

        public static void _DrawDoorInfo(VoiceOverrideDoor voiceOverrideDoor) {
            if (!voiceOverrideDoor) {
                return;
            }

            var doorTransform = voiceOverrideDoor.transform;
            var doorPosition = doorTransform.position;
            var exitDirection = doorTransform.TransformDirection(voiceOverrideDoor.ExitDirection).normalized;


            Handles.color = Color.red;
            Handles.DrawDottedLine(doorPosition, doorPosition + exitDirection, 2);

            GUI.color = Color.red;
            Handles.Label(doorPosition + exitDirection, "Outside");

            Handles.color = Color.green;
            Handles.DrawDottedLine(doorPosition, doorPosition - exitDirection, 2);

            GUI.color = Color.green;
            Handles.Label(doorPosition - exitDirection, "Inside");
        }

        private static void _SelectRoom(Vector3 mousePosition, UdonSharpBehaviour destination) {
            var voiceOverrideDoor = (VoiceOverrideDoor)destination;
            if (Utilities.IsValid(voiceOverrideDoor.VoiceOverrideRoom)) {
                EditorGUIHelper._TrySelectAtClick(mousePosition, voiceOverrideDoor.VoiceOverrideRoom);
            }
        }
    }
}