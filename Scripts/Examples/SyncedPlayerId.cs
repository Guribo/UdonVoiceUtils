using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Scripts.Examples
{
    /// <summary>
    /// Component which is used to synchronize a value on demand independently from high continuous synced udon
    /// behaviours to reduce bandwidth.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncedPlayerId : UdonSharpBehaviour
    {
        [UdonSynced]
        public int playerId = -1;

        /// <summary>
        /// Udon behaviour that wants to have one of its variables synced to all players
        /// </summary>
        [SerializeField] protected UdonBehaviour targetBehaviour;
        /// <summary>
        /// Variable which will get synchronized with all players
        /// </summary>
        [SerializeField] protected string targetVariable = "playerId";

        /// <summary>
        /// Triggers Serialization of the manually synced player id.
        /// Does nothing if the caller does not own this behaviour/gameobject.
        /// </summary>
        /// <returns>false if the local player is not the owner or anything goes wrong</returns>
        public bool UpdateForAll()
        {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(targetBehaviour) 
                || !Utilities.IsValid(localPlayer) 
                || !localPlayer.IsOwner(gameObject))
            {
                return false;
            }
            
            targetBehaviour.SendCustomEvent("OnPreSerialization");

            var value = targetBehaviour.GetProgramVariable(targetVariable);
            if (value == null)
            {
                Debug.LogError($"'{targetVariable}' does not exist in '{targetBehaviour.name}'");
                return false;
            }

            // ReSharper disable once OperatorIsCanBeUsed
            if (value.GetType() == typeof(int))
            {
                playerId = (int) value;
                RequestSerialization();
                return true;
            }

            Debug.LogError($"'{targetVariable}' in '{targetBehaviour.name}' is not an integer");
            return false;
        }

        public override void OnDeserialization()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer.IsOwner(gameObject) || 
                !Utilities.IsValid(targetBehaviour) 
                || !Utilities.IsValid(localPlayer))
            {
                return;
            }

            // refresh the variable in the target udon behaviour
            targetBehaviour.SetProgramVariable(targetVariable, playerId);
            targetBehaviour.SendCustomEvent("OnDeserialization");
        }

        public override void OnPostSerialization()
        {
            if (Utilities.IsValid(targetBehaviour))
            {
                targetBehaviour.SendCustomEvent("OnPostSerialization");
            }
        }
    }
}