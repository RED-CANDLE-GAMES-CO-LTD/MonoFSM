using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    /// <summary>
    ///     把 TargetPositionResolver 解析出的位置寫入目標 VarVector3。
    ///     來源支援 VarVector3 / VarTransform / VarEntity。
    ///     與 SetLocalPositionAction 相反（那個是讀 Var 寫回 Transform）。
    ///     核心邏輯在 PositionToVarVector3Writer，Render 版見 SetVarVector3FromTargetRender。
    /// </summary>
    public class SetVarVector3FromTargetAction : AbstractStateAction
    {
        [HideLabel] [InlineProperty] public PositionToVarVector3Writer _writer = new();

        public override string Description => _writer.Description;

        protected override void OnActionExecuteImplement() => _writer.Write(this);
    }
}
