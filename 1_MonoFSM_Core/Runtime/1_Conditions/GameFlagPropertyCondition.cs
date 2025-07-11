namespace MonoFSM.Core
{
    //放一個condition，檢查GameState的某個Property
    public class GameFlagPropertyCondition : AbstractConditionBehaviour
    {
        public FlagFieldBoolEntry FieldBool;
        protected override bool IsValid => FieldBool.isValid;
    }
}