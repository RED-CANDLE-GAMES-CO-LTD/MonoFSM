using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.Variable
{
    public class TargetFrameRateValueSource : AbstractValueSource<int>
    {
        public override int Value => Application.targetFrameRate;
        public override bool HasValue => true;
    }
}
