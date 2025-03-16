namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    //FIXME: 並沒有註冊唷
    public class IsUnityObjectVariableNullCondition : AbstractConditionComp
    {
        [DropDownRef] public AbstractObjectVariable unityObjectVariable;

        //FIXME: Variable Tag？
        protected override bool IsValid => unityObjectVariable.RawValue == null;
        
    }
}