using TLP.UdonUtils.Editor;
using UnityEditor;

namespace TLP.UdonVoiceUtils.Editor
{
    [InitializeOnLoad]
    public class UdonVoiceUtilsDefinitions
    {
        static UdonVoiceUtilsDefinitions() {
            CustomDefinitionUtils.EnsureDefinitionsExist(typeof(UdonVoiceUtilsDefinitions), "TLP_UDONVOICEUTILS");
        }
    }
}