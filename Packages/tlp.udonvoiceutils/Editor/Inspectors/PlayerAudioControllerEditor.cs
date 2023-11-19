#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using System.Collections.Generic;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    [CustomEditor(typeof(PlayerAudioController))]
    public class PlayerAudioControllerEditor : UnityEditor.Editor
    {
        private readonly HashSet<PlayerAudioOverride> _relevantBehaviours =
            new HashSet<PlayerAudioOverride>();

        private const int RefreshInterval = 60;
        private int _refreshCount;


        public void OnSceneGUI()
        {
            var playerAudioController = (PlayerAudioController)target;
            if (!Utilities.IsValid(playerAudioController))
            {
                return;
            }

            var guiEvent = Event.current;

            switch (guiEvent.type)
            {
                case EventType.Repaint:
                {
                    // draw lines to each connected element
                    _refreshCount = (_refreshCount + 1) % RefreshInterval;
                    if (_refreshCount == 0)
                    {
                        UpdateRelevantBehaviours(playerAudioController);
                    }

                    foreach (var playerAudioOverride in _relevantBehaviours)
                    {
                        // TODO refactor redundant code
                        if (!playerAudioOverride)
                        {
                            continue;
                        }

                        if (playerAudioOverride.PlayerAudioController != playerAudioController)
                        {
                            continue;
                        }

                        var doorPosition = playerAudioOverride.transform.position;

                        Handles.color = Color.white;
                        Handles.DrawDottedLine(playerAudioController.transform.position, doorPosition, 2);

                        GUI.color = Color.white;
                        Handles.Label(doorPosition, playerAudioOverride.gameObject.name);
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

        private void UpdateRelevantBehaviours(PlayerAudioController playerAudioController)
        {
            _relevantBehaviours.Clear();

            foreach (var udonBehaviour in Resources.FindObjectsOfTypeAll<UdonBehaviour>())
            {
                try
                {
                    foreach (var betterPlayerAudioOverride in udonBehaviour.gameObject
                                 .GetComponents<PlayerAudioOverride>())
                    {
                        if (betterPlayerAudioOverride.PlayerAudioController == playerAudioController)
                        {
                            _relevantBehaviours.Add(betterPlayerAudioOverride);
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void HandleInput(Event guiEvent, UdonSharpBehaviour destination)
        {
            var mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            var mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftMouseDown(mousePosition, destination);
            }
        }

        private void HandleLeftMouseDown(Vector3 mousePosition, UdonSharpBehaviour destination)
        {
            var roomGuiPosition =
                HandleUtility.WorldToGUIPoint(destination.transform.position);
            var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
            bool clickCloseToDestinationGameObject = Vector2.Distance(roomGuiPosition, mouseGuiPosition) < 10f;
            if (clickCloseToDestinationGameObject)
            {
                Selection.SetActiveObjectWithContext(
                    destination.gameObject,
                    destination
                );
                EditorGUIUtility.PingObject(destination);
            }
        }
    }
}
#endif