#if !COMPILER_UDONSHARP && UNITY_EDITOR
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace TLP.UdonVoiceUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(MockLocalPlayerChangeEventListener), ExecutionOrder)]
    public class MockLocalPlayerChangeEventListener : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestCase.ExecutionOrder + 100;

        [FormerlySerializedAs("added")]
        public bool Added;

        [FormerlySerializedAs("removed")]
        public bool Removed;

        public void LocalPlayerAdded() {
            Added = true;
        }

        public void LocalPlayerRemoved() {
            Removed = true;
        }

        public override void OnEvent(string name) {
            switch (name) {
                case "LocalPlayerAdded":
                    LocalPlayerAdded();
                    break;
                case "LocalPlayerRemoved":
                    LocalPlayerRemoved();
                    break;
                default:
                    base.OnEvent(name);
                    break;
            }
        }
    }
}
#endif