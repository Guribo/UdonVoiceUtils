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

        protected override string _GetDescription() {
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
                    _DrawConnectedElements(voiceOverrideRoom);
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
                        EditorGUIHelper._HandleInput(guiEvent, udonSharpBehaviour,
                                EditorGUIHelper._TrySelectAtClick);
                    }
                }
                    break;
            }
        }

        private void _DrawConnectedElements(VoiceOverrideRoom voiceOverrideRoom) {
            // draw lines to each connected element
            _refreshCount = (_refreshCount + 1) % RefreshInterval;
            if (_refreshCount == 0) {
                _UpdateRelevantBehaviours(voiceOverrideRoom);
            }

            foreach (var udonSharpBehavior in _relevantBehaviours) {
                _DrawConnectionsForVoiceOverrideElements(voiceOverrideRoom, udonSharpBehavior);
            }
        }

        private static void _DrawConnectionsForVoiceOverrideElements(
                VoiceOverrideRoom voiceOverrideRoom,
                UdonSharpBehaviour udonSharpBehavior
        ) {
            // TODO refactor redundant code
            var voiceOverrideDoor = udonSharpBehavior as VoiceOverrideDoor;
            if (_DrawVoiceOverrideDoorConnections(voiceOverrideRoom, voiceOverrideDoor)) {
                return;
            }

            var voiceOverrideRoomEnterButton = udonSharpBehavior as VoiceOverrideRoomEnterButton;
            if (_DrawConnectionForEnterButton(voiceOverrideRoom, voiceOverrideRoomEnterButton)) {
                return;
            }

            var voiceOverrideRoomExitButton = udonSharpBehavior as VoiceOverrideRoomExitButton;
            if (voiceOverrideRoomExitButton == null) {
                return;
            }

            if (voiceOverrideRoomExitButton.VoiceOverrideRoom != voiceOverrideRoom) {
                return;
            }

            if (!Utilities.IsValid(voiceOverrideRoomExitButton)) {
                return;
            }

            _DrawConnectionToExitButton(voiceOverrideRoom, voiceOverrideRoomExitButton);
        }

        private static bool _DrawConnectionForEnterButton(
                VoiceOverrideRoom voiceOverrideRoom,
                VoiceOverrideRoomEnterButton voiceOverrideRoomEnterButton
        ) {
            if (voiceOverrideRoomEnterButton != null) {
                if (voiceOverrideRoomEnterButton.VoiceOverrideRoom != voiceOverrideRoom) {
                    return true;
                }

                if (Utilities.IsValid(voiceOverrideRoomEnterButton)) {
                    _DrawConnectionToEnterButton(voiceOverrideRoom, voiceOverrideRoomEnterButton);
                }

                return true;
            }

            return false;
        }

        private static bool _DrawVoiceOverrideDoorConnections(
                VoiceOverrideRoom voiceOverrideRoom,
                VoiceOverrideDoor voiceOverrideDoor
        ) {
            if (voiceOverrideDoor != null) {
                if (voiceOverrideDoor.VoiceOverrideRoom != voiceOverrideRoom) {
                    return true;
                }

                _DrawVoiceOverrideDoorInfo(voiceOverrideRoom, voiceOverrideDoor);

                return true;
            }

            return false;
        }

        private static void _DrawConnectionToExitButton(
                VoiceOverrideRoom voiceOverrideRoom,
                VoiceOverrideRoomExitButton voiceOverrideRoomExitButton
        ) {
            var doorPosition = voiceOverrideRoomExitButton.transform.position;

            Handles.color = Color.white;
            Handles.DrawDottedLine(voiceOverrideRoom.transform.position, doorPosition, 2);

            GUI.color = Color.white;
            Handles.Label(doorPosition, voiceOverrideRoomExitButton.gameObject.name);
        }

        private static void _DrawVoiceOverrideDoorInfo(
                VoiceOverrideRoom voiceOverrideRoom,
                VoiceOverrideDoor voiceOverrideDoor
        ) {
            VoiceOverrideDoorEditor._DrawDoorInfo(voiceOverrideDoor);

            if (Utilities.IsValid(voiceOverrideDoor)) {
                _DrawConnectionToDoor(voiceOverrideRoom, voiceOverrideDoor);
            }
        }

        private static void _DrawConnectionToEnterButton(
                VoiceOverrideRoom voiceOverrideRoom,
                VoiceOverrideRoomEnterButton voiceOverrideRoomEnterButton
        ) {
            var doorPosition = voiceOverrideRoomEnterButton.transform.position;

            Handles.color = Color.white;
            Handles.DrawDottedLine(voiceOverrideRoom.transform.position, doorPosition, 2);

            GUI.color = Color.white;
            Handles.Label(doorPosition, voiceOverrideRoomEnterButton.gameObject.name);
        }

        private static void _DrawConnectionToDoor(VoiceOverrideRoom voiceOverrideRoom, VoiceOverrideDoor voiceOverrideDoor) {
            var doorPosition = voiceOverrideDoor.transform.position;

            Handles.color = Color.white;
            Handles.DrawDottedLine(voiceOverrideRoom.transform.position, doorPosition, 2);

            GUI.color = Color.white;
            Handles.Label(doorPosition, voiceOverrideDoor.gameObject.name);
        }

        private void _UpdateRelevantBehaviours(VoiceOverrideRoom voiceOverrideRoom) {
            _relevantBehaviours.Clear();

            foreach (var udonBehaviour in Resources.FindObjectsOfTypeAll<UdonBehaviour>()) {
                _AddRelevantVoiceOverrideDoor(voiceOverrideRoom, udonBehaviour);

                _AddRelevantExitButton(voiceOverrideRoom, udonBehaviour);

                _AddRelevantEnterButton(voiceOverrideRoom, udonBehaviour);
            }
        }

        private void _AddRelevantEnterButton(VoiceOverrideRoom voiceOverrideRoom, UdonBehaviour udonBehaviour) {
            foreach (var enterButton in udonBehaviour.gameObject
                             .GetComponents<VoiceOverrideRoomEnterButton>()) {
                if (enterButton.VoiceOverrideRoom == voiceOverrideRoom) {
                    _relevantBehaviours.Add(enterButton);
                }
            }
        }

        private void _AddRelevantExitButton(VoiceOverrideRoom voiceOverrideRoom, UdonBehaviour udonBehaviour) {
            foreach (var exitButton in udonBehaviour.gameObject
                             .GetComponents<VoiceOverrideRoomExitButton>()) {
                if (exitButton.VoiceOverrideRoom == voiceOverrideRoom) {
                    _relevantBehaviours.Add(exitButton);
                }
            }
        }

        private void _AddRelevantVoiceOverrideDoor(VoiceOverrideRoom voiceOverrideRoom, UdonBehaviour udonBehaviour) {
            foreach (var voiceOverrideDoor in udonBehaviour.gameObject.GetComponents<VoiceOverrideDoor>()) {
                if (voiceOverrideDoor.VoiceOverrideRoom == voiceOverrideRoom) {
                    _relevantBehaviours.Add(voiceOverrideDoor);
                }
            }
        }
    }
}