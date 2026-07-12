using JetBrains.Annotations;
using TLP.UdonAVLTree.Runtime;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonVoiceUtils.Runtime.Debugging;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonVoiceUtils.Runtime.Core.WorldAudio;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonVoiceUtils.Editor.Core.WorldAudio
{
    [CustomEditor(typeof(EmitterRegistry))]
    public class EmitterRegistryEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return "Maintains the set of active audio emitters registered with the importance system. " +
                   "Provides add/remove operations and rebuilds the emitter array when dirty. " +
                   "Used by WorldAudioController to track which emitters need updates.";
        }
    }
}
#endif
namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(EmitterRegistry), ExecutionOrder)]
    public class EmitterRegistry : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VoiceRangeVisualizer.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        [Header("Dependencies")]
        [SerializeField]
        internal AvlTree ImportanceTree;
        #endregion

        #region State
        internal readonly DataDictionary UniqueEmitters = new DataDictionary();
        [SerializeField]
        internal OccludedEmitter[] Emitters = new OccludedEmitter[0];
        internal bool NeedsRebuild;
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            return IsSet(ImportanceTree, nameof(ImportanceTree));
        }
        #endregion

        #region Public API
        public bool Add(OccludedEmitter occludedEmitter) {
            if (!HasStartedOk) {
                return false;
            }

            if (!Utilities.IsValid(occludedEmitter)) {
                return false;
            }

            if (UniqueEmitters.ContainsKey(occludedEmitter)) {
                return true;
            }

            UniqueEmitters.Add(occludedEmitter, 0);
            NeedsRebuild = true;
            return true;
        }

        public bool Remove(OccludedEmitter occludedEmitter) {
            if (!HasStartedOk) {
                return false;
            }

            if (!Utilities.IsValid(occludedEmitter)) {
                return false;
            }

            if (!UniqueEmitters.ContainsKey(occludedEmitter)) {
                return false;
            }

            UniqueEmitters.Remove(occludedEmitter);
            if (Utilities.IsValid(ImportanceTree)) {
                ImportanceTree.Remove(occludedEmitter);
            }

            NeedsRebuild = true;
            return true;
        }
        #endregion

        #region Internal
        internal void Initialize() {
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif
            if (!NeedsRebuild) {
                return;
            }

            var keys = UniqueEmitters.GetKeys();
            Emitters = new OccludedEmitter[keys.Count];
            int index = 0;
            for (int i = 0; i < keys.Count; i++) {
                var key = keys[i];
                if (key.TokenType != TokenType.Reference) {
                    continue;
                }

                if (!Utilities.IsValid((OccludedEmitter)key.Reference)) {
                    continue;
                }

                Emitters[index] = (OccludedEmitter)key.Reference;
                ++index;
            }

            Emitters = Emitters.ResizeOrCreate(index);
            NeedsRebuild = false;
        }
        #endregion
    }
}
