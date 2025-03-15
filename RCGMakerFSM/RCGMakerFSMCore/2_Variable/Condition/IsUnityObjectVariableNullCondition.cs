namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class IsUnityObjectVariableNullCondition : AbstractConditionComp
    {
        [DropDownRef] public AbstractObjectVariable unityObjectVariable;

        //FIXME: Variable Tag？
        protected override bool IsValid => unityObjectVariable.RawValue == null;
    }
}