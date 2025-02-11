namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class IsUnityObjectVariableNullCondition : AbstractConditionComp
    {
        [DropDownRef] public AbstractMonoReferenceVariable unityObjectVariable;

        //FIXME: Variable Tag？
        protected override bool isValid => unityObjectVariable.RawValue == null;
    }
}