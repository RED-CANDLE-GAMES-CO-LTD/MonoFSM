using System;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.DataProvider
{
    public interface IDataChangedListener //和下面差不多？
    {
        void OnDataChanged(Object data);
    }

    /// <summary>
    /// 我想要監聽某個變數的變化，然後做出相應的處理。
    /// </summary>
    public interface IVarChangedListener //FIXME: 要有人幫忙註冊吧？
    {
        void OnVarChanged(AbstractMonoVariable variable);
        //FIXME: 身邊需要IDataChangedProvider?
    }

    public interface IDataChangedProvider
    {
    }

    /// <summary>
    /// FIXME: VarMonoFieldValueProvider?
    /// </summary>
    public class ObjectOfVariableFieldOfVarProvider : AbstractFieldOfVarProvider
    {
        // protected override AbstractMonoVariable ListenToVariable => _variableProviderRef?.VarRaw;
        // public override Object targetObject =>
        //     _objectProviderRef?.Get<Object>(); // _variableProviderRef?.GetVar<VarMono>()?.Value;

        // public override Type targetType => _objectProviderRef?.ValueType; //_variableProviderRef.GetValueType;

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