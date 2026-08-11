using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Action.VariableAction
{
    /// <summary>
    /// 一次性定量轉移：從 source 扣除 _amount，等量加到 target（source 不足時只轉剩餘量）
    /// 與 FloatChangePerSecondAction 的差別：不乘 DeltaTime，適合掛在低頻 timer（如每秒的 OnTimeUp）
    /// </summary>
    public class VarFloatTransferAction : AbstractStateAction
    {
        [Required] public VarFloat _sourceVar;
        [Required] public VarFloat _targetVar;

        public VarFloatWrapper _amount = new(1f);

        [PreviewInInspector]
        public override string Description =>
            $"{(_sourceVar != null ? _sourceVar.Description : "null")} --({(_amount != null ? _amount.Description : "null")})--> {(_targetVar != null ? _targetVar.Description : "null")}";

        protected override void OnActionExecuteImplement()
        {
            if (_sourceVar == null || _targetVar == null)
            {
                Debug.LogError("VarFloatTransferAction: Source or Target variable is not set.",
                    this);
                return;
            }

            var desired = _amount != null ? _amount.Value : 0f;
            var available = Mathf.Max(0f, _sourceVar.CurrentValue - _sourceVar.Min);
            var actual = Mathf.Min(desired, available);
            if (actual <= 0f)
                return;

            //先扣 source：Max Stamina 這種靠 modifier 吃 source 的 stat 會先升上限，target 才加得進去
            _sourceVar.AddBy(-actual, this);
            _targetVar.AddBy(actual, this);
        }
    }
}
