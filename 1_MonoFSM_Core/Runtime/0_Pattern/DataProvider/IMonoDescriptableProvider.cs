using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Attributes;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using MonoFSM.Runtime.Item_BuildSystem;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    public interface IMonoDescriptableProvider
    {
        public MonoEntity GetMonoDescriptable();
    }

    public enum ProviderType
    {
        ParentMono, //已經有Instance了
        GlobalMonoInstance, //已經有Instance了
        Variable //還不一定有。可能是null
    }


    //FIXME: TMonoDescriptable是不是不好？不要再inherit MonoDescriptable
//     [Serializable]
//     public class MonoDescriptableProvider<TMonoDescriptable> : IMonoDescriptableProvider, IValueProvider
//         where TMonoDescriptable : MonoDescriptable
//     {
//         [SerializeReferenceParentValidate] [SerializeField]
//         private MonoBehaviour propertyParent;
//         //從Parent拿
//         //從Variable拿？
//
//
//         [SerializeField] private ProviderType providerType;
//
//         //如果是parent就不需要這個了？
//         // [ShowIf("providerType", ProviderType.GlobalMonoInstance)] 
//         [SerializeField] private MonoDescriptableTag monoDescriptableTag;
//
//         [ShowIf("providerType", ProviderType.Variable)] [SerializeReference]
//         public IVarMonoProvider variableProvider;
//         //有可能沒有？
//
//         [PreviewInInspector]
//         public DescriptableData SampleData
//         {
//             get
//             {
//                 switch (providerType)
//                 {
//                     case ProviderType.Variable:
//                         if (variableProvider == null) return null;
//                         return variableProvider.SampleData;
//                 }
//
//                 var monoDescriptable = GetMonoDescriptable();
//                 if (monoDescriptable == null) return null;
// #if UNITY_EDITOR
//                 return monoDescriptable.Key?.SamepleData;
// #else
//                 return null;
// #endif
//             }
//         }
//
//         public AbstractMonoVariable GetVariable(VariableTag tag)
//         {
//             return GetMonoDescriptable()?.GetVariable(tag);
//         }
//
//         [Button]
//         private void Refresh()
//         {
//             GetMonoDescriptable();
//         }
//
//         [PreviewInInspector]
//         public MonoDescriptable GetMonoDescriptable()
//         {
//             if (propertyParent == null)
//             {
//                 Debug.LogError("No Parent Found");
//                 return null;
//             }
//
//             switch (providerType)
//             {
//                 case ProviderType.ParentMono:
//                     return propertyParent.GetMonoCompInParent(monoDescriptableTag);
//                 case ProviderType.GlobalMonoInstance:
//                     return propertyParent.GetGlobalInstance(monoDescriptableTag);
//                 case ProviderType.Variable:
//                     return variableProvider?.Variable?.Value;
//                 default:
//                     return propertyParent.GetComponentInParent<TMonoDescriptable>();
//             }
//             // return propertyParent.GetMonoDescriptableInstance(monoDescriptableTag);
//         }
//
//         [GUIColor(0.8f, 1.0f, 0.8f)]
//         [PreviewInInspector]
//         public TMonoDescriptable CurrentInstance => GetMonoDescriptable() as TMonoDescriptable;
//
//         public object GetValue()
//         {
//             return CurrentInstance;
//         }
//
//         public T GetValue<T>()
//         {
//             var value = GetValue();
//             if (value == null)
//                 return default;
//             try
//             {
//                 return (T)value;
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Cannot cast {value} to {typeof(T)}");
//                 return default;
//             }
//         }
//
//         public string Description => monoDescriptableTag.name;
//         
//     }

    //可以refactor
    // [MovedFrom(false, null, "rcg.rcgmakercore.Runtime", "MonoDescriptableSource")]
    // [Serializable]
    // public class MonoDescriptableDropdownRefProvider : IConfigVar, IMonoDescriptableProvider
    // {
    //     // [InlineEditor]
    //     [DropDownRef] public MonoDescriptable _monoDescriptable;
    //
    //     object IConfigVar.GetValue()
    //     {
    //         return _monoDescriptable;
    //     }
    //
    //     public string GetDescription()
    //     {
    //         return _monoDescriptable.name;
    //     }
    //
    //     public MonoDescriptable GetMonoDescriptable()
    //     {
    //         return _monoDescriptable;
    //     }
    // }
    //
    // [Serializable]
    // public class MonoDescriptableFromTag : IConfigVar, IMonoDescriptableProvider
    // {
    //     [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;
    //     public MonoDescriptableTag monoDescriptableTag;
    //
    //     object IConfigVar.GetValue()
    //     {
    //         return GetMonoDescriptable();
    //     }
    //
    //     public string GetDescription()
    //     {
    //         return monoDescriptableTag.name;
    //     }
    //
    //     [GUIColor(0.8f, 1.0f, 0.8f)]
    //     [PreviewInInspector]
    //     private IMonoDescriptable currentInstance => GetMonoDescriptable();
    //
    //     public MonoDescriptable GetMonoDescriptable()
    //     {
    //         return propertyParent.GetMonoCompInParent(monoDescriptableTag);
    //     }
    // }
}