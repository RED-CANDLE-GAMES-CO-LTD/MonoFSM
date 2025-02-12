using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Attributes;
using RCGMaker.Runtime.Item_BuildSystem;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using RCGMaker.Runtime.Mono;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.DataProvider
{
    public interface IMonoDescriptableProvider
    {
        public IMonoDescriptable GetMonoDescriptable();
    }

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

    //可以refactor
    // [MovedFrom(false, null, "rcg.rcgmakercore.Runtime", "MonoDescriptableSource")]
    [Serializable]
    public class MonoDescriptableConfig : IConfigVar, IMonoDescriptableProvider
    {
        // [InlineEditor]
        [DropDownRef] public MonoDescriptable _monoDescriptable;

        object IConfigVar.GetValue()
        {
            return _monoDescriptable;
        }

        public IMonoDescriptable GetMonoDescriptable()
        {
            return _monoDescriptable;
        }
    }

    [Serializable]
    public class MonoDescriptableFromTag : IConfigVar, IMonoDescriptableProvider
    {
        [SerializeReferenceParentValidate] public MonoBehaviour propertyParent;
        public MonoDescriptableTag monoDescriptableTag;

        object IConfigVar.GetValue()
        {
            return GetMonoDescriptable();
        }

        [GUIColor(0.8f, 1.0f, 0.8f)]
        [PreviewInInspector]
        IMonoDescriptable currentInstance => GetMonoDescriptable();

        public IMonoDescriptable GetMonoDescriptable()
        {
            return propertyParent.GetMonoDescriptableInstance(monoDescriptableTag);
        }
    }
}