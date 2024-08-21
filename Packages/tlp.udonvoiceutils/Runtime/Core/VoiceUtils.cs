using TLP.UdonUtils.Runtime.Common;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core
{
    public static class VoiceUtils
    {
        /// <summary>
        /// Searches the scene for the GameObject 'TLP_PlayerAudioController' and its PlayerAudioController
        /// </summary>
        /// <returns>The first found PlayerAudioController in the scene or null if not found</returns>
        public static PlayerAudioController FindPlayerAudioController() {
            var found = GameObject.Find(PlayerAudioController.ExpectedGameObjectName());
            if (!Utilities.IsValid(found)) {
                Debug.LogError($"GameObject '{PlayerAudioController.ExpectedGameObjectName()}' does not exist in the scene");
                return null;
            }

            var controller = found.GetComponent<PlayerAudioController>();
            if (Utilities.IsValid(controller)) {
                return controller;
            }

            Debug.LogError(
                    $"GameObject '{found.transform.GetPathInScene()}' has no valid {nameof(PlayerAudioController)} component");
            return null;
        }
    }
}