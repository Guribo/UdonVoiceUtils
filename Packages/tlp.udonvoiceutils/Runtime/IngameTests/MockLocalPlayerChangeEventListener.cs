#if !COMPILER_UDONSHARP && UNITY_EDITOR
using TLP.UdonUtils;

namespace TLP.UdonVoiceUtils.Runtime.IngameTests
{
    public class MockLocalPlayerChangeEventListener : TlpBaseBehaviour
    {
        public bool added;
        public bool removed;

        public void LocalPlayerAdded()
        {
            added = true;
        }

        public void LocalPlayerRemoved()
        {
            removed = true;
        }

        public override void OnEvent(string name)
        {
            switch (name)
            {
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