using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace Guribo.UdonBetterAudio.Scripts
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class OwnershipTransfer : UdonSharpBehaviour
    {
        /// <summary>
        /// Changes the ownership of the entire hierarchy of the gameobject provided, including all relevant parents
        /// and their children
        /// </summary>
        /// <param name="go"></param>
        /// <param name="newOwner"></param>
        /// <returns></returns>
        public bool TransferOwnership(GameObject go, VRCPlayerApi newOwner)
        {
            if (!Utilities.IsValid(go))
            {
                Debug.LogError("OwnershipTransfer.SetOwner: invalid GameObject");
                return false;
            }
            
            if (!Utilities.IsValid(newOwner))
            {
                Debug.LogError("OwnershipTransfer.SetOwner: invalid new owner");
                return false;
            }

            // find the top most udon behaviour
            var topBehaviour = FindTopComponent(typeof(UdonBehaviour), gameObject.transform);
            if (!Utilities.IsValid(topBehaviour))
            {
                Debug.LogError($"OwnershipTransfer.SetOwner: GameObject {gameObject.name} " +
                                 $"has no parent udon behaviour which could change ownership");
                return false;
            }

            var allTransforms = GetComponentsInChildren<Transform>( true);

            if (allTransforms == null || allTransforms.Length == 0)
            {
                Debug.LogError($"OwnershipTransfer.SetOwner: GameObject {gameObject.name} " +
                               $"has no udon behaviours it its hierarchy");
                return false;
            }

            foreach (var childTransform in allTransforms)
            {
                if (!Utilities.IsValid(childTransform))
                {
                    Debug.LogWarning("OwnershipTransfer.SetOwner: invalid transform found. Skipping.");
                    continue;
                }

                var oldOwnerId = -1;
                var oldOwner = Networking.GetOwner(childTransform.gameObject);
                if (Utilities.IsValid(oldOwner))
                {
                    oldOwnerId = oldOwner.playerId;
                }

                Debug.Log($"OwnershipTransfer.SetOwner: setting owner of " +
                          $"'{childTransform.gameObject.name}' " +
                          $"from player {oldOwnerId} to player {newOwner.playerId}");
                
                Networking.SetOwner(newOwner, childTransform.gameObject);
            }

            return true;
        }

        /// <summary>
        /// Finds the highest component of a give type in the current gameobject hierarchy in parents
        /// </summary>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public Component FindTopComponent(Type type, Transform start)
        {
            if (!Utilities.IsValid(start))
            {
                return null;
            }
            
            Component topComponent = null;
            var topTransform = start;

            do
            {
                var behaviour = topTransform.GetComponent(type);
                if (Utilities.IsValid(behaviour))
                {
                    topComponent = behaviour;
                }

                topTransform = topTransform.parent;
            } while (Utilities.IsValid(topTransform));

            return topComponent;
        }
    }
}