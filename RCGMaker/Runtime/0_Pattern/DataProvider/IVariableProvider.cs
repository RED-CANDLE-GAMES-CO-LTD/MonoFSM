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
using RCGUIBinder;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Core.DataProvider
{
    //這個好像是正解喔？封裝完只需要宣告一個field, assign一個tag就能拿到了
    //從parent的VariableOwner拿到Variable
    //VariableProviderInParent?
    public enum ProviderType
    {
        ParentMono, //已經有Instance了
        GlobalMonoInstance, //已經有Instance了
        Variable, //還不一定有。可能是null
    }

    [Serializable]
    public class MonoDescriptableProvider<TMonoDescriptable> : IMonoDescriptableProvider
        where TMonoDescriptable : class, IMonoDescriptable
    {
        [SerializeReferenceParentValidate] [SerializeField]
        private MonoBehaviour propertyParent;
        //從Parent拿
        //從Variable拿？


        public ProviderType providerType;

        //如果是parent就不需要這個了？
        [ShowIf("providerType", ProviderType.GlobalMonoInstance)] [SerializeField]
        MonoDescriptableTag monoDescriptableTag;

        [ShowIf("providerType", ProviderType.Variable)] [SerializeReference]
        public IVariableMonoDescriptableProvider variableProvider;

        [PreviewInInspector]
        public DescriptableData SampleData
        {
            get
            {
                switch (providerType)
                {
                    case ProviderType.Variable:
                        return variableProvider.SampleData;
                }

                var monoDescriptable = GetMonoDescriptable();
                if (monoDescriptable == null) return null;
                return monoDescriptable.Key?.SamepleData;
            }
        }

        [PreviewInInspector]
        public IMonoDescriptable GetMonoDescriptable()
        {
            if (propertyParent == null) return null;
            switch (providerType)
            {
                case ProviderType.ParentMono:
                    return propertyParent.GetComponentInParent<TMonoDescriptable>();
                case ProviderType.GlobalMonoInstance:
                    return propertyParent.GetMonoDescriptableInstance(monoDescriptableTag);
                case ProviderType.Variable:
                    return variableProvider?.GetVariableMonoDescriptable?.Value;
                default:
                    return propertyParent.GetComponentInParent<TMonoDescriptable>();
            }
            // return propertyParent.GetMonoDescriptableInstance(monoDescriptableTag);
        }

        [GUIColor(0.8f, 1.0f, 0.8f)]
        [PreviewInInspector]
        public TMonoDescriptable CurrentInstance => GetMonoDescriptable() as TMonoDescriptable;
    }

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

    //同個owner下的variable
    //FIXME: 壞處：沒有SampleData, 不能直接拿到property
    [Serializable]
    public class VariableProvider<T> : IVariableProvider
    {
        [SerializeReferenceParentValidate] [SerializeField]
        private MonoBehaviour propertyParent;

        private bool TypeCheckFail()
        {
            if (varTag == null) return false;
            return typeof(T).IsAssignableFrom(varTag._valueFilterType.RestrictType) == false;
        }

        //FIXME: dropdown validate? 多檢查parent的owner?
        [InfoBox("Tag Type is wrong", InfoMessageType.Error, nameof(TypeCheckFail))] [Required]
        public VariableTag varTag;

        private void OnGlobalMonoTagChange()
        {
            _runtimeCachedOwner = null;
        }

        [OnValueChanged(nameof(OnGlobalMonoTagChange))]
        public MonoDescriptableTag globalMonoTag; //空的話就是自己

        [PreviewInInspector] private Type variableValueType => typeof(T);
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

                if (propertyParent == null) return null;
                if (globalMonoTag != null)
                {
                    var globalDescriptable = propertyParent.GetMonoDescriptableInstance(globalMonoTag);
                    if (globalDescriptable == null) return null;
                    //FIXME: 
                    return globalDescriptable;
                }

                _runtimeCachedOwner = propertyParent.GetComponentInParent<VariableOwner>();
                return _runtimeCachedOwner;
            }
        }

        private VariableOwner _runtimeCachedOwner;

        [GUIColor(0.8f, 1.0f, 0.8f)]
        [PreviewInInspector]
        public AbstractMonoVariable GetMonoVariable
        {
            get
            {
                if (owner == null)
                {
                    if (Application.isPlaying)
                        Debug.LogError("Owner is null", propertyParent);
                    return null;
                }

                if (owner.VariableFolder == null)
                {
                    if (Application.isPlaying)
                        Debug.LogError("VariableFolder is null", propertyParent);
                    return null;
                }

                return owner.VariableFolder.GetVariable(varTag);
            }
        }

        // [ShowInInspector]
        // RCGVariableFolder GetFolder =>  owner?.VariableFolder;
        [PreviewInInspector] public T Value => GetMonoVariable == null ? default : GetMonoVariable.GetValue<T>();
    }

    public interface IVariableProvider
    {
        AbstractMonoVariable GetMonoVariable { get; }
    }

    public class VariableProviderFromParentEntity : IVariableProvider
    {
        [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;
        public VariableTag varTag;
        private MonoDescriptable parentDescriptable => propertyParent.GetComponentInParent<MonoDescriptable>();
        public AbstractMonoVariable GetMonoVariable => parentDescriptable.GetVariable(varTag);
    }

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