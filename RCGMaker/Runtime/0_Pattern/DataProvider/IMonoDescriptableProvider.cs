using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Attributes;
using RCGMaker.Runtime.Item_BuildSystem;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.DataProvider
{
    public interface IMonoDescriptableProvider
    {
        public IMonoDescriptable GetMonoDescriptable();
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