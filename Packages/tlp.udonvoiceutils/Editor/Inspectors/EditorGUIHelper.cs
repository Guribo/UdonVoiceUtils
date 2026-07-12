using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace TLP.UdonVoiceUtils.Editor.Inspectors
{
    /// <summary>
    /// Shared editor GUI helpers for OnSceneGUI click-to-select logic.
    /// </summary>
    internal static class EditorGUIHelper
    {
        /// <summary>
        /// Converts a mouse GUI event to a world-space position on the Y=0 plane
        /// and calls <paramref name="onMouseDown"/> on left-click.
        /// </summary>
        internal static void HandleInput(Event guiEvent, UdonSharpBehaviour destination,
                Action<Vector3, UdonSharpBehaviour> onMouseDown) {
            var mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            var mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                guiEvent.modifiers == EventModifiers.None) {
                onMouseDown(mousePosition, destination);
            }
        }

        /// <summary>
        /// Checks if a click position is near a target's screen position and selects it.
        /// </summary>
        internal static void TrySelectAtClick(Vector3 mousePosition, UdonSharpBehaviour target) {
            var targetGuiPosition =
                    HandleUtility.WorldToGUIPoint(target.transform.position);
            var mouseGuiPosition = HandleUtility.WorldToGUIPoint(mousePosition);
            bool clickCloseToTarget = Vector2.Distance(targetGuiPosition, mouseGuiPosition) < 10f;
            if (clickCloseToTarget) {
                Selection.SetActiveObjectWithContext(
                        target.gameObject,
                        target
                );
                EditorGUIUtility.PingObject(target);
            }
        }
    }
}
