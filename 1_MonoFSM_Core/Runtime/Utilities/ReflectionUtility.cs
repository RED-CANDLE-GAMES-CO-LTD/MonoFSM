using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MonoFSM.Core.DataProvider;
using MonoFSM.Variable.FieldReference;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.Utilities
{
    /// <summary>
    /// 反射相關的工具方法，提供快取機制和效能最佳化
    /// </summary>
    public static class ReflectionUtility
    {
        #region Reflection Caching

        // 使用 ValueTuple 當作 Dictionary Key：Type 與成員名稱
        private static readonly Dictionary<(Type, string), Func<object, object>> GetterCache = new();
        
        // 快取成員型別資訊，避免重複反射查找
        private static readonly Dictionary<(Type, string), Type> MemberTypeCache = new();

        /// <summary>
        /// 取得指定型別與成員名稱的 getter delegate。
        /// 如果已快取則直接回傳，否則建立一個並快取起來。
        /// 使用 RefactorSafeNameResolver 來查找成員（支援舊名稱）。
        /// </summary>
        public static Func<object, object> GetMemberGetter(Type type, string memberName)
        {
            var key = (type, memberName);
            if (GetterCache.TryGetValue(key, out var getter)) return getter;

            // 使用 RefactorSafeNameResolver 查找成員（支援舊名稱）
            var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(type, memberName);

            if (member is PropertyInfo prop)
            {
                getter = CreatePropertyGetter(prop);
                GetterCache[key] = getter;
                return getter;
            }

            if (member is FieldInfo field)
            {
                getter = CreateFieldGetter(field);
                GetterCache[key] = getter;
                return getter;
            }

            return null;
        }

        /// <summary>
        /// 使用 Expression 建立 field 的 getter delegate
        /// </summary>
        public static Func<object, object> CreateFieldGetter(FieldInfo field)
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
        public static Func<object, object> CreatePropertyGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instanceParam, property.DeclaringType);
            var propertyAccess = Expression.Property(castInstance, property);
            var convertResult = Expression.Convert(propertyAccess, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);
            return lambda.Compile();
        }

        /// <summary>
        /// 取得指定型別與成員名稱的成員型別，支援 field 和 property。
        /// 如果已快取則直接回傳，否則查找並快取結果。
        /// </summary>
        /// <param name="parentType">父型別</param>
        /// <param name="memberName">成員名稱</param>
        /// <returns>成員型別，如果找不到則回傳 null</returns>
        public static Type GetMemberType(Type parentType, string memberName)
        {
            if (parentType == null || string.IsNullOrEmpty(memberName))
                return null;

            var key = (parentType, memberName);
            if (MemberTypeCache.TryGetValue(key, out var cachedType))
                return cachedType;

            // 使用 RefactorSafeNameResolver 查找成員（支援舊名稱）
            var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(parentType, memberName);

            Type memberType = null;
            
            if (member is PropertyInfo prop)
            {
                memberType = prop.PropertyType;
            }
            else if (member is FieldInfo field)
            {
                memberType = field.FieldType;
            }
            else
            {
                // 如果 RefactorSafeNameResolver 找不到，嘗試直接查找
                var directProp = parentType.GetProperty(memberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (directProp != null)
                {
                    memberType = directProp.PropertyType;
                }
                else
                {
                    var directField = parentType.GetField(memberName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (directField != null)
                    {
                        memberType = directField.FieldType;
                    }
                }
            }

            // 快取結果（即使是 null 也要快取，避免重複查找）
            MemberTypeCache[key] = memberType;
            return memberType;
        }

        /// <summary>
        /// 清除反射快取
        /// </summary>
        public static void ClearCache()
        {
            GetterCache.Clear();
            MemberTypeCache.Clear();
        }

        /// <summary>
        /// 取得快取中的項目數量
        /// </summary>
        public static int CacheCount => GetterCache.Count + MemberTypeCache.Count;

        #endregion

        #region Type Conversion Utilities

        /// <summary>
        /// 檢查是否可以將一個型別轉換為另一個型別
        /// </summary>
        public static bool CanConvertType(Type from, Type to)
        {
            try
            {
                // 嘗試是否可以用 Convert.ChangeType 轉換
                if (to.IsAssignableFrom(from)) return true;
                if (to == typeof(string)) return true; // 大部分型別都可以轉成字串
                
                // 檢查數值型別轉換
                var typeCode1 = Type.GetTypeCode(from);
                var typeCode2 = Type.GetTypeCode(to);
                
                return typeCode1 != TypeCode.Object && typeCode2 != TypeCode.Object;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 嘗試將物件轉換為指定型別
        /// </summary>
        public static bool TryConvertValue<T>(object value, out T result)
        {
            result = default;
            
            if (value == null)
                return false;

            if (value is T directValue)
            {
                result = directValue;
                return true;
            }

            try
            {
                result = (T)Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Field Path Operations

        /// <summary>
        /// 根據 pathEntries 依序利用反射從 obj 取得最終欄位值
        /// 支援若欄位為陣列時，根據 index 取得對應元素
        /// </summary>
        /// <param name="obj">起始物件</param>
        /// <param name="entries">欄位路徑項目</param>
        /// <param name="logTarget">用於 Debug.Log 的目標物件</param>
        /// <returns>最終欄位值</returns>
        /// FIXME: 這個會有gc?
        /// FIXME: 吃<T>?
        public static object GetFieldValueFromPath(object obj, List<FieldPathEntry> entries, Object logTarget = null)
        {
            if (obj == null)
                return null;
            var currentObj = obj;

            var i = 0;
            foreach (var entry in entries)
            {
                if (currentObj == null)
                {
                    Debug.LogError($"在 '{entry._propertyName}' 層級遇到 null", logTarget);
                    return $"在 '{entry._propertyName}' 層級遇到 null";
                }

                // 直接從 currentObj 獲取實際的 Type，而不依賴序列化的資料
                var type = currentObj.GetType();

                if (entry._propertyName == null) return "欄位名稱為空";

                var getter = GetMemberGetter(type, entry._propertyName);

                // 檢查欄位是否已重命名，如果是則更新 entry.fieldName
                var foundMember = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(type, entry._propertyName);
                if (foundMember != null && foundMember.Name != entry._propertyName)
                {
                    Debug.Log($"欄位 '{entry._propertyName}' 已重命名為 '{foundMember.Name}'，正在更新參考", logTarget);
                    entry._propertyName = foundMember.Name;
                }

                if (getter != null)
                {
                    currentObj = getter(currentObj); // 可能拿到陣列
                }
                else
                {
                    Debug.LogError($"在 {i}層 {type.Name} 中找不到名稱為 '{entry._propertyName}' 的欄位或屬性" + obj, logTarget);
                    return $"在 {type.Name} 中找不到名稱為 '{entry._propertyName}' 的欄位或屬性";
                }

                // 如果是陣列，取得指定index的element value
                if (currentObj is Array arr)
                {
                    if (entry.index < 0 || entry.index >= arr.Length)
                    {
                        Debug.LogError($"索引 {entry.index} 超出陣列 '{entry._propertyName}' 的範圍 (長度 {arr.Length})",
                            logTarget);
                        return $"索引 {entry.index} 超出陣列 '{entry._propertyName}' 的範圍 (長度 {arr.Length})";
                    }

                    currentObj = arr.GetValue(entry.index);
                }

                if (currentObj == null) //半途遇到 null
                    if (entry._canBeNull)
                        return null; // 如果允許為 null，則直接返回 null
                i++;
                entry._tempCurrentObject = currentObj; // 用於 Unity 編輯器的預覽
            }

            
            return currentObj;
        }

        /// <summary>
        /// 更新 pathEntries 中每一層的型別資訊
        /// </summary>
        /// <param name="pathEntries">欄位路徑項目</param>
        /// <param name="startType">起始型別</param>
        /// <param name="supportedTypes">支援的型別清單</param>
        /// <param name="indexInjector">索引注入器</param>
        public static void UpdatePathEntryTypes(List<FieldPathEntry> pathEntries, Type startType, 
            List<Type> supportedTypes = null, IIndexInjector indexInjector = null)
        {
            if (pathEntries == null) return;

            var currentType = startType;

            for (var i = 0; i < pathEntries.Count; i++)
            {
                pathEntries[i]._serializedType.SetType(currentType);
                if (supportedTypes != null)
                    pathEntries[i]._supportedTypes = supportedTypes;

                // 把index注入到pathEntries
                if (indexInjector != null && pathEntries[i].IsArray)
                    pathEntries[i].index = indexInjector.Index;

                if (currentType != null && !string.IsNullOrEmpty(pathEntries[i]._propertyName))
                {
                    // 嘗試 Property
                    var prop = currentType.GetProperty(pathEntries[i]._propertyName,
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

        /// <summary>
        /// 檢查欄位路徑的最終型別是否與目標型別相容
        /// </summary>
        /// <param name="obj">起始物件</param>
        /// <param name="pathEntries">欄位路徑項目</param>
        /// <param name="targetType">目標型別</param>
        /// <returns>是否相容</returns>
        public static bool IsFieldPathTypeCompatible(Object obj, List<FieldPathEntry> pathEntries, Type targetType)
        {
            if (pathEntries == null || pathEntries.Count == 0) return true;
            if (obj == null) return false;

            try
            {
                var result = GetFieldValueFromPath(obj, pathEntries);
                if (result == null) return false;

                var resultType = result.GetType();
                Debug.Log($"最終欄位值型別：{resultType}, 目標型別：{targetType}", obj);
                return targetType.IsAssignableFrom(resultType) ||
                       CanConvertType(resultType, targetType);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}