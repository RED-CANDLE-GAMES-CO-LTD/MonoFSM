namespace MonoFSM.Variable.Condition
{
    //FIXME: 並沒有註冊唷
    public class IsUnityObjectVariableNullCondition : AbstractConditionBehaviour
    {
        [DropDownRef] public AbstractObjectVariable unityObjectVariable;

        //FIXME: Variable Tag？
        protected override bool IsValid 
            => unityObjectVariable.RawValue == null;
    }
}