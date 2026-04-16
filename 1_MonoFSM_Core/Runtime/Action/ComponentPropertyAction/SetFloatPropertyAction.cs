using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.ComponentPropertyAction
{
    /// <summary>
    /// 將 float 值 Set 到任意 Component 的 float property 上
    /// </summary>
    public class SetFloatPropertyAction : AbstractSetComponentPropertyAction<float>
    {
        [SerializeField] private VarFloatWrapper _sourceValue;

        protected override float GetSourceValue() => _sourceValue.Value;

        protected override void SetSourceValue(float value) =>
            _sourceValue?.SetValue(value, this);

        protected override string GetSourceDescription() =>
            _sourceValue != null ? _sourceValue.Description : "0";
    }
}
