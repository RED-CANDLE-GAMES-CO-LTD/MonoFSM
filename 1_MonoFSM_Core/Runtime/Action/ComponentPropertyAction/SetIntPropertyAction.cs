using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.ComponentPropertyAction
{
    /// <summary>
    /// 將 int 值 Set 到任意 Component 的 int property 上
    /// </summary>
    public class SetIntPropertyAction : AbstractSetComponentPropertyAction<int>
    {
        [SerializeField] private VarIntWrapper _sourceValue;

        protected override int GetSourceValue() => _sourceValue.Value;

        protected override void SetSourceValue(int value) =>
            _sourceValue?.SetValue(value, this);

        protected override string GetSourceDescription() =>
            _sourceValue != null ? _sourceValue.Description : "0";
    }
}
