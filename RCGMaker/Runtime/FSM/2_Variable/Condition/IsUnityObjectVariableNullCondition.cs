namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class IsUnityObjectVariableNullCondition : AbstractConditionComp
    {
        [DropDownRef] public AbstractObjectVariable unityObjectVariable;

        //FIXME: Variable Tag？
        protected override bool isValid => unityObjectVariable.RawValue == null;
    }
}