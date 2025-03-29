using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Core.DataProvider
{
    [Serializable]
    public class GameDataObjectFieldProvider : AbstractFieldValueProvider
    {
        // [BoxGroup("Instance")] [PreviewInInspector] [AutoParent]
        // public IDescriptableProvider _descriptableProvider;

        //1. 從Parent直接拿到MonoDescriptable
        //2. 從Variable拿到MonoDescriptable
        //FIXME: 從某個VariableDescriptableData拿到會不會更好？
        //從某個VariableMonoDescriptable拿到Data

        // [BoxGroup("Instance")] public VariableMonoDescriptableProvider _monoDescriptableProvider;
        //FIXME: 這個不好！
        // [Header("Deprecated")] [Obsolete] [PropertyOrder(-1)] [BoxGroup("Instance")]
        // public MonoDescriptableProvider<MonoDescriptable> _descriptableProvider;


        // [PropertyOrder(-1)]
        // [BoxGroup("Instance")]
        // [PreviewInInspector]
        // private GameFlagBase dataInstance => _variableProviderRef?.FinalData;
        // _descriptableProvider?.CurrentInstance?.Descriptable;

        //不一定需要instance, 有type就好了？

        protected override AbstractMonoVariable ListenToVariable => _variableProviderRef?.VarRaw;

        //_descriptableProvider?.variableProvider.Variable;
        // VarMono varMono => _variableProvider?.GetVar<VarMono>();
        [PreviewInInspector]
        [PropertyOrder(-1)]
        public override Object targetObject
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying == false) //FIXME: 如果有也可以用descriptable?
                {
                    // if (varMono == null)
                    // {
                    //     
                    //     if (_variableProvider is IVariableMonoDescriptableProvider varMonoDescriptableProvider)
                    //         return varMonoDescriptableProvider.SampleData;
                    //     return null;
                    // }
                    //
                    // if (varMono.Value == null)
                    //     return varMono.SampleData;
                    // return varMono.Value.Data;
                    // if (_descriptableProvider?.CurrentInstance?.Descriptable == null)
                    //     return _descriptableProvider?.SampleData;
                    // return _descriptableProvider?.CurrentInstance?.Descriptable as Object;
                    if (_variableProviderRef == null)
                        return null;
                }
#endif

                if (_variableProviderRef == null)
                {
                    Debug.LogError("Variable Provider is null", this);
                    return null;
                }

                return _variableProviderRef.FinalData;
                // return varMono.Value.Data; //_descriptableProvider?.CurrentInstance?.Descriptable as Object;
                //一定要sample data?
            }
        }

        public override Type targetType => typeof(DescriptableData); //有currentInstance的話，就可以直接拿到type

        // private Type dataType => _monoDescriptableProvider.GetVariable.FinalDataType; //FIXME: 還是錯...
        //Data Object Field Provider
    }
}