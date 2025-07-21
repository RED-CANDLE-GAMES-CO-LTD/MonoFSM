using System;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Utilities;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.DataProvider
{
    /// <summary>
    ///     表示欄位路徑上單一層級的資料結構
    /// </summary>
    [Serializable]
    public class FieldPathEntry
    {
        public void SetSerializedType(Type type)
        {
            _serializedType = new MySerializedType<Object>
            {
                RestrictType = type
            };
        }

        // 父層型別，由外部更新（非序列化）
        // [NonSerialized] public Type parentType;
        [InlineProperty(LabelWidth = 60)] public MySerializedType<Object> _serializedType; //FIXME: refactor時會爛掉...有點麻煩

#if UNITY_EDITOR
        [NonSerialized] [PreviewInDebugMode] public object _tempCurrentObject;
#endif
        
        public Type GetPropertyType()
        {
            if (string.IsNullOrEmpty(_propertyName)) return null;
            
            // 使用 ReflectionUtility 的快取機制來提高效率
            var memberType = ReflectionUtility.GetMemberType(parentType, _propertyName);

            if (IsArray && memberType is { IsArray: true })
                return memberType.GetElementType();

            return memberType;
        }

        [FormerlySerializedAs("fieldName")] [ValueDropdown(nameof(GetFieldOptions))]
        public string _propertyName;

        public string PropertyPath
        {
            get
            {
                if (string.IsNullOrEmpty(_propertyName))
                    return string.Empty;

                // 如果是陣列，則加上索引
                if (IsArray)
                    return $"{_propertyName}[{index}]";

                // 否則只返回欄位名稱
                return _propertyName;
            }
        }

        // 當對應的欄位為陣列時，才會顯示 index 欄位
        //FIXME: 不可以編輯？用index注入？
        // [PreviewInInspector]
        [ShowIf(nameof(IsArray))] [LabelText("Index")]
        public int index; //injected index;

        private Type parentType => _serializedType.RestrictType; //為什麼叫parentType？
        

        // 支援的型別清單
        //restrict to types?
        //FIXME: editor only?
        [ShowInDebugMode]
        [PreviewInInspector] public List<Type> _supportedTypes;

        /// <summary>
        ///     動態回傳 parentType 中所有可存取的欄位與屬性名稱
        /// </summary>
        public IEnumerable<ValueDropdownItem<string>> GetFieldOptions()
        {
            var options = new List<ValueDropdownItem<string>>();
            var pType = _serializedType.RestrictType;
            // Debug.Log("GetFieldOptions parentType:" + pType);
            if (pType != null)
            {
                // 取得所有 Field
                // var fields = parentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                // foreach (var field in fields)
                //     options.Add(new ValueDropdownItem<string>(field.Name + ":" + field.FieldType, field.Name));
                // 取得所有 Property（可讀取的）
                var properties =
                    pType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    if (!prop.CanRead) continue;

                    var propType = prop.PropertyType;
                    var isSupportedType = _supportedTypes == null ? true : _supportedTypes.Contains(propType);

                    // || typeof(DescriptableData).IsAssignableFrom(propType)
                    //propType.IsSerializable ||
                    if (propType.IsArray || //好像管nested class就好了？還是array?
                        isSupportedType)
                    {
                        // Debug.Log("prop.Name:" + prop.Name + "propType:" + propType + "propType.IsSerializable" +
                        //           propType.IsSerializable);
                        options.Add(new ValueDropdownItem<string>($"{prop.Name}:{propType}", prop.Name));
                    }
                }
            }

            return options;
        }

        /// <summary>
        ///     判斷 parentType 中選擇的欄位是否為陣列
        /// </summary>
        public bool IsArray
        {
            get
            {
                if (parentType == null || string.IsNullOrEmpty(_propertyName))
                    return false;

                // 使用 ReflectionUtility 的快取機制來提高效率
                var memberType = ReflectionUtility.GetMemberType(parentType, _propertyName);
                return memberType?.IsArray ?? false;
            }
        }

        public bool _canBeNull;
    }
}