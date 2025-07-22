using System;
using MonoFSM.Core.Utilities;
using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    public class ValueRef : PropertyOfTypeProvider, IValueProvider
    {
        [DropDownRef] [SerializeField] private PropertyOfTypeProvider _valueProvider;

        public T1 Get<T1>()
        {
            return (T1)ReflectionUtility.GetFieldValueFromPath(_valueProvider, _pathEntries, gameObject);
            // return _valueProvider.Get<T1>();
        }

        public override Type ValueType => lastPathEntryType;
        public override string Description => _valueProvider?.Description + "." + PropertyPath; //最後一段會重複？
        protected override string DescriptionTag => "ref";
        public override Type GetObjectType => _valueProvider?.ValueType;
    }
}