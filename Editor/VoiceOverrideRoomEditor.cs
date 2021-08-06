using System.Collections.Generic;
using Guribo.UdonBetterAudio.Runtime.Examples;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Editor
{
    [CustomEditor(typeof(VoiceOverrideRoom))]
    public class VoiceOverrideRoomEditor : UnityEditor.Editor
    {
        private readonly HashSet<UdonSharpBehaviour> _relevantBehaviours = new HashSet<UdonSharpBehaviour>();

        private int refreshInterval = 60;
        private int _refreshCount;

        public override void OnInspectorGUI()
        {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();
        }

        public void OnSceneGUI()
        {
            var voiceOverrideRoom = (VoiceOverrideRoom) target;
            if (!Utilities.IsValid(voiceOverrideRoom))
            {
                return;
            }

            Event guiEvent = Event.current;


            switch (guiEvent.type)
            {
                case EventType.Repaint:
                {
                    // draw lines to each connected element
                    _refreshCount = (_refreshCount + 1) % refreshInterval;
                    if (_refreshCount == 0)
                    {
                        UpdateRelevantBehaviours(voiceOverrideRoom);
                    }

                    foreach (var udonSharpBehavior in _relevantBehaviours)
                    {
                        // TODO refactor redundant code
                        var voiceOverrideDoor = udonSharpBehavior as VoiceOverrideDoor;
                        if (voiceOverrideDoor != null)
                        {
                            if (voiceOverrideDoor.voiceOverrideRoom != voiceOverrideRoom)
                            {
                                continue;
                            }

                            VoiceOverrideDoorEditor.DrawDoorInfo(voiceOverrideDoor);

                            if (Utilities.IsValid(voiceOverrideDoor))
                            {
                                var doorPosition = voiceOverrideDoor.transform.position;

                                Handles.color = Color.white;
                                Handles.DrawDottedLine(voiceOverrideRoom.transform.position, doorPosition, 2);

                                GUI.color = Color.white;
                                Handles.Label(doorPosition, voiceOverrideDoor.gameObject.name);
                            }
                            continue;
                        }

                        var voiceOverrideRoomEnterButton = udonSharpBehavior as VoiceOverrideRoomEnterButton;
                        if (voiceOverrideRoomEnterButton != null)
                        {
                            if (voiceOverrideRoomEnterButton.voiceOverrideRoom != voiceOverrideRoom)
                            {
                                continue;
                            }

                            if (Utilities.IsValid(voiceOverrideRoomEnterButton))
                            {
                                var doorPosition = voiceOverrideRoomEnterButton.transform.position;

                                Handles.color = Color.white;
                                Handles.DrawDottedLine(voiceOverrideRoom.transform.position, doorPosition, 2);

                                GUI.color = Color.white;
                                Handles.Label(doorPosition, voiceOverrideRoomEnterButton.gameObject.name);
                            }
                            continue;
                        }

                        var voiceOverrideRoomExitButton = udonSharpBehavior as VoiceOverrideRoomExitButton;
                        if (voiceOverrideRoomExitButton != null)
                        {
                            if (voiceOverrideRoomExitButton.voiceOverrideRoom != voiceOverrideRoom)
                            {
                                continue;
                            }

                            if (Utilities.IsValid(voiceOverrideRoomExitButton))
                            {
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
                    foreach (var udonSharpBehaviour in _relevantBehaviours)
                    {
                        HandleInput(guiEvent, udonSharpBehaviour);
                    }
                }
                    break;
            }
        }

        private void UpdateRelevantBehaviours(VoiceOverrideRoom voiceOverrideRoom)
        {
            _relevantBehaviours.Clear();
            
            
            foreach (var udonBehaviour in Resources.FindObjectsOfTypeAll<UdonBehaviour>())
            {
                foreach (var betterPlayerAudioOverride in udonBehaviour.gameObject.GetUdonSharpComponents<VoiceOverrideDoor>())
                {
                    if (betterPlayerAudioOverride.voiceOverrideRoom == voiceOverrideRoom)
                    {
                        _relevantBehaviours.Add(betterPlayerAudioOverride);
                    }
                }
                
                foreach (var betterPlayerAudioOverride in udonBehaviour.gameObject.GetUdonSharpComponents<VoiceOverrideRoomExitButton>())
                {
                    if (betterPlayerAudioOverride.voiceOverrideRoom == voiceOverrideRoom)
                    {
                        _relevantBehaviours.Add(betterPlayerAudioOverride);
                    }
                }
                
                foreach (var betterPlayerAudioOverride in udonBehaviour.gameObject.GetUdonSharpComponents<VoiceOverrideRoomEnterButton>())
                {
                    if (betterPlayerAudioOverride.voiceOverrideRoom == voiceOverrideRoom)
                    {
                        _relevantBehaviours.Add(betterPlayerAudioOverride);
                    }
                }
            }
        }

        void HandleInput(Event guiEvent, UdonSharpBehaviour destination)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            Vector3 mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftMouseDown(mousePosition, destination);
            }
        }

        void HandleLeftMouseDown(Vector3 mousePosition, UdonSharpBehaviour destination)
        {
            var roomGuiPosition =
                HandleUtility.WorldToGUIPoint(destination.transform.position);
            var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
            var clickCloseToDestinationGameObject = Vector2.Distance(roomGuiPosition, mouseGuiPosition) < 10f;
            if (clickCloseToDestinationGameObject)
            {
                Selection.SetActiveObjectWithContext(destination.gameObject,
                    destination);
                EditorGUIUtility.PingObject(destination);
            }
        }
    }
}