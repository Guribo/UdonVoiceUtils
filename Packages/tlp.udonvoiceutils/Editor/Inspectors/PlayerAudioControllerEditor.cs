#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    [CustomEditor(typeof(PlayerAudioController))]
    public class PlayerAudioControllerEditor : UnityEditor.Editor
    {
        private readonly HashSet<PlayerAudioOverride> _relevantBehaviours =
                new HashSet<PlayerAudioOverride>();

        public override void OnInspectorGUI() {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();
        }

        public void OnSceneGUI() {
            var playerAudioController = (PlayerAudioController)target;
            if (!Utilities.IsValid(playerAudioController)) {
                Debug.LogWarning($"{nameof(playerAudioController)} invalid");
                return;
            }

            var guiEvent = Event.current;

            switch (guiEvent.type) {
                case EventType.Repaint:
                {
                    // draw lines to each connected element

                    UpdateRelevantBehaviours();

                    foreach (var playerAudioOverride in _relevantBehaviours) {
                        // TODO refactor redundant code
                        if (!playerAudioOverride) {
                            continue;
                        }
                        var doorPosition = playerAudioOverride.transform.position;

                        Handles.color = Color.white;
                        Handles.DrawDottedLine(playerAudioController.transform.position, doorPosition, 2);

                        GUI.color = Color.white;
                        Handles.Label(doorPosition, playerAudioOverride.transform.GetPathInScene());
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
                        HandleInput(guiEvent, udonSharpBehaviour);
                    }
                }
                    break;
            }
        }

        private void UpdateRelevantBehaviours() {
            _relevantBehaviours.Clear();

            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            Debug.Log($"Roots: {rootGameObjects.Count()}");

            foreach (var rootGameObject in rootGameObjects) {
                var udonBehaviours = rootGameObject.GetComponentsInChildren<UdonBehaviour>();
                Debug.Log($"{rootGameObject.transform.GetPathInScene()}: {udonBehaviours.Count()}");

                foreach (var udonBehaviour in udonBehaviours) {
                    try {
                        var udonSharpBehaviour = UdonSharpEditorUtility.GetProxyBehaviour(udonBehaviour);

                        if (!udonSharpBehaviour) {
                            Debug.LogWarning(
                                    $"{udonBehaviour.GetComponentPathInScene()} has no backing {nameof(UdonSharpBehaviour)}");
                            continue;
                        }

                        var playerAudioOverride = (PlayerAudioOverride)udonSharpBehaviour;
                        if (!playerAudioOverride) {
                            Debug.LogWarning(
                                    $"{udonSharpBehaviour.GetScriptPathInScene()} is no {nameof(PlayerAudioOverride)}");
                            continue;
                        }

                        _relevantBehaviours.Add(playerAudioOverride);
                    }
                    catch (Exception) {
                        // ignored
                    }
                }
            }

            Debug.Log($"{nameof(_relevantBehaviours)}: {_relevantBehaviours.Count}");
        }

        private void HandleInput(Event guiEvent, UdonSharpBehaviour destination) {
            var mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            var mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None) {
                HandleLeftMouseDown(mousePosition, destination);
            }
        }

        private void HandleLeftMouseDown(Vector3 mousePosition, UdonSharpBehaviour destination) {
            var roomGuiPosition =
                    HandleUtility.WorldToGUIPoint(destination.transform.position);
            var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
            bool clickCloseToDestinationGameObject = Vector2.Distance(roomGuiPosition, mouseGuiPosition) < 10f;
            if (clickCloseToDestinationGameObject) {
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