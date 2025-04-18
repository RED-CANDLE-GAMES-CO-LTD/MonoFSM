using System;
using MonoFSM.Variable;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace RCGMaker.Core.DataProvider
{
    public interface IDataChangedListener
    {
        void OnDataChanged(Object data);
    }

    /// <summary>
    /// FIXME: VarMonoFieldValueProvider?
    /// </summary>
    public class ObjectOfVariableFieldValueProvider : AbstractFieldValueProvider
    {
        protected override AbstractMonoVariable ListenToVariable => _variableProviderRef?.VarRaw;
        public override Object targetObject => _variableProviderRef?.GetVar<VarMono>()?.Value;
        public override Type targetType => _variableProviderRef.GetValueType;

        // [Required] [PropertyOrder(-1)] public VariableMonoDescriptableProvider _variableProvider;

        // private void Start()
        // {
        //     if (_variableProvider == null)
        //         return;
        //     _variableProvider.Variable.OnValueChanged += OnVariableChanged;
        //     if (_variableProvider.Variable.Value != null)
        //         OnVariableChanged(_variableProvider.Variable.Value);
        // }
    }
}