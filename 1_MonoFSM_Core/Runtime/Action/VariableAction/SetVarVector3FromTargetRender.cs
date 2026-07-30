using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Sirenix.OdinInspector;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    /// <summary>
    ///     Render 版：每 render frame 把 TargetPositionResolver 解析出的位置寫入目標 VarVector3。
    ///     掛在 IRenderInvoker 底下，走 render 迴圈（VFX/SFX 同步用）。
    ///     Action 版見 SetVarVector3FromTargetAction，核心邏輯共用 PositionToVarVector3Writer。
    /// </summary>
    public class SetVarVector3FromTargetRender : AbstractRenderBehaviour
    {
        [HideLabel] [InlineProperty] public PositionToVarVector3Writer _writer = new();

        protected override bool HasError()
        {
            return base.HasError() || _writer.HasTarget == false;
        }

        public override string Description => _writer.Description;

        public override void OnEnterRenderImplement() => _writer.Write(this);

        public override void OnRenderImplement() => _writer.Write(this);
    }
}
