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
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestCase.ExecutionOrder + 100;

        [FormerlySerializedAs("added")]
        public bool Added;

        [FormerlySerializedAs("removed")]
        public bool Removed;

        public void _LocalPlayerAdded() {
            Added = true;
        }

        public void _LocalPlayerRemoved() {
            Removed = true;
        }

        public override void _OnEvent(string eventName) {
            switch (eventName) {
                case nameof(_LocalPlayerAdded):
                    _LocalPlayerAdded();
                    break;
                case nameof(_LocalPlayerRemoved):
                    _LocalPlayerRemoved();
                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }
    }
}