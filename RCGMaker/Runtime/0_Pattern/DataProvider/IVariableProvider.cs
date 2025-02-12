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
        VariableMonoDescriptable GetVariableMonoDescriptable { get; }
        DescriptableData SampleData { get; }
    }


    [Serializable]
    public class VariableMonoDescriptableProvider : VariableProvider<MonoDescriptable>,
        IVariableMonoDescriptableProvider
    {
        //目的：是要拿到Variable, 還是要拿到MonoDescriptable?

        public VariableMonoDescriptable GetVariableMonoDescriptable => GetMonoVariable as VariableMonoDescriptable;

        [PreviewInInspector]
        public DescriptableData SampleData =>
            GetVariableMonoDescriptable?.SampleData;
    }

    //這個好像是正解喔？封裝完只需要宣告一個field, assign一個tag就能拿到了
    //從parent的VariableOwner拿到Variable
    //VariableProviderInParent?
    //同個owner下的variable
    //FIXME: 壞處：沒有SampleData, 不能直接拿到property
    [Serializable]
    public class VariableProvider<TVariableType> : IVariableProvider
    {
        [SerializeReferenceParentValidate] [SerializeField]
        private MonoBehaviour propertyParent;

        private MonoBehaviour CurrentTarget
        {
            get
            {
                if (_currentTarget == null)
                    return propertyParent;
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
            return GetMonoVariable;
        }

        public TVariableType GetValueFrom(MonoBehaviour target)
        {
            _currentTarget = target;
            FetchOwner(target);
            return Value;
        }

        private bool TypeCheckFail()
        {
            if (varTag == null) return false;
            return typeof(TVariableType).IsAssignableFrom(varTag._valueFilterType.RestrictType) == false;
        }

        //FIXME: dropdown validate? 多檢查parent的owner?
        [InfoBox("Tag Type is wrong", InfoMessageType.Error, nameof(TypeCheckFail))] [Required]
        public VariableTag varTag;

        private void OnGlobalMonoTagChange()
        {
            _runtimeCachedOwner = null;
        }

        //FIXME: filter tags in parent? hide if parent is null? bool? enum?
        IEnumerable<ValueDropdownItem<MonoDescriptableTag>> GetGlobalMonoTags()
        {
            var parents = CurrentTarget.GetComponentsInParent<MonoDescriptable>();
            var tags = new List<ValueDropdownItem<MonoDescriptableTag>>();
            foreach (var parent in parents)
            {
                tags.Add(new ValueDropdownItem<MonoDescriptableTag>(parent.Tag.name, parent.Tag));
            }

            return tags;
        }

        [ValueDropdown(nameof(GetGlobalMonoTags))] [OnValueChanged(nameof(OnGlobalMonoTagChange))]
        public MonoDescriptableTag globalMonoTag; //空的話就是自己

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
            if (target == null) return null;
            if (globalMonoTag != null)
            {
                var globalDescriptable = target.GetMonoDescriptableInstance(globalMonoTag);
                if (globalDescriptable == null) return null;
                //FIXME: 
                return globalDescriptable;
            }

            _runtimeCachedOwner = target.GetComponentInParent<VariableOwner>();
            return _runtimeCachedOwner;
            // return _runtimeCachedOwner;
        }

        private VariableOwner _runtimeCachedOwner;

        public void SetValue(TVariableType value, MonoBehaviour byWho)
        {
            GetMonoVariable.SetValue(value, byWho);
        }

        [GUIColor(0.8f, 1.0f, 0.8f)]
        [PreviewInInspector]
        public AbstractMonoVariable GetMonoVariable
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

                return owner.VariableFolder.GetVariable(varTag);
            }
        }

        // [ShowInInspector]
        // RCGVariableFolder GetFolder =>  owner?.VariableFolder;
        [PreviewInInspector]
        public TVariableType Value => GetMonoVariable == null ? default : GetMonoVariable.GetValue<TVariableType>();
    }

    public interface IVariableProvider
    {
        AbstractMonoVariable GetMonoVariable { get; } //還是其實這個也可以？
        // AbstractMonoVariable GetMonoVariableFrom(MonoBehaviour target);
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

    public class VariableProviderFromGlobalInstance : IVariableProvider
    {
        [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;

        public MonoDescriptableTag monoDescriptableTag;
        public VariableTag varTag;

        [PreviewInInspector]
        public AbstractMonoVariable GetMonoVariable
        {
            get
            {
                var descriptable = propertyParent.GetMonoDescriptableInstance(monoDescriptableTag);
                if (descriptable == null) return null;
                return descriptable.GetVariable(varTag);
            }
        }
    }

    /// <summary>
    ///     同個owner下的variable
    /// </summary>
    [Serializable]
    public class VariableProviderByTag : IConfigVar, IVariableProvider
    {
        [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;
        public VariableTag varTag; //動態拿

        object IConfigVar.GetValue()
        {
            return GetMonoVariable;
        }

        [PreviewInInspector]
        public AbstractMonoVariable GetMonoVariable
        {
            get
            {
                if (_cachedMonoVariable == null) BindCache();
                return _cachedMonoVariable;
            }
        }

        private AbstractMonoVariable _cachedMonoVariable;

        //不能用autoParent了齁... 還是連nested class都可以爬出來，或是掛[AutoClassBinder]
        private void BindCache()
        {
            if (propertyParent == null) return;
            var owner = propertyParent.GetComponentInParent<IVariableOwner>();
            if (owner == null) return; //會一直叫...怎麼辦... 用getter不好，應該是要從Editor/Odin那邊叫
            _cachedMonoVariable = owner.VariableFolder.GetVariable(varTag);
        }
    }

    [Serializable]
    public class VariableInOwner : IConfigVar, IVariableProvider
    {
        // [InlineEditor]
        // public VariableTag varTag; //這個assign也要被限定範圍？
        // // public object GetValue => varTag;
        //

        [FormerlySerializedAs("_variable")] [DropDownRef]
        public AbstractMonoVariable _monoVariable;

        object IConfigVar.GetValue()
        {
            // throw new NotImplementedException();
            return _monoVariable.objectValue;
        }

        public AbstractMonoVariable GetMonoVariable => _monoVariable;
    }
}