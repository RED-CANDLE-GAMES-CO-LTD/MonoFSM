using System;
using System.Collections.Generic;
using System.Reflection;
using RCGMaker.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.DataProvider
{
    /// <summary>
    ///     表示欄位路徑上單一層級的資料結構
    /// </summary>
    [Serializable]
    public class FieldPathEntry
    {
        [ValueDropdown(nameof(GetFieldOptions))]
        public string fieldName;

        // 當對應的欄位為陣列時，才會顯示 index 欄位
        //FIXME: 不可以編輯？用index注入？
        [PreviewInInspector] [ShowIf(nameof(IsArray))] [LabelText("Index")]
        public int index; //injected index;

        // 父層型別，由外部更新（非序列化）
        // [NonSerialized] public Type parentType;
        public MySerializedType _serializedType; //FIXME: refactor時會爛掉...有點麻煩

        Type parentType => _serializedType.RestrictType;

        // 支援的型別清單
        //restrict to types?
        [PreviewInInspector] public List<Type> _supportedTypes;

        /// <summary>
        ///     動態回傳 parentType 中所有可存取的欄位與屬性名稱
        /// </summary>
        public IEnumerable<ValueDropdownItem<string>> GetFieldOptions()
        {
            var options = new List<ValueDropdownItem<string>>();
            var parentType = _serializedType.RestrictType;
            if (parentType != null)
            {
                // 取得所有 Field
                // var fields = parentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                // foreach (var field in fields)
                //     options.Add(new ValueDropdownItem<string>(field.Name + ":" + field.FieldType, field.Name));
                // 取得所有 Property（可讀取的）
                var properties =
                    parentType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                if (parentType == null || string.IsNullOrEmpty(fieldName))
                    return false;

                // var field = parentType.GetField(fieldName,
                //     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                // if (field != null) return field.FieldType.IsArray;
                var prop = parentType.GetProperty(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null) return prop.PropertyType.IsArray;
                return false;
            }
        }
    }
}