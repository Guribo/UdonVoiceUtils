using System.Collections.Generic;
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    [CustomEditor(typeof(VoiceOverrideRoom))]
    public class VoiceOverrideRoomEditor : TlpBehaviourEditor
    {
        private readonly HashSet<UdonSharpBehaviour> _relevantBehaviours = new HashSet<UdonSharpBehaviour>();

        private const int RefreshInterval = 60;
        private int _refreshCount;

        protected override string GetDescription() {
            return "Example script using a PlayerAudioOverride to define a space/zone in which every player " +
                   "has specific audio/voice properties. " +
                   "Players must be added explicitly through other scripts (e.g. VoiceOverrideDoor). " +
                   "All added players are automatically synchronized over the network.\n\n" +
                   "Note: please make sure that if the room is a physical space, that players can only enter/exit " +
                   "through means (e.g. VoiceOverrideDoor) that add/remove players from the VoiceOverrideRoom!\n\n" +
                   "Use cases: rooms, vehicles (cockpits), " +
                   "spaces with complex geometry (not suited for trigger based zones).\n\n" +
                   "Pitfall: players being carried by other players (station on avatar) will not be added to a room " +
                   "when the carrying player uses e.g. the example VoiceOverrideRoomEnter-/ExitButton scripts.";
        }

        public void OnSceneGUI() {
            var voiceOverrideRoom = (VoiceOverrideRoom)target;
            if (!Utilities.IsValid(voiceOverrideRoom)) {
                return;
            }

            var guiEvent = Event.current;


            switch (guiEvent.type) {
                case EventType.Repaint:
                {
                    // draw lines to each connected element
                    _refreshCount = (_refreshCount + 1) % RefreshInterval;
                    if (_refreshCount == 0) {
                        UpdateRelevantBehaviours(voiceOverrideRoom);
                    }

                    foreach (var udonSharpBehavior in _relevantBehaviours) {
                        // TODO refactor redundant code
                        var voiceOverrideDoor = udonSharpBehavior as VoiceOverrideDoor;
                        if (voiceOverrideDoor != null) {
                            if (voiceOverrideDoor.VoiceOverrideRoom != voiceOverrideRoom) {
                                continue;
                            }

                            VoiceOverrideDoorEditor.DrawDoorInfo(voiceOverrideDoor);

                            if (Utilities.IsValid(voiceOverrideDoor)) {
                                var doorPosition = voiceOverrideDoor.transform.position;

                                Handles.color = Color.white;
                                Handles.DrawDottedLine(voiceOverrideRoom.transform.position, doorPosition, 2);

                                GUI.color = Color.white;
                                Handles.Label(doorPosition, voiceOverrideDoor.gameObject.name);
                            }

                            continue;
                        }

                        var voiceOverrideRoomEnterButton = udonSharpBehavior as VoiceOverrideRoomEnterButton;
                        if (voiceOverrideRoomEnterButton != null) {
                            if (voiceOverrideRoomEnterButton.VoiceOverrideRoom != voiceOverrideRoom) {
                                continue;
                            }

                            if (Utilities.IsValid(voiceOverrideRoomEnterButton)) {
                                var doorPosition = voiceOverrideRoomEnterButton.transform.position;

                                Handles.color = Color.white;
                                Handles.DrawDottedLine(voiceOverrideRoom.transform.position, doorPosition, 2);

                                GUI.color = Color.white;
                                Handles.Label(doorPosition, voiceOverrideRoomEnterButton.gameObject.name);
                            }

                            continue;
                        }

                        var voiceOverrideRoomExitButton = udonSharpBehavior as VoiceOverrideRoomExitButton;
                        if (voiceOverrideRoomExitButton != null) {
                            if (voiceOverrideRoomExitButton.VoiceOverrideRoom != voiceOverrideRoom) {
                                continue;
                            }

                            if (Utilities.IsValid(voiceOverrideRoomExitButton)) {
                                var doorPosition = voiceOverrideRoomExitButton.transform.position;

                                Handles.color = Color.white;
                                Handles.DrawDottedLine(voiceOverrideRoom.transform.position, doorPosition, 2);

                                GUI.color = Color.white;
                                Handles.Label(doorPosition, voiceOverrideRoomExitButton.gameObject.name);
                            }

                            continue;
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
                    foreach (var udonSharpBehaviour in _relevantBehaviours) {
                        EditorGUIHelper.HandleInput(guiEvent, udonSharpBehaviour,
                                EditorGUIHelper.TrySelectAtClick);
                    }
                }
                    break;
            }
        }

        private void UpdateRelevantBehaviours(VoiceOverrideRoom voiceOverrideRoom) {
            _relevantBehaviours.Clear();


            foreach (var udonBehaviour in Resources.FindObjectsOfTypeAll<UdonBehaviour>()) {
                foreach (var voiceOverrideDoor in udonBehaviour.gameObject.GetComponents<VoiceOverrideDoor>()) {
                    if (voiceOverrideDoor.VoiceOverrideRoom == voiceOverrideRoom) {
                        _relevantBehaviours.Add(voiceOverrideDoor);
                    }
                }

                foreach (var exitButton in udonBehaviour.gameObject
                                 .GetComponents<VoiceOverrideRoomExitButton>()) {
                    if (exitButton.VoiceOverrideRoom == voiceOverrideRoom) {
                        _relevantBehaviours.Add(exitButton);
                    }
                }

                foreach (var enterButton in udonBehaviour.gameObject
                                 .GetComponents<VoiceOverrideRoomEnterButton>()) {
                    if (enterButton.VoiceOverrideRoom == voiceOverrideRoom) {
                        _relevantBehaviours.Add(enterButton);
                    }
                }
            }
        }
    }
}