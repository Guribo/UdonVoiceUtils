using TLP.UdonUtils.Runtime.Common;
using UdonSharp;
using VRC.SDKBase;

namespace TLP.UdonVoiceUtils.Runtime.Core.WorldAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ImportanceComparer : Comparer
    {
        protected override bool ComparisonImplementation(UdonSharpBehaviour first, UdonSharpBehaviour second, out int comparisonResult) {
            comparisonResult = 0;
            if (!Utilities.IsValid(first)) {
                return false;
            }

            if (!Utilities.IsValid(second)) {
                return false;
            }

            var a = (OccludedEmitter)first;
            var b = (OccludedEmitter)second;

            if (!Utilities.IsValid(a)) {
                return false;
            }

            if (!Utilities.IsValid(b)) {
                return false;
            }

            comparisonResult = a.CachedImportance.CompareTo(b.CachedImportance);
            if (comparisonResult == 0) {
                comparisonResult = a.GetInstanceID().CompareTo(b.GetInstanceID());
            }
            return true;
        }
    }
}