using System;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Foundation;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    /// <summary>
    /// 把一顆 VarFloat 設成 _sourceVar 的值（常數或另一顆 VarFloat），掛在 [Event] 底下寫數值用。
    /// _targetVar 可以是跨 entity 的 proxy Var（例：掛貨滑車 spawn 包裹後寫 d_托盤貨物.d_商品價格）。
    /// </summary>
    [QuickCreate]
    public class SetVarFloatConstAction : AbstractStateAction
    {
        public override string Description =>
            $"Set {_targetVar?.Description} to {_sourceVar.Description}";

        [FormerlySerializedAs("targetVar")]
        // [MCPExtractable]
        [DropDownRef]
        public VarFloat _targetVar;

        // [Obsolete]
        // public float TargetValue;

        public VarFloatWrapper _sourceVar;

        protected override void OnActionExecuteImplement()
        {
            // if (_sourceVar._var == null && _sourceVar.Value == 0f) //舊規
            //     targetVar.SetValue(TargetValue, this);
            // else //新規
            _targetVar.SetValue(_sourceVar.Value, this);
        }

    }
}
