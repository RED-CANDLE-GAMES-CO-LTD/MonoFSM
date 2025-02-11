using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using RCGUIBinder;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace RCGMaker.Core.DataProvider
{
    // class MonoFieldTag
    // {
    //     
    // }
    // class MonoFieldEntry
    // {
    //     private MonoFieldTag fieldTagName;
    //     //   
    // }


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
        [ShowIf(nameof(IsArray))] [LabelText("Index")]
        public int index;

        // 父層型別，由外部更新（非序列化）
        // [NonSerialized] public Type parentType;
        public MySerializedType _serializedType;

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
                    bool isSupportedType = _supportedTypes != null && _supportedTypes.Contains(propType);

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

    public interface ITypeRestrict
    {
        public List<Type> SupportedTypes { get; }
    }

    public abstract class AbstractFieldValueProvider : MonoBehaviour
    {
        [PreviewInInspector] [Auto] ITypeRestrict _typeRestrict;
        public abstract UnityEngine.Object targetObject { get; }
        [PreviewInInspector] [AutoParent] IIndexInjector _indexInjector;

        private void Awake()
        {
            // UpdateParentTypes();
        }

        /// <summary>
        ///     從 targetObject 開始，依序根據 pathEntries 更新每一層的 parentType
        ///     若欄位為陣列，則下一層的 parentType 設為陣列元素的型別
        /// </summary>
        [OnInspectorGUI]
        // [Button("更新")]
        private void UpdateParentTypes()
        {
            var currentType = targetObject ? targetObject.GetType() : null;
            for (var i = 0; i < pathEntries.Count; i++)
            {
                pathEntries[i]._serializedType.SetType(currentType);
                // Debug.Log("currentType:" + currentType);
                // 若 parentType 為可序列化型別或 Unity Object，則不限制支援的型別
                // if (currentType.IsSerializable ||
                //     typeof(Object).IsAssignableFrom(currentType))
                // {
                //     Debug.Log("currentType.IsSerializable" + currentType.IsSerializable +
                //               "typeof(Object).IsAssignableFrom(currentType)" +
                //               typeof(Object).IsAssignableFrom(currentType));
                //     pathEntries[i]._supportedTypes = null;
                // }
                // else
                if (_typeRestrict != null)
                    pathEntries[i]._supportedTypes = _typeRestrict.SupportedTypes;

                //把index注入到pathEntries
                if (_indexInjector != null && pathEntries[i].IsArray)
                {
                    pathEntries[i].index = _indexInjector.Index;
                }

                if (currentType != null && !string.IsNullOrEmpty(pathEntries[i].fieldName))
                {
                    // 先嘗試 Field
                    // var field = currentType.GetField(pathEntries[i].fieldName,
                    //     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    // if (field != null)
                    // {
                    //     if (field.FieldType.IsArray)
                    //         // 若為陣列，下一層的型別為元素型別
                    //         currentType = field.FieldType.GetElementType();
                    //     else
                    //         currentType = field.FieldType;
                    //     continue;
                    // }

                    // 再嘗試 Property
                    var prop = currentType.GetProperty(pathEntries[i].fieldName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        if (prop.PropertyType.IsArray)
                            currentType = prop.PropertyType.GetElementType();
                        else
                            currentType = prop.PropertyType;
                        continue;
                    }

                    // 若都找不到，後續就無法推算
                    currentType = null;
                }
                else
                {
                    currentType = null;
                }
            }
        }

        //cache Type & field name to a function?

        /// <summary>
        ///     根據 pathEntries 依序利用反射從 obj 取得最終欄位值
        ///     支援若欄位為陣列時，根據 index 取得對應元素
        /// </summary>
        private object GetFieldValueFromPath(object obj, List<FieldPathEntry> entries)
        {
            if (obj == null)
                return "";
            var currentObj = obj;

            foreach (var entry in entries)
            {
                if (currentObj == null)
                {
                    Debug.LogError($"在 '{entry.fieldName}' 層級遇到 null", this);
                    return $"在 '{entry.fieldName}' 層級遇到 null";
                }

                var type = entry._serializedType.RestrictType;
                if (type == null)
                {
                    Debug.LogError("Type is null" + entry.fieldName + entry._serializedType.TypeName, this);
                    return "Type is null";
                }

                // var type = currentObj.GetType();
                if (entry.fieldName == null)
                {
                    Debug.LogError("欄位名稱為空", this);
                    return "欄位名稱為空";
                }


                // var field = type.GetField(entry.fieldName,
                //     BindingFlags.Public | BindingFlags.Instance);
                // if (field != null)
                // {
                //     currentObj = field.GetValue(currentObj);
                // }
                // else
                // {
                // var prop = type.GetProperty(entry.fieldName,
                //     BindingFlags.Public | BindingFlags.Instance);
                // if (prop != null)
                //     currentObj = prop.GetValue(currentObj, null);
                // else
                //     return $"在 {type.Name} 中找不到名稱為 '{entry.fieldName}' 的欄位或屬性";

                Func<object, object> getter = GetMemberGetter(type, entry.fieldName);
                if (getter != null)
                {
                    currentObj = getter(currentObj);
                }
                else
                {
                    Debug.LogError($"在 {type.Name} 中找不到名稱為 '{entry.fieldName}' 的欄位或屬性", this);
                    return $"在 {type.Name} 中找不到名稱為 '{entry.fieldName}' 的欄位或屬性";
                }

                //如果是陣列，取得指定index的element value
                if (entry.IsArray)
                {
                    if (currentObj is Array arr)
                    {
                        if (entry.index < 0 || entry.index >= arr.Length)
                        {
                            Debug.LogError($"索引 {entry.index} 超出陣列 '{entry.fieldName}' 的範圍 (長度 {arr.Length})", this);
                            return $"索引 {entry.index} 超出陣列 '{entry.fieldName}' 的範圍 (長度 {arr.Length})";
                        }

                        currentObj = arr.GetValue(entry.index);
                    }
                    else
                    {
                        Debug.LogError($"欄位 '{entry.fieldName}' 預期為陣列，但實際上不是陣列", this);
                        return $"欄位 '{entry.fieldName}' 預期為陣列，但實際上不是陣列";
                    }
                }
                // }

                // 若此層的欄位是陣列，則利用 entry.index 存取指定的元素
            }

            return currentObj;
        }

        // void GetFieldValue()
        // {
        //     // 每次按下前先更新所有層級的 parentType
        //     UpdateParentTypes();
        //     var resultValue = GetFieldValueFromPath(targetObject, pathEntries);
        //     Debug.Log("結果：" + (resultValue != null ? resultValue.ToString() : "null"));
        // }

        [OnValueChanged("GetFieldValue")] [ListDrawerSettings(ShowFoldout = false)] [BoxGroup("Field")]
        public List<FieldPathEntry> pathEntries;


        [Button("Runtime 取得欄位值")]
        public object GetFieldValue()
        {
            // 每次按下前先更新所有層級的 parentType
            var resultValue = GetFieldValueFromPath(targetObject, pathEntries);
            // Debug.Log("結果：" + (resultValue != null ? resultValue.ToString() : "null"));
            return resultValue;
        }

        [Button("Editor 取得欄位值")]
        public object EditorGetFieldValue()
        {
            UpdateParentTypes();
            var resultValue = GetFieldValueFromPath(targetObject, pathEntries);
            return resultValue;
        }

        [Button("新增層級")]
        private void AddLevel()
        {
            pathEntries.Add(new FieldPathEntry());
            GetFieldValue();
        }

        [Button("刪除最後一層")]
        private void RemoveLastLevel()
        {
            if (pathEntries.Count > 0)
                pathEntries.RemoveAt(pathEntries.Count - 1);
        }


        #region 快取 Reflection Getter

        // 使用 ValueTuple 當作 Dictionary Key：Type 與成員名稱
        private static Dictionary<(System.Type, string), Func<object, object>> getterCache =
            new Dictionary<(System.Type, string), Func<object, object>>();

        /// <summary>
        /// 取得指定型別與成員名稱的 getter delegate。
        /// 如果已快取則直接回傳，否則建立一個並快取起來。
        /// </summary>
        private static Func<object, object> GetMemberGetter(System.Type type, string memberName)
        {
            var key = (type, memberName);
            if (getterCache.TryGetValue(key, out var getter))
            {
                return getter;
            }

            // 嘗試從 Field 取得
            // FieldInfo field = type.GetField(memberName,
            //     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // if (field != null)
            // {
            //     getter = CreateFieldGetter(field);
            //     getterCache[key] = getter;
            //     return getter;
            // }

            // 嘗試從 Property 取得
            // var field = type.GetField(entry.fieldName,
            //     BindingFlags.Public | BindingFlags.Instance);
            // if (field != null)
            // {
            //     currentObj = field.GetValue(currentObj);
            // }
            // else
            // {
            // var prop = type.GetProperty(entry.fieldName,
            //     BindingFlags.Public | BindingFlags.Instance);
            // if (prop != null)
            //     currentObj = prop.GetValue(currentObj, null);
            // else
            //     return $"在 {type.Name} 中找不到名稱為 '{entry.fieldName}' 的欄位或屬性";
            PropertyInfo prop = type.GetProperty(memberName,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                getter = CreatePropertyGetter(prop);
                getterCache[key] = getter;
                return getter;
            }

            return null;
        }

        /// <summary>
        /// 使用 Expression 建立 field 的 getter delegate
        /// </summary>
        private static Func<object, object> CreateFieldGetter(FieldInfo field)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instanceParam, field.DeclaringType);
            var fieldAccess = Expression.Field(castInstance, field);
            var convertResult = Expression.Convert(fieldAccess, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);
            return lambda.Compile();
        }

        /// <summary>
        /// 使用 Expression 建立 property 的 getter delegate
        /// </summary>
        private static Func<object, object> CreatePropertyGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instanceParam, property.DeclaringType);
            var propertyAccess = Expression.Property(castInstance, property);
            var convertResult = Expression.Convert(propertyAccess, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);
            return lambda.Compile();
        }

        #endregion
    }

    [Serializable]
    public class SODataObjectFieldProvider : AbstractFieldValueProvider
    {
        // [BoxGroup("Instance")] [PreviewInInspector] [AutoParent]
        // public IDescriptableProvider _descriptableProvider;

        //1. 從Parent直接拿到MonoDescriptable
        //2. 從Variable拿到MonoDescriptable
        //FIXME: 從某個VariableDescriptableData拿到會不會更好？
        //從某個VariableMonoDescriptable拿到Data

        // [BoxGroup("Instance")] public VariableMonoDescriptableProvider _monoDescriptableProvider;
        [PropertyOrder(-1)] [BoxGroup("Instance")]
        public MonoDescriptableProvider<IMonoDescriptable> _descriptableProvider;

        [PropertyOrder(-1)]
        [BoxGroup("Instance")]
        [PreviewInInspector]
        private IDescriptableData dataInstance =>
            _descriptableProvider?.CurrentInstance?.Descriptable;

        //不一定需要instance, 有type就好了？
        [PropertyOrder(-1)]
        public override Object targetObject
        {
            get
            {
                if (Application.isPlaying == false) //FIXME: 如果有也可以用descriptable?
                    return _descriptableProvider?.SampleData;
                else
                    return _descriptableProvider?.CurrentInstance?.Descriptable as Object;
                //一定要sample data?
            }
        }

        // private Type dataType => _monoDescriptableProvider.GetVariable.FinalDataType; //FIXME: 還是錯...
        //Data Object Field Provider
    }
}