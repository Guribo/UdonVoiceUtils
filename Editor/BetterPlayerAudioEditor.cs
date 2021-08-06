#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using System.Collections.Generic;
using Guribo.UdonBetterAudio.Runtime;
using Guribo.UdonBetterAudio.Runtime.Examples;
using Guribo.UdonUtils.Editor;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Editor
{
    [CustomEditor(typeof(BetterPlayerAudio))]
    public class BetterPlayerAudioEditor : UdonLibraryEditor
    {
        protected override string GetSymbolName()
        {
            return "betterPlayerAudio";
        }

        private readonly HashSet<BetterPlayerAudioOverride> _relevantBehaviours =
            new HashSet<BetterPlayerAudioOverride>();

        private int refreshInterval = 60;
        private int _refreshCount;


        public void OnSceneGUI()
        {
            var betterPlayerAudio = (BetterPlayerAudio) target;
            if (!Utilities.IsValid(betterPlayerAudio))
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
                        UpdateRelevantBehaviours(betterPlayerAudio);
                    }

                    foreach (var playerAudioOverride in _relevantBehaviours)
                    {
                        // TODO refactor redundant code
                        if (playerAudioOverride)
                        {
                            if (playerAudioOverride.betterPlayerAudio != betterPlayerAudio)
                            {
                                continue;
                            }

                            var doorPosition = playerAudioOverride.transform.position;

                            Handles.color = Color.white;
                            Handles.DrawDottedLine(betterPlayerAudio.transform.position, doorPosition, 2);

                            GUI.color = Color.white;
                            Handles.Label(doorPosition, playerAudioOverride.gameObject.name);
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

        private void UpdateRelevantBehaviours(BetterPlayerAudio betterPlayerAudio)
        {
            _relevantBehaviours.Clear();

            foreach (var udonBehaviour in Resources.FindObjectsOfTypeAll<UdonBehaviour>())
            {
                try
                {
                    foreach (var betterPlayerAudioOverride in udonBehaviour.gameObject.GetUdonSharpComponents<BetterPlayerAudioOverride>())
                    {
                        if (betterPlayerAudioOverride.betterPlayerAudio == betterPlayerAudio)
                        {
                            _relevantBehaviours.Add(betterPlayerAudioOverride);
                        }
                    }
                }
                catch (Exception e)
                {
                    // ignored
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
#endif