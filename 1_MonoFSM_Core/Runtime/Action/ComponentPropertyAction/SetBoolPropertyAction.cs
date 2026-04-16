using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.ComponentPropertyAction
{
    /// <summary>
    /// 將 bool 值 Set 到任意 Component 的 bool property 上
    /// </summary>
    public class SetBoolPropertyAction : AbstractSetComponentPropertyAction<bool>
    {
        [SerializeField] private VarBoolWrapper _sourceValue = new(true);

        protected override bool GetSourceValue() => _sourceValue.Value;

        protected override void SetSourceValue(bool value) =>
            _sourceValue?.SetValue(value, this);

        protected override string GetSourceDescription() =>
            _sourceValue != null ? _sourceValue.Description : "false";
    }
}
