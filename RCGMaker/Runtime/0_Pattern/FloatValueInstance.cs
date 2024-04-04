using RCGMaker.Runtime.FSM._2_Variable;

namespace RCGMaker.Core
{
    public class FloatValueInstance : ValueInstance<float>, IFloatValue
    {
        public float FinalValue => SourceValue;
    }
}