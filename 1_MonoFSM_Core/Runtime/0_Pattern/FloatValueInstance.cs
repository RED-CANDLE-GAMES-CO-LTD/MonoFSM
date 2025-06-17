using MonoFSM.Variable;

namespace MonoFSM.Core
{
    public class FloatValueInstance : ValueInstance<float>, IFloatValueProvider
    {
        public float FinalValue 
            => SourceValue;
    }
}