using System;

using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Attributes;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using MonoFSM.Runtime.Variable;
using MonoFSM.Runtime.Item_BuildSystem;
using UIValueBinder;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace MonoFSM.Core.DataProvider
{
    //重要，這個是最基本的拿到VarMono的Provider
    //FIXME: 怎麼Monobeheviour化
    public interface IVarMonoProvider //IVarMonoProvider
    {
        VarEntity Variable { get; }
        DescriptableData SampleData { get; }
        MonoEntity Value => Variable?.Value;
    }

    // [MovedFrom(true, "RCGMaker.Runtime")]
    //FIXME: 簡寫VarMonoProvider?
    //FIXME:好像還是要把常用的Property包掉，否則很難用
    // [Serializable]
    // public class VariableMonoDescriptableProvider : VariableProvider<VarBlackboard, MonoDescriptable>,
    //     IVarMonoProvider
    // {
    //     //目的：是要拿到Variable, value 是 MonoDescriptable
    //
    //     public VarBlackboard GetVarBlackboardDescriptable => GetVarRaw() as VarBlackboard;
    //
    //     [PreviewInInspector]
    //     public DescriptableData SampleData =>
    //         GetVarBlackboardDescriptable?.SampleData;
    // }

    //這個好像是正解喔？封裝完只需要宣告一個field, assign一個tag就能拿到了
    //從parent的VariableOwner拿到Variable
    //VariableProviderInParent?
    //同個owner下的variable
    //FIXME: 壞處：沒有SampleData, 不能直接拿到property
    //FIXME: 好像應該要有個可以直接傳VarBool的Provider? VarBool是DirectRef
    //FIXME:好像還是要把常用的Property包掉，否則很難用

    //Serializable至少都不會掉內容，不要用SerializeReference
    // [Serializable]
    // public class VariableProvider<TVarMonoType, TValueType> //: IVariableProvider, IVarTagProperty, IConfigVar
    //     where TVarMonoType : AbstractMonoVariable
    // {
    //     public override string ToString()
    //     {
    //         return GetValue().ToString();
    //     }
    //
    //     public Type GetValueType => typeof(TValueType);
    //
    //     public TVarMonoType GetVar()
    //     {
    //         return GetVar<TVarMonoType>();
    //     }
    //
    //     [ShowInDebugMode]
    //     [FormerlySerializedAs("propertyParent")] [SerializeReferenceParentValidate] [SerializeField]
    //     private MonoBehaviour _propertyParent;
    //
    //     private MonoBehaviour CurrentTarget
    //     {
    //         get
    //         {
    //             if (_currentTarget == null)
    //                 return _propertyParent;
    //             return _currentTarget;
    //         }
    //     }
    //
    //     [ShowInDebugMode] private MonoBehaviour _currentTarget;
    //
    //     //Dynamic Parent
    //     public AbstractMonoVariable GetMonoVariableFrom(MonoBehaviour target)
    //     {
    //         _currentTarget = target;
    //         FetchOwner(target);
    //         //FIXME:
    //         return GetVarRaw();
    //     }
    //
    //     public TValueType GetValueFrom(MonoBehaviour target)
    //     {
    //         _currentTarget = target;
    //         FetchOwner(target);
    //         return Value;
    //     }
    //
    //     private bool TypeCheckFail()
    //     {
    //         if (_varTag == null) return false;
    //         return typeof(TValueType).IsAssignableFrom(_varTag._valueFilterType.RestrictType) == false;
    //     }
    //
    //     //FIXME: dropdown validate? 多檢查parent的owner? dropdown tag?
    //     [BoxGroup("varTag")]
    //     [FormerlySerializedAs("varTag")]
    //     [InfoBox("Tag Type is wrong", InfoMessageType.Error, nameof(TypeCheckFail))]
    //     [Required]
    //     public VariableTag _varTag;
    //
    //     [BoxGroup("varTag")]
    //     [ShowInInspector]
    //     [ValueDropdown(nameof(GetParentVariableTags))]
    //     private VariableTag DropDownVarTag
    //     {
    //         set => _varTag = value;
    //         get => _varTag;
    //     }
    //     //FIXME: 拿到Variable的方式還是要很多種？
    //     //用varTag, monoTag直接找到 variable
    //     //從VarMono, 拿到他的variable
    //
    //     private void OnGlobalMonoTagChange()
    //     {
    //         _runtimeCachedOwner = null;
    //     }
    //
    //     private IEnumerable<ValueDropdownItem<VariableTag>> GetParentVariableTags()
    //     {
    //         var parents = CurrentTarget.GetComponentsInParent<MonoBlackboard>();
    //         var tags = new List<ValueDropdownItem<VariableTag>>();
    //         foreach (var parent in parents)
    //         foreach (var variable in parent.VariableFolder.GetValues)
    //             if (variable is TVarMonoType)
    //                 tags.Add(new ValueDropdownItem<VariableTag>(variable.name, variable._varTag));
    //
    //         return tags;
    //     }
    //
    //     private IEnumerable<ValueDropdownItem<MonoDescriptableTag>> GetParentMonoTags()
    //     {
    //         var parents = CurrentTarget.GetComponentsInParent<MonoDescriptable>();
    //         var tags = new List<ValueDropdownItem<MonoDescriptableTag>>();
    //         foreach (var parent in parents)
    //             tags.Add(new ValueDropdownItem<MonoDescriptableTag>(parent.Tag.name, parent.Tag));
    //
    //         return tags;
    //     }
    //
    //     // [ValueDropdown(nameof(GetGlobalMonoTags))] [OnValueChanged(nameof(OnGlobalMonoTagChange))]
    //     //FIXME: 1. 常常會空著
    //     public MonoDescriptableTag _parentMonoTag; //空的話就是自己
    //
    //     [ShowInDebugMode]private Type variableValueType => typeof(TValueType);
    //     //FIXME:也可以用string拿？
    //     // MonoDescriptable parentDescriptable => propertyParent.GetComponentInParent<MonoDescriptable>();
    //
    //     //prefab裏可以不用有
    //     //FIXME: 這個auto parent是不是不會跑到？是靠Inspector code才抓到的
    //     //FIXME: 這樣沒有辦法提早cache?
    //     // [AutoParent]
    //     [ShowInDebugMode]
    //     public MonoBlackboard owner
    //     {
    //         get
    //         {
    //             if (Application.isPlaying && _runtimeCachedOwner != null) //runtime才要cache
    //                 return _runtimeCachedOwner;
    //
    //             _runtimeCachedOwner = FetchOwner(CurrentTarget);
    //             return _runtimeCachedOwner;
    //         }
    //     }
    //
    //     private MonoBlackboard FetchOwner(MonoBehaviour target)
    //     {
    //         if (target == null)
    //         {
    //             if (Application.isPlaying)
    //                 Debug.LogError("Target is null", _propertyParent);
    //             return null;
    //         }
    //
    //         if (_parentMonoTag != null)
    //         {
    //             var monoCompInParent = target.GetMonoCompInParent(_parentMonoTag);
    //             if (monoCompInParent == null) return null;
    //             //FIXME: 
    //             return monoCompInParent;
    //         }
    //
    //         _runtimeCachedOwner = target.GetComponentInParent<MonoBlackboard>();
    //         if (_runtimeCachedOwner == null)
    //             Debug.LogError("VariableOwner InParent is null at:" + target, target);
    //         return _runtimeCachedOwner;
    //         // return _runtimeCachedOwner;
    //     }
    //
    //     private MonoBlackboard _runtimeCachedOwner;
    //
    //     public void SetValue(TValueType value, MonoBehaviour byWho)
    //     {
    //         GetVarRaw().SetValue(value, byWho);
    //     }
    //
    //     public TMonoVar GetVar<TMonoVar>() where TMonoVar : AbstractMonoVariable
    //     {
    //         return GetVarRaw() as TMonoVar;
    //     }
    //
    //
    //     [GUIColor(0.8f, 1.0f, 0.8f)]
    //     [PreviewInInspector]
    //     public TVarMonoType Variable => GetVarRaw(false) as TVarMonoType; //這個如果沒抓到不要噴error，preview用的
    //
    //     // [GUIColor(0.8f, 1.0f, 0.8f)]
    //     // [PreviewInInspector]
    //
    //     public AbstractMonoVariable GetVarRaw(bool errorIfNotFound = true)
    //     {
    //         if (owner == null)
    //         {
    //             if (Application.isPlaying)
    //                 Debug.LogError("Owner is null", CurrentTarget);
    //             return null;
    //         }
    //
    //         if (owner.VariableFolder == null)
    //         {
    //             if (Application.isPlaying)
    //                 Debug.LogError("VariableFolder is null", CurrentTarget);
    //             return null;
    //         }
    //
    //         Debug.Log("GetVariable:" + _varTag+"owner:"+owner, owner);
    //         var variable = owner.GetVariable(_varTag);
    //         //FIXME: 怎麼樣算正常？
    //         //bool isNullOk
    //
    //         if (variable == null && errorIfNotFound)
    //             Debug.LogError($"Variable{_varTag} is null in owner{owner}", CurrentTarget);
    //
    //         return variable;
    //     }
    //
    //     // public AbstractMonoVariable VarRaw
    //     // {
    //     //     get
    //     //     {
    //     //         if (owner == null)
    //     //         {
    //     //             if (Application.isPlaying)
    //     //                 Debug.LogError("Owner is null", CurrentTarget);
    //     //             return null;
    //     //         }
    //     //
    //     //         if (owner.VariableFolder == null)
    //     //         {
    //     //             if (Application.isPlaying)
    //     //                 Debug.LogError("VariableFolder is null", CurrentTarget);
    //     //             return null;
    //     //         }
    //     //
    //     //         var variable = owner.GetVariable(_varTag);
    //     //         //FIXME: 怎麼樣算正常？
    //     //         //bool isNullOk
    //     //         
    //     //         if (variable == null)
    //     //             Debug.LogError($"Variable{_varTag} is null in owner{owner}", CurrentTarget);
    //     //
    //     //         return variable;
    //     //     }
    //     // }
    //
    //     // [ShowInInspector]
    //     // RCGVariableFolder GetFolder =>  owner?.VariableFolder;
    //     [ShowInDebugMode]
    //     public TValueType Value => GetVarRaw(false) == null ? default : GetVarRaw(false).GetValue<TValueType>();
    //
    //     public VariableTag varTag
    //     {
    //         get => _varTag;
    //         set => _varTag = value;
    //     }
    //
    //     public object GetValue()
    //     {
    //         return Value;
    //     }
    // }

    public interface IVarTagProperty //給Drawer拿來用的, 必須是property才需要被折疊 (State.Expand
    {
        VariableTag varTag { get; set; }
    }


    //FIXME: 這有啥用？ TargetVarRef
    public interface IVariableProvider //ValueProvider?
    {
        // public GameFlagBase FinalData => VarRaw?.FinalData;
        AbstractMonoVariable VarRaw { get; } //還是其實這個也可以？
        // Type GetValueType { get; }

        TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable;
        // object SampleData { get; }
        // AbstractMonoVariable GetMonoVariableFrom(MonoBehaviour target);
    }

    //FindVar("Owner").MonoDescriptable.GetVariable(varTag);
    //倒著寫不太舒服...
    //GetMonoInParent().GetVariable(varTag);
    //某個MonoDescriptable的Variable, MonoDescriptable是某個Variable的值...

    public interface IValue<out TValue>
    {
        TValue Value { get; }
    }

    //我想要拿到一個值，方法有：
    //從MonoVariable拿
    //從MonoVariable的Value的GetVariable的(tag) 拿
    //可以一路連到天邊
    //GetVariable(Tag)
    //GetVariable(GetVariable(Tag).Value).Value
    //Variable.Value.GetVariable(Tag).
    //GetVariable(Tag).Value

    // [Serializable]
    // public struct VariableChainStep
    // {
    //     // public VariableOwner owner;
    //     // [SerializeReference] public IVariableTagProvider variableTagProvider;
    //     public bool IsTagFromVariable;
    //     public MonoDescriptableTag monoParentTag;
    //     [HideIf("IsTagFromVariable")] public VariableTag TagConfig;
    //     [ShowIf("IsTagFromVariable")] public VariableTagFromVariable TagFromVariable;
    //     public VariableTag Tag => IsTagFromVariable ? TagFromVariable.Value : TagConfig;
    //
    //     public VariableOwner GetParentOwner(MonoBehaviour target) => target.GetMonoCompInParent(monoParentTag);
    //
    //     public AbstractMonoVariable GetVariable(MonoBehaviour target) => GetParentOwner(target).GetVariable(Tag);
    // }

    //var currentSelectEquip = GetVariable("currentSelect").Value;
    //var player = GetVariable("player").Value;
    //var typeTagOfCurrentSelectEquip = GetVariable("currentSelect").Value.GetVariable("EquipType").Value;  
    //player.GetVariable(typeTagOfCurrentSelectEquip).Value = currentSelectEquip

    // [Serializable]
    // public class MonoValueProvider
    // {
    //     [SerializeReferenceParentValidate] public MonoBehaviour parentMono;
    //
    //     public VariableChainStep[] variableChainSteps;
    //
    //     // [SerializeReference] public IVariableTagProvider[] variableEntries;
    //     public T GetValue<T>()
    //     {
    //         var target = parentMono;
    //         // var variableOwner = parentMono.GetComponentInParent<VariableOwner>();
    //         AbstractMonoVariable currentVariable = null;
    //         var index = 0;
    //         while (index < variableChainSteps.Length)
    //         {
    //             var entry = variableChainSteps[index];
    //             currentVariable = entry.GetVariable(target);
    //             // currentVariable = variableOwner.GetVariable(entry.Tag);
    //             if (currentVariable == null) return default;
    //             var value = currentVariable.objectValue;
    //             if (value is VariableOwner owner)
    //             {
    //                 target = owner;
    //             }
    //         }
    //
    //         if (currentVariable == null) return default;
    //         return currentVariable.GetValue<T>();
    //     }
    // }

    public interface IVariableTagProvider : IValue<VariableTag>
    {
    }

    [Serializable]
    public class VariableTagRefProvider : IVariableTagProvider
    {
        public VariableTag _variableTag;
        public VariableTag Value => _variableTag;
    }

    // [Serializable]
    // public class VariableTagFromVariable : IVariableTagProvider
    // {
    //     // IVariableTagProvider _varTagProvider;
    //     public VarTagVariable _monoVariable;
    //     public VariableTag Value => _monoVariable?.Value;
    // }

    [Serializable]
    public class ValueRefProvider<TValue> : IValue<TValue>
    {
        public enum ProviderType
        {
            DirectRef,
            ParentMono, //已經有Instance了
            GlobalMonoInstance, //已經有Instance了
            Variable //還不一定有。可能是null
        }

        [SerializeReferenceParentValidate] [SerializeField]
        private MonoBehaviour propertyParent;
        //從Parent拿
        //從Variable拿？


        [SerializeField] private ProviderType providerType;

        public TValue _valueRef;
        [DropDownRef] public TValue _valueRefFromDropDown;
        public TValue Value => _valueRef;
    }


    //超級無敵複雜？
    //FIXME: 這個可以砍掉？
    [Serializable]
    public class VariableProviderFromMonoDescriptable : IVariableProvider
    {
        [SerializeReference] public IMonoDescriptableProvider _monoDescriptableProvider;

        //FIXME: 連tag都可能需要DI
        //FIXME: 任何資料都可能可以DI...VariableEntry
        public VariableTag _varTag;

        public AbstractMonoVariable VarRaw =>
            _monoDescriptableProvider?.GetMonoDescriptable()?.GetVar(_varTag);

        public Type GetValueType => typeof(MonoEntity);

        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return VarRaw as TVariable;
        }
    }

    public interface IDynamicVariableProvider //動態拿到Variable
    {
        AbstractMonoVariable GetMonoVariable(MonoBehaviour target);
    }

    // public class VariableProviderFromParentEntity : IVariableProvider
    // {
    //     [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;
    //     public VariableTag varTag;
    //     private MonoDescriptable parentDescriptable => propertyParent.GetComponentInParent<MonoDescriptable>();
    //     public AbstractMonoVariable GetMonoVariable => parentDescriptable.GetVariable(varTag);
    // }

    //FIXME: 這個class很冗？

    public class VariableProviderFromGlobalInstance<TVariable> : IVariableProvider
        where TVariable : AbstractMonoVariable
    {
        [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;

        //FIXME: tag需要更鬆一點？類似同個型別都吃？interface...MonoDescriptable... MonoUISelecting
        [FormerlySerializedAs("monoDescriptableTag")] [Required]
        public MonoEntityTag _monoEntityTag;
        [Required] public VariableTag varTag;

        [PreviewInInspector]
        public AbstractMonoVariable VarRaw
        {
            get
            {
                if (varTag == null && Application.isPlaying)
                {
                    Debug.LogError("Variable Tag is null", propertyParent);
                    return null;
                }

                var descriptable = propertyParent.GetGlobalInstance(_monoEntityTag);
                if (descriptable == null) return null;
                return descriptable.GetVar(varTag);
            }
        }

        public Type GetValueType => VarRaw.ValueType;

        public TVariable1 GetVar<TVariable1>() where TVariable1 : AbstractMonoVariable
        {
            return VarRaw as TVariable1;
        }

        public TVariable GetMonoVar()
        {
            return VarRaw as TVariable;
        }
    }

    //FIXME: 這個連型別都沒有，太粗了吧？
    [Serializable]
    public class VariableProviderFromGlobalInstance : IVariableProvider //fixme
    {
        [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;

        //FIXME: tag需要更鬆一點？類似同個型別都吃？interface...MonoDescriptable... MonoUISelecting
        [FormerlySerializedAs("monoDescriptableTag")] [Required]
        public MonoEntityTag _monoEntityTag;
        [Required] public VariableTag varTag;

        [PreviewInInspector]
        public AbstractMonoVariable VarRaw
        {
            get
            {
                if (varTag == null && Application.isPlaying)
                {
                    Debug.LogError("Variable Tag is null", propertyParent);
                    return null;
                }

                var descriptable = propertyParent.GetGlobalInstance(_monoEntityTag);
                if (descriptable == null) return null;
                return descriptable.GetVar(varTag);
            }
        }

        public Type GetValueType => VarRaw.ValueType;

        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return VarRaw as TVariable;
        }
    }

    // /// <summary>
    // ///     同個owner下的variable
    // /// </summary>
    // [Serializable]
    // public class VariableProviderByTag : IConfigVar, IVariableProvider
    // {
    //     [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;
    //     public VariableTag varTag; //動態拿
    //
    //     object IConfigVar.GetValue()
    //     {
    //         return MonoVariable;
    //     }
    //
    //     [PreviewInInspector]
    //     public AbstractMonoVariable MonoVariable
    //     {
    //         get
    //         {
    //             if (_cachedMonoVariable == null) BindCache();
    //             return _cachedMonoVariable;
    //         }
    //     }
    //
    //     public TVariable GetMonoVar<TVariable>() where TVariable : AbstractMonoVariable
    //     {
    //         return MonoVariable as TVariable;
    //     }
    //
    //     private AbstractMonoVariable _cachedMonoVariable;
    //
    //     //不能用autoParent了齁... 還是連nested class都可以爬出來，或是掛[AutoClassBinder]
    //     private void BindCache()
    //     {
    //         if (propertyParent == null) return;
    //         var owner = propertyParent.GetComponentInParent<IVariableOwner>();
    //         if (owner == null) return; //會一直叫...怎麼辦... 用getter不好，應該是要從Editor/Odin那邊叫
    //         _cachedMonoVariable = owner.VariableFolder.GetVariable(varTag);
    //     }
    // }

    //dropdown選owner下的variable, 好像還算蠻好的？FIXME: 但沒有用到tag?太特定
    // [Serializable]
    // public class VariableInOwner : IConfigVar, IVariableProvider
    // {
    //     // [InlineEditor]
    //     // public VariableTag varTag; //這個assign也要被限定範圍？
    //     // // public object GetValue => varTag;
    //     //
    //     //Direct Ref, 不太好
    //     [Required] [DropDownRef] public AbstractMonoVariable _monoVariable;
    //
    //     object IConfigVar.GetValue()
    //     {
    //         // throw new NotImplementedException();
    //         return _monoVariable.objectValue;
    //     }
    //
    //     public AbstractMonoVariable VarRaw => _monoVariable;
    //
    //     public TVariable GetMonoVar<TVariable>() where TVariable : AbstractMonoVariable
    //     {
    //         return _monoVariable as TVariable;
    //     }
    // }
}