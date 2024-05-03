#if !COMPILER_UDONSHARP && UNITY_EDITOR
using TLP.UdonUtils;
using UnityEngine.Serialization;

namespace TLP.UdonVoiceUtils.Runtime.IngameTests
{
    public class MockLocalPlayerChangeEventListener : TlpBaseBehaviour
    {
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