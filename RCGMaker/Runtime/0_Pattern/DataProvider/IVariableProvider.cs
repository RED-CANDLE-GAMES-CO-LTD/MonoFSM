using System;
using System.Collections.Generic;
using System.Reflection;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.FSM.RCGStateMachine;
using RCGMaker.Runtime.Item_BuildSystem;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using RCGMaker.Runtime.Mono;
using RCGUIBinder;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Core.DataProvider
{
    public interface IVariableMonoDescriptableProvider
    {
        VarMono GetVarMonoDescriptable { get; }
        DescriptableData SampleData { get; }
    }


    [Serializable]
    public class VariableMonoDescriptableProvider : VariableProvider<MonoDescriptable>,
        IVariableMonoDescriptableProvider
    {
        //目的：是要拿到Variable, value 是 MonoDescriptable

        public VarMono GetVarMonoDescriptable => Variable as VarMono;

        [PreviewInInspector]
        public DescriptableData SampleData =>
            GetVarMonoDescriptable?.SampleData;
    }

    //這個好像是正解喔？封裝完只需要宣告一個field, assign一個tag就能拿到了
    //從parent的VariableOwner拿到Variable
    //VariableProviderInParent?
    //同個owner下的variable
    //FIXME: 壞處：沒有SampleData, 不能直接拿到property
    [Serializable]
    public class VariableProvider<TVariableType> : IVariableProvider, IVarTagProperty
    {
        [FormerlySerializedAs("propertyParent")] [SerializeReferenceParentValidate] [SerializeField]
        private MonoBehaviour _propertyParent;

        private MonoBehaviour CurrentTarget
        {
            get
            {
                if (_currentTarget == null)
                    return _propertyParent;
                return _currentTarget;
            }
        }

        [PreviewInInspector] private MonoBehaviour _currentTarget;

        //Dynamic Parent
        public AbstractMonoVariable GetMonoVariableFrom(MonoBehaviour target)
        {
            _currentTarget = target;
            FetchOwner(target);
            //FIXME:
            return Variable;
        }

        public TVariableType GetValueFrom(MonoBehaviour target)
        {
            _currentTarget = target;
            FetchOwner(target);
            return Value;
        }

        private bool TypeCheckFail()
        {
            if (_varTag == null) return false;
            return typeof(TVariableType).IsAssignableFrom(_varTag._valueFilterType.RestrictType) == false;
        }

        //FIXME: dropdown validate? 多檢查parent的owner?
        [FormerlySerializedAs("varTag")]
        [InfoBox("Tag Type is wrong", InfoMessageType.Error, nameof(TypeCheckFail))]
        [Required]
        public VariableTag _varTag;

        //FIXME: 拿到Variable的方式還是要很多種？
        //用varTag, monoTag直接找到 variable
        //從VarMono, 拿到他的variable

        private void OnGlobalMonoTagChange()
        {
            _runtimeCachedOwner = null;
        }

        IEnumerable<ValueDropdownItem<MonoDescriptableTag>> GetParentMonoTags()
        {
            var parents = CurrentTarget.GetComponentsInParent<MonoDescriptable>();
            var tags = new List<ValueDropdownItem<MonoDescriptableTag>>();
            foreach (var parent in parents)
            {
                tags.Add(new ValueDropdownItem<MonoDescriptableTag>(parent.Tag.name, parent.Tag));
            }

            return tags;
        }

        // [ValueDropdown(nameof(GetGlobalMonoTags))] [OnValueChanged(nameof(OnGlobalMonoTagChange))]
        //FIXME: 1. 常常會空著
        public MonoDescriptableTag _parentMonoTag; //空的話就是自己

        [PreviewInInspector] private Type variableValueType => typeof(TVariableType);
        //FIXME:也可以用string拿？
        // MonoDescriptable parentDescriptable => propertyParent.GetComponentInParent<MonoDescriptable>();

        //prefab裏可以不用有
        //FIXME: 這個auto parent是不是不會跑到？是靠Inspector code才抓到的
        //FIXME: 這樣沒有辦法提早cache?
        // [AutoParent]
        [PreviewInInspector]
        public VariableOwner owner
        {
            get
            {
                if (Application.isPlaying && _runtimeCachedOwner != null) //runtime才要cache
                    return _runtimeCachedOwner;

                _runtimeCachedOwner = FetchOwner(CurrentTarget);
                return _runtimeCachedOwner;
            }
        }

        VariableOwner FetchOwner(MonoBehaviour target)
        {
            if (target == null)
            {
                Debug.LogError("Target is null", _propertyParent);
                return null;
            }

            if (_parentMonoTag != null)
            {
                var monoCompInParent = target.GetMonoCompInParent(_parentMonoTag);
                if (monoCompInParent == null) return null;
                //FIXME: 
                return monoCompInParent;
            }

            _runtimeCachedOwner = target.GetComponentInParent<VariableOwner>();
            if (_runtimeCachedOwner == null)
                Debug.LogError("VariableOwner InParent is null at:" + target, target);
            return _runtimeCachedOwner;
            // return _runtimeCachedOwner;
        }

        private VariableOwner _runtimeCachedOwner;

        public void SetValue(TVariableType value, MonoBehaviour byWho)
        {
            Variable.SetValue(value, byWho);
        }

        public TMonoVar GetMonoVar<TMonoVar>() where TMonoVar : AbstractMonoVariable
        {
            return Variable as TMonoVar;
        }

        [GUIColor(0.8f, 1.0f, 0.8f)]
        [PreviewInInspector]
        public AbstractMonoVariable Variable
        {
            get
            {
                if (owner == null)
                {
                    if (Application.isPlaying)
                        Debug.LogError("Owner is null", CurrentTarget);
                    return null;
                }

                if (owner.VariableFolder == null)
                {
                    if (Application.isPlaying)
                        Debug.LogError("VariableFolder is null", CurrentTarget);
                    return null;
                }

                return owner.GetVariable(_varTag);
            }
        }

        // [ShowInInspector]
        // RCGVariableFolder GetFolder =>  owner?.VariableFolder;
        [PreviewInInspector]
        public TVariableType Value => Variable == null ? default : Variable.GetValue<TVariableType>();

        public VariableTag varTag
        {
            get => _varTag;
            set => _varTag = value;
        }
    }

    public interface IVarTagProperty
    {
        VariableTag varTag { get; set; }
    }

    public interface IVariableProvider
    {
        AbstractMonoVariable Variable { get; } //還是其實這個也可以？

        TVariable GetMonoVar<TVariable>() where TVariable : AbstractMonoVariable;
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

    [Serializable]
    public class VariableTagFromVariable : IVariableTagProvider
    {
        // IVariableTagProvider _varTagProvider;
        public VarTagVariable _monoVariable;
        public VariableTag Value => _monoVariable?.Value;
    }

    [Serializable]
    public class ValueRefProvider<TValue> : IValue<TValue>
    {
        public enum ProviderType
        {
            DirectRef,
            ParentMono, //已經有Instance了
            GlobalMonoInstance, //已經有Instance了
            Variable, //還不一定有。可能是null
        }

        [SerializeReferenceParentValidate] [SerializeField]
        private MonoBehaviour propertyParent;
        //從Parent拿
        //從Variable拿？


        [SerializeField] ProviderType providerType;

        public TValue _valueRef;
        [DropDownRef] public TValue _valueRefFromDropDown;
        public TValue Value => _valueRef;
    }


    //超級無敵複雜？
    [Serializable]
    public class VariableProviderFromMonoDescriptable : IVariableProvider
    {
        [SerializeReference] public IMonoDescriptableProvider _monoDescriptableProvider;

        //FIXME: 連tag都可能需要DI
        //FIXME: 任何資料都可能可以DI...VariableEntry
        public VariableTag _varTag;

        public AbstractMonoVariable Variable =>
            _monoDescriptableProvider.GetMonoDescriptable().GetVariable(_varTag);

        public TVariable GetMonoVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return Variable as TVariable;
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
        [Required] public MonoDescriptableTag monoDescriptableTag;
        [Required] public VariableTag varTag;

        [PreviewInInspector]
        public AbstractMonoVariable Variable
        {
            get
            {
                if (varTag == null && Application.isPlaying)
                {
                    Debug.LogError("Variable Tag is null", propertyParent);
                    return null;
                }

                var descriptable = propertyParent.GetGlobalInstance(monoDescriptableTag);
                if (descriptable == null) return null;
                return descriptable.GetVariable(varTag);
            }
        }

        public TVariable1 GetMonoVar<TVariable1>() where TVariable1 : AbstractMonoVariable
        {
            return Variable as TVariable1;
        }

        public TVariable GetMonoVar()
        {
            return Variable as TVariable;
        }
    }

    [Serializable]
    public class VariableProviderFromGlobalInstance : IVariableProvider //fixme
    {
        [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;

        //FIXME: tag需要更鬆一點？類似同個型別都吃？interface...MonoDescriptable... MonoUISelecting
        [Required] public MonoDescriptableTag monoDescriptableTag;
        [Required] public VariableTag varTag;

        [PreviewInInspector]
        public AbstractMonoVariable Variable
        {
            get
            {
                if (varTag == null && Application.isPlaying)
                {
                    Debug.LogError("Variable Tag is null", propertyParent);
                    return null;
                }

                var descriptable = propertyParent.GetGlobalInstance(monoDescriptableTag);
                if (descriptable == null) return null;
                return descriptable.GetVariable(varTag);
            }
        }

        public TVariable GetMonoVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return Variable as TVariable;
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
    [Serializable]
    public class VariableInOwner : IConfigVar, IVariableProvider
    {
        // [InlineEditor]
        // public VariableTag varTag; //這個assign也要被限定範圍？
        // // public object GetValue => varTag;
        //
        //Direct Ref, 不太好
        [Required] [DropDownRef] public AbstractMonoVariable _monoVariable;

        object IConfigVar.GetValue()
        {
            // throw new NotImplementedException();
            return _monoVariable.objectValue;
        }

        public AbstractMonoVariable Variable => _monoVariable;

        public TVariable GetMonoVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return _monoVariable as TVariable;
        }
    }
}