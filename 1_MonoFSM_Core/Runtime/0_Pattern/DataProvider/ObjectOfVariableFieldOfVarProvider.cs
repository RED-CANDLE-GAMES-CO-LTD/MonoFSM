using System;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.DataProvider
{
    public interface IDataChangedListener
    {
        void OnDataChanged(Object data);
    }

    /// <summary>
    /// FIXME: VarMonoFieldValueProvider?
    /// </summary>
    public class ObjectOfVariableFieldOfVarProvider : AbstractFieldOfVarProvider
    {
        // protected override AbstractMonoVariable ListenToVariable => _variableProviderRef?.VarRaw;
        public override Object targetObject =>
            _objectProviderRef?.Get<Object>(); // _variableProviderRef?.GetVar<VarMono>()?.Value;

        public override Type targetType => _objectProviderRef?.ValueType; //_variableProviderRef.GetValueType;

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