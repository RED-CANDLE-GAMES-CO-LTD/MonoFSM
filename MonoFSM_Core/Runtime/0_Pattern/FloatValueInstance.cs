using MonoFSM.Variable;

namespace RCGMaker.Core
{
    public class FloatValueInstance : ValueInstance<float>, IFloatValueProvider
    {
        public float FinalValue => SourceValue;
    }
}