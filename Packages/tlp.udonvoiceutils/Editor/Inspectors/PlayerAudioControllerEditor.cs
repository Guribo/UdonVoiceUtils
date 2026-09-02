#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using System.Collections.Generic;
using TLP.UdonUtils.Editor.Core;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonVoiceUtils.Runtime.Core.PlayerAudio;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    [CustomEditor(typeof(PlayerAudioController))]
    public class PlayerAudioControllerEditor : TlpBehaviourEditor
    {
        private readonly HashSet<PlayerAudioOverride> _relevantBehaviours =
                new HashSet<PlayerAudioOverride>();

        protected override string _GetDescription() {
            return "Main controller for any player audio override. Only one allowed per scene/world. " +
                   "Can be disabled to turn of all effects (resets every player to configured default). " +
                   "Can provide global effects (configurable via PlayerAudioConfigurationModels). " +
                   "Can be used to tweak performance and quality of audio effects.";
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

                    _UpdateRelevantBehaviours();
                    _DrawAudioOverrideLine(playerAudioController);
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

        private void _DrawAudioOverrideLine(PlayerAudioController playerAudioController) {
            foreach (var playerAudioOverride in _relevantBehaviours) {
                if (!playerAudioOverride) {
                    continue;
                }
                var overridePosition = playerAudioOverride.transform.position;

                Handles.color = Color.white;
                Handles.DrawDottedLine(playerAudioController.transform.position, overridePosition, 2);

                GUI.color = Color.white;
                Handles.Label(overridePosition, playerAudioOverride.transform._GetPathInScene());
            }
        }

        private void _UpdateRelevantBehaviours() {
            _relevantBehaviours.Clear();

            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
#if TLP_DEBUG
            Debug.Log($"Roots: {rootGameObjects.Length}");
#endif

            foreach (var rootGameObject in rootGameObjects) {
                var udonBehaviours = rootGameObject.GetComponentsInChildren<UdonBehaviour>();
#if TLP_DEBUG
                Debug.Log($"{rootGameObject.transform._GetPathInScene()}: {udonBehaviours.Length}");
#endif
                _ExtractPlayerAudioOverrides(udonBehaviours);
            }
#if TLP_DEBUG
            Debug.Log($"{nameof(_relevantBehaviours)}: {_relevantBehaviours.Count}");
#endif
        }

        private void _ExtractPlayerAudioOverrides(UdonBehaviour[] udonBehaviours) {
            foreach (var udonBehaviour in udonBehaviours) {
                try {
                    var udonSharpBehaviour = UdonSharpEditorUtility.GetProxyBehaviour(udonBehaviour);

                    if (!udonSharpBehaviour) {
                        continue;
                    }

                    var playerAudioOverride = udonSharpBehaviour as PlayerAudioOverride;
                    if (!playerAudioOverride) {
                        continue;
                    }

                    _relevantBehaviours.Add(playerAudioOverride);
                }
                catch (Exception) {
                    // ignored
                }
            }
        }
    }
}
#endif