using System;
using MonoFSM.Core.Utilities;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    
    public class ValueRef : PropertyOfTypeProvider, IValueProvider
    {
        // [ShowDrawerChain]
        [DropDownRef] [SerializeField] private PropertyOfTypeProvider _valueProvider;

        public override T1 Get<T1>()
        {
            var v = ReflectionUtility.GetFieldValueFromPath(_valueProvider.ValueRaw, _pathEntries, gameObject);
            if (v == null)
                // Debug.LogWarning($"ValueRef: Value is null for path '{PropertyPath}'", this);
                return default;

            //string想拿int怎麼辦...
            if (v is T1 value)
            {
                return value;
            }
            else
            {
                //三小也太醜XDDD
                if (typeof(T1) == typeof(string))
                {
                    var str = v.ToString();

                    // Debug.Log($"ValueRef: Converting value to string:{str}, {str.GetType()} -> {typeof(T1)}", this);
                    return (T1)(object)str; //如果是string的話，直接轉成string
                }


                Debug.LogError($"ValueRef: Value type mismatch. Expected {typeof(T1)}, but got {v?.GetType()}", this);
                return default;
            }
            // return _valueProvider.Get<T1>();
        }

        public override Type ValueType => lastPathEntryType;
        public override string Description => _valueProvider?.Description + "." + PropertyPath; //最後一段會重複？
        protected override string DescriptionTag => "ref";
        public override Type GetObjectType => _valueProvider?.ValueType;
    }
}