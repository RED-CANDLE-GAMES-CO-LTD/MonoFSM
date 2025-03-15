using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Core.DataProvider
{
    //各種data來源
    //監聽的模組要另外掛嗎？
    public abstract class AbstractFieldValueProvider : MonoBehaviour
    {
        //這個auto會太慢耶導致看的時候error?
        [Component(addAt = AddComponentAt.Same)] [Required] [Auto]
        protected AbstractVariableProviderRef _variableProviderRef;

        // [Obsolete]
        // [Required]
        // [BoxGroup("Get Value From a Variable")]
        // [SerializeReference]
        // [HideIf(nameof(_variableProviderRef))]
        // public IVariableProvider _variableProvider; //可能是mono, 也可能是數字而已

        [PreviewInInspector] [Auto] IDataChangedListener _dataChangedListener;
        protected abstract AbstractMonoVariable ListenToVariable { get; }
        [PreviewInInspector] [Auto] ITypeRestrict _typeRestrict;
        public abstract Object targetObject { get; }
        public abstract Type targetType { get; }
        [PreviewInInspector] [AutoParent] IIndexInjector _indexInjector;

        void UpdateView()
        {
            _dataChangedListener.OnDataChanged(targetObject);
        }

        private void Start()
        {
            //這個variable已經準備好了嗎？
            if (ListenToVariable)
                ListenToVariable.OnValueChangedRaw += UpdateView;
            else
            {
                Debug.LogError("ListenToVariable is null", this);
            }
        }

        private void OnDestroy()
        {
            if (ListenToVariable)
                ListenToVariable.OnValueChangedRaw -= UpdateView;
        }

        /// <summary>
        ///     從 targetObject 開始，依序根據 pathEntries 更新每一層的 parentType
        ///     若欄位為陣列，則下一層的 parentType 設為陣列元素的型別
        /// </summary>
        [OnInspectorGUI]
        // [Button("更新")]
        private void UpdateParentTypes()
        {
            if (_variableProviderRef == null)
                return;
            var currentType = targetObject ? targetObject.GetType() : targetType;
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
            //第一次是obj是DescriptableData
            if (obj == null)
                return "";
            var currentObj = obj;

            var i = 0;
            foreach (var entry in entries)
            {
                if (currentObj == null)
                {
                    Debug.LogError($"在 '{entry.fieldName}' 層級遇到 null", this);
                    return $"在 '{entry.fieldName}' 層級遇到 null";
                }
                else
                {
                    // Debug.Log($"在 '{entry.fieldName}' {i}層級的物件: {currentObj}", this);
                }

                //FIXME: 如果某個type被refactor的時候，serializedType記得東西會爛掉，要重新開Prefab儲存
                //FIXME: 這個prefab抓到的不一定會是對的耶... 除非是先拿到正確的sampleData
                //var type = entry._serializedType.RestrictType;
                //直接從 currentObj 獲取實際的 Type，而不依賴序列化的資料
                var type = currentObj.GetType();

                if (entry.fieldName == null)
                {
                    Debug.LogError("欄位名稱為空", this);
                    return "欄位名稱為空";
                }

                Func<object, object> getter = GetMemberGetter(type, entry.fieldName);
                if (getter != null)
                {
                    currentObj = getter(currentObj); //可能拿到陣列
                }
                else
                {
                    Debug.LogError($"在 {i}層 {type.Name} 中找不到名稱為 '{entry.fieldName}' 的欄位或屬性" + obj, this);
                    return $"在 {type.Name} 中找不到名稱為 '{entry.fieldName}' 的欄位或屬性";
                }

                // Debug.Log("CurrentObj1:" + currentObj, this);
                //如果是陣列，取得指定index的element value
                // if (entry.IsArray)
                // {
                if (currentObj is Array arr)
                {
                    if (entry.index < 0 || entry.index >= arr.Length)
                    {
                        Debug.LogError($"索引 {entry.index} 超出陣列 '{entry.fieldName}' 的範圍 (長度 {arr.Length})", this);
                        return $"索引 {entry.index} 超出陣列 '{entry.fieldName}' 的範圍 (長度 {arr.Length})";
                    }

                    currentObj = arr.GetValue(entry.index);
                    // Debug.Log("CurrentObj2:" + currentObj, this);
                }
                // else
                // {
                //     Debug.LogError($"欄位 '{entry.fieldName}' 預期為陣列，但實際上不是陣列", this);
                //     return $"欄位 '{entry.fieldName}' 預期為陣列，但實際上不是陣列";
                // }
                // }
                // }

                // 若此層的欄位是陣列，則利用 entry.index 存取指定的元素
                i++;
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
}