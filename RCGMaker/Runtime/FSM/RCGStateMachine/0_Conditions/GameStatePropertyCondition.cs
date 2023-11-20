namespace RCGMaker.Core
{
    //放一個condition，檢查GameState的某個Property
    public class GameStatePropertyCondition : AbstractConditionComp
    {
        public FlagFieldBoolEntry FieldBool;
        protected override bool isValid => FieldBool.isValid;
    }
}