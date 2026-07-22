using System;
using MonoFSM.Variable;
using MonoValueProvider;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    /// <summary>
    ///     共用邏輯：把 TargetPositionResolver 解析出的位置寫入目標 VarVector3。
    ///     來源支援 VarVector3 / VarTransform / VarEntity（由 resolver 依序解析）。
    ///     給 Action 版（SetVarVector3FromTargetAction）與 Render 版
    ///     （SetVarVector3FromTargetRender）共用，避免重複實作。
    /// </summary>
    [Serializable]
    public class PositionToVarVector3Writer
    {
        [InlineProperty] [HideLabel] public TargetPositionResolver _source = new();

        [Required] [DropDownRef] public VarVector3 _targetVar;

        public string Description =>
            $"{_source.BindingSource} => {(_targetVar != null ? _targetVar.name : "?")}";

        public void Write(Object byWho)
        {
            if (_targetVar == null)
            {
                Debug.LogError("targetVar is null", byWho);
                return;
            }

            // 沒有有效來源就不寫，避免蓋掉既有值
            if (!_source.HasTarget)
                return;

            _targetVar.SetValue(_source.GetTargetPosition(_targetVar.Value), byWho);
        }
    }
}
