using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MonoFSM.Core.DataProvider;
using MonoFSM.Variable;
using MonoFSM.Variable.FieldReference;
using UnityEngine;
using UnityEngine.Profiling;
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
        private static readonly Dictionary<(Type, string), Func<object, object>> GetterCache =
            new();

        // Specialized caches for common value types to avoid boxing
        private static readonly Dictionary<(Type, string), Func<object, int>> IntGetterCache =
            new();

        private static readonly Dictionary<(Type, string), Func<object, float>> FloatGetterCache =
            new();

        private static readonly Dictionary<(Type, string), Func<object, bool>> BoolGetterCache =
            new();

        private static readonly Dictionary<(Type, string), Func<object, double>> DoubleGetterCache =
            new();

        private static readonly Dictionary<(Type, string), Func<object, long>> LongGetterCache =
            new();

        private static readonly Dictionary<(Type, string), Func<object, string>> StringGetterCache =
            new();

        // 快取成員型別資訊，避免重複反射查找
        private static readonly Dictionary<(Type, string), Type> MemberTypeCache = new();

        /// <summary>
        /// 取得指定型別與成員名稱的 getter delegate。
        /// 如果已快取則直接回傳，否則建立一個並快取起來。
        /// 使用 RefactorSafeNameResolver 來查找成員（支援舊名稱）。
        /// </summary>
        public static Func<object, object> GetMemberGetter(Type type, string memberName)
        {
            Profiler.BeginSample("GetMemberGetter Tuple Key");
            var key = (type, memberName);
            Profiler.EndSample();
            if (GetterCache.TryGetValue(key, out var getter))
            {
                // Debug.Log("GetterCache member success: " + memberName);
                return getter;
            }

            // const BindingFlags flags =
            //     BindingFlags.Public | BindingFlags.NonPublic |
            //     BindingFlags.Instance;
            //
            // // 1. 先嘗試直接用當前名稱查找
            // MemberInfo member = type.GetProperty(memberName, flags) as MemberInfo ??
            //                     type.GetField(memberName, flags);
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

            //查不到的話有問題吧？
            Debug.LogError("GetMemberGetter not found type:" + type + " member:" + memberName);
            return null;
        }

        static bool IsPrimitiveType(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
        }

        /// <summary>
        /// 直接從快取中取得成員值，避免先取得函式再呼叫的步驟
        /// </summary>
        public static (T value, string error) GetValueFromCacheMemberGetter<T>(
            object instance,
            Type type,
            string memberName
        )
        {
            if (instance == null)
                return (default, "instance is null");

            Profiler.BeginSample("GetValueFromCacheMemberGetter Tuple Key");
            var key = (type, memberName);
            Profiler.EndSample();

            // 根據類型使用對應的緩存
            if (typeof(T) == typeof(int))
            {
                if (IntGetterCache.TryGetValue(key, out var intGetter))
                {
                    try
                    {
                        var result = intGetter(instance);
                        return ((T)(object)result, null);
                    }
                    catch (Exception e)
                    {
                        return (default, $"Error calling int getter: {e.Message}");
                    }
                }

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newIntGetter = CreateSpecializedGetter<int>(member);
                    if (newIntGetter != null)
                    {
                        IntGetterCache[key] = newIntGetter;
                        try
                        {
                            var result = newIntGetter(instance);
                            return ((T)(object)result, null);
                        }
                        catch (Exception e)
                        {
                            return (default, $"Error calling new int getter: {e.Message}");
                        }
                    }
                }
            }
            else if (typeof(T) == typeof(float))
            {
                if (FloatGetterCache.TryGetValue(key, out var floatGetter))
                {
                    try
                    {
                        var result = floatGetter(instance);
                        return ((T)(object)result, null);
                    }
                    catch (Exception e)
                    {
                        return (default, $"Error calling float getter: {e.Message}");
                    }
                }

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newFloatGetter = CreateSpecializedGetter<float>(member);
                    if (newFloatGetter != null)
                    {
                        FloatGetterCache[key] = newFloatGetter;
                        try
                        {
                            var result = newFloatGetter(instance);
                            return ((T)(object)result, null);
                        }
                        catch (Exception e)
                        {
                            return (default, $"Error calling new float getter: {e.Message}");
                        }
                    }
                }
            }
            else if (typeof(T) == typeof(bool))
            {
                if (BoolGetterCache.TryGetValue(key, out var boolGetter))
                {
                    try
                    {
                        var result = boolGetter(instance);
                        return ((T)(object)result, null);
                    }
                    catch (Exception e)
                    {
                        return (default, $"Error calling bool getter: {e.Message}");
                    }
                }

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newBoolGetter = CreateSpecializedGetter<bool>(member);
                    if (newBoolGetter != null)
                    {
                        BoolGetterCache[key] = newBoolGetter;
                        try
                        {
                            var result = newBoolGetter(instance);
                            return ((T)(object)result, null);
                        }
                        catch (Exception e)
                        {
                            return (default, $"Error calling new bool getter: {e.Message}");
                        }
                    }
                }
            }
            else if (typeof(T) == typeof(double))
            {
                if (DoubleGetterCache.TryGetValue(key, out var doubleGetter))
                {
                    try
                    {
                        var result = doubleGetter(instance);
                        return ((T)(object)result, null);
                    }
                    catch (Exception e)
                    {
                        return (default, $"Error calling double getter: {e.Message}");
                    }
                }

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newDoubleGetter = CreateSpecializedGetter<double>(member);
                    if (newDoubleGetter != null)
                    {
                        DoubleGetterCache[key] = newDoubleGetter;
                        try
                        {
                            var result = newDoubleGetter(instance);
                            return ((T)(object)result, null);
                        }
                        catch (Exception e)
                        {
                            return (default, $"Error calling new double getter: {e.Message}");
                        }
                    }
                }
            }
            else if (typeof(T) == typeof(long))
            {
                if (LongGetterCache.TryGetValue(key, out var longGetter))
                {
                    try
                    {
                        var result = longGetter(instance);
                        return ((T)(object)result, null);
                    }
                    catch (Exception e)
                    {
                        return (default, $"Error calling long getter: {e.Message}");
                    }
                }

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newLongGetter = CreateSpecializedGetter<long>(member);
                    if (newLongGetter != null)
                    {
                        LongGetterCache[key] = newLongGetter;
                        try
                        {
                            var result = newLongGetter(instance);
                            return ((T)(object)result, null);
                        }
                        catch (Exception e)
                        {
                            return (default, $"Error calling new long getter: {e.Message}");
                        }
                    }
                }
            }
            else if (typeof(T) == typeof(string))
            {
                if (StringGetterCache.TryGetValue(key, out var stringGetter))
                {
                    try
                    {
                        var result = stringGetter(instance);
                        return ((T)(object)result, null);
                    }
                    catch (Exception e)
                    {
                        return (default, $"Error calling string getter: {e.Message}");
                    }
                }

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newStringGetter = CreateSpecializedGetter<string>(member);
                    if (newStringGetter != null)
                    {
                        StringGetterCache[key] = newStringGetter;
                        try
                        {
                            var result = newStringGetter(instance);
                            return ((T)(object)result, null);
                        }
                        catch (Exception e)
                        {
                            return (default, $"Error calling new string getter: {e.Message}");
                        }
                    }
                }
            }

            // 如果不是常見的值類型，回退到通用方法
            Profiler.BeginSample("GetValueFromCacheMemberGetter.GetMemberGetter");
            var generalGetter = GetMemberGetter(type, memberName);
            Profiler.EndSample();
            if (generalGetter != null)
            {
                try
                {
                    var value = generalGetter(instance);
                    if (value is T tValue)
                        return (tValue, null);
                    return (
                        default,
                        $"Value type mismatch: expected {typeof(T)}, got {value?.GetType()}"
                    );
                }
                catch (Exception e)
                {
                    return (default, $"Error calling general getter: {e.Message}");
                }
            }

            return (default, $"Member '{memberName}' not found in type {type}");
        }

        /// <summary>
        /// 取得專門的值類型 getter，避免裝箱 GC
        /// </summary>
        public static Func<object, T> GetMemberGetterSpecialized<T>(Type type, string memberName)
        {
            Profiler.BeginSample("GetMemberGetterSpecialized Tuple Key");
            var key = (type, memberName);
            Profiler.EndSample();

            // 根據類型使用對應的緩存
            if (typeof(T) == typeof(int))
            {
                if (IntGetterCache.TryGetValue(key, out var intGetter))
                    return intGetter as Func<object, T>; //這個感覺又gc了...

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newIntGetter = CreateSpecializedGetter<int>(member);
                    if (newIntGetter != null)
                    {
                        IntGetterCache[key] = newIntGetter;
                        return newIntGetter as Func<object, T>;
                    }
                }
            }
            else if (typeof(T) == typeof(float))
            {
                if (FloatGetterCache.TryGetValue(key, out var floatGetter))
                    return floatGetter as Func<object, T>;

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newFloatGetter = CreateSpecializedGetter<float>(member);
                    if (newFloatGetter != null)
                    {
                        FloatGetterCache[key] = newFloatGetter;
                        return newFloatGetter as Func<object, T>;
                    }
                }
            }
            else if (typeof(T) == typeof(bool))
            {
                if (BoolGetterCache.TryGetValue(key, out var boolGetter))
                    return boolGetter as Func<object, T>;

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newBoolGetter = CreateSpecializedGetter<bool>(member);
                    if (newBoolGetter != null)
                    {
                        BoolGetterCache[key] = newBoolGetter;
                        return newBoolGetter as Func<object, T>;
                    }
                }
            }
            else if (typeof(T) == typeof(double))
            {
                if (DoubleGetterCache.TryGetValue(key, out var doubleGetter))
                    return doubleGetter as Func<object, T>;

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newDoubleGetter = CreateSpecializedGetter<double>(member);
                    if (newDoubleGetter != null)
                    {
                        DoubleGetterCache[key] = newDoubleGetter;
                        return newDoubleGetter as Func<object, T>;
                    }
                }
            }
            else if (typeof(T) == typeof(long))
            {
                if (LongGetterCache.TryGetValue(key, out var longGetter))
                    return longGetter as Func<object, T>;

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newLongGetter = CreateSpecializedGetter<long>(member);
                    if (newLongGetter != null)
                    {
                        LongGetterCache[key] = newLongGetter;
                        return newLongGetter as Func<object, T>;
                    }
                }
            }
            else if (typeof(T) == typeof(string))
            {
                if (StringGetterCache.TryGetValue(key, out var stringGetter))
                    return stringGetter as Func<object, T>;

                var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                    type,
                    memberName
                );
                if (member != null)
                {
                    var newStringGetter = CreateSpecializedGetter<string>(member);
                    if (newStringGetter != null)
                    {
                        StringGetterCache[key] = newStringGetter;
                        return newStringGetter as Func<object, T>;
                    }
                }
            }

            // 如果不是常見的值類型，回退到通用方法
            Profiler.BeginSample("GetMemberGetterSpecialized.GetMemberGetter");
            var generalGetter = GetMemberGetter(type, memberName);
            Profiler.EndSample();
            if (generalGetter != null)
            {
                return obj =>
                {
                    var value = generalGetter(obj);
                    if (value is T tValue)
                        return tValue;
                    return default;
                };
            }

            return null;
        }

        /// <summary>
        /// 創建專門的值類型 getter，避免裝箱
        /// </summary>
        private static Func<object, T> CreateSpecializedGetter<T>(MemberInfo member)
        {
            if (member is PropertyInfo prop)
            {
                return CreateSpecializedPropertyGetter<T>(prop);
            }

            if (member is FieldInfo field)
            {
                return CreateSpecializedFieldGetter<T>(field);
            }

            return null;
        }

        /// <summary>
        /// 使用 Expression 建立專門的 property getter，避免裝箱
        /// </summary>
        private static Func<object, T> CreateSpecializedPropertyGetter<T>(PropertyInfo property)
        {
            if (property.PropertyType != typeof(T))
                return null;

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instanceParam, property.DeclaringType);
            var propertyAccess = Expression.Property(castInstance, property);
            var lambda = Expression.Lambda<Func<object, T>>(propertyAccess, instanceParam);
            return lambda.Compile();
        }

        /// <summary>
        /// 使用 Expression 建立專門的 field getter，避免裝箱
        /// </summary>
        private static Func<object, T> CreateSpecializedFieldGetter<T>(FieldInfo field)
        {
            if (field.FieldType != typeof(T))
                return null;

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instanceParam, field.DeclaringType);
            var fieldAccess = Expression.Field(castInstance, field);
            var lambda = Expression.Lambda<Func<object, T>>(fieldAccess, instanceParam);
            return lambda.Compile();
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
            return GetMemberType(parentType, memberName, null);
        }

        /// <summary>
        /// 取得指定型別與成員名稱的成員型別，支援動態型別推斷
        /// </summary>
        /// <param name="parentType">父型別</param>
        /// <param name="memberName">成員名稱</param>
        /// <param name="instance">實際物件實例，用於動態型別推斷</param>
        /// <returns>成員型別，如果找不到則回傳 null</returns>
        public static Type GetMemberType(Type parentType, string memberName, object instance)
        {
            if (parentType == null || string.IsNullOrEmpty(memberName))
                return null;

            var key = (parentType, memberName);
            if (MemberTypeCache.TryGetValue(key, out var cachedType))
                return cachedType;

            // 使用 RefactorSafeNameResolver 查找成員（支援舊名稱）
            var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                parentType,
                memberName
            );

            Type memberType = null;

            if (member is PropertyInfo prop)
            {
                memberType = prop.PropertyType;

                // 檢查是否有DynamicType attribute
                var dynamicTypeAttr = prop.GetCustomAttribute<Attributes.DynamicTypeAttribute>();
                if (dynamicTypeAttr != null && instance != null)
                {
                    // Debug.Log(
                    //     $"動態型別：{dynamicTypeAttr.TypeProviderMethod} 來自屬性 {prop.Name}"
                    // );
                    memberType =
                        GetDynamicMemberType(parentType, prop, dynamicTypeAttr, instance)
                        ?? memberType;
                }
                else
                {
                    // Debug.Log($"靜態型別：{memberType} 來自屬性 {prop.Name}");
                }
            }
            else if (member is FieldInfo field)
            {
                memberType = field.FieldType;

                // 檢查是否有DynamicType attribute
                var dynamicTypeAttr =
                    field.GetCustomAttribute<MonoFSM.Core.Attributes.DynamicTypeAttribute>();
                if (dynamicTypeAttr != null && instance != null)
                {
                    memberType =
                        GetDynamicMemberType(parentType, field, dynamicTypeAttr, instance)
                        ?? memberType;
                }
            }
            // else
            // {
            //     // 如果 RefactorSafeNameResolver 找不到，嘗試直接查找
            //     var directProp = parentType.GetProperty(
            //         memberName,
            //         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            //     );
            //     if (directProp != null)
            //     {
            //         memberType = directProp.PropertyType;
            //     }
            //     else
            //     {
            //         var directField = parentType.GetField(
            //             memberName,
            //             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            //         );
            //         if (directField != null)
            //         {
            //             memberType = directField.FieldType;
            //         }
            //     }
            // }

            // 快取結果（即使是 null 也要快取，避免重複查找）
            MemberTypeCache[key] = memberType;
            return memberType;
        }

        /// <summary>
        /// 取得動態成員的實際型別
        /// </summary>
        private static Type GetDynamicMemberType(
            Type parentType,
            MemberInfo member,
            Attributes.DynamicTypeAttribute dynamicTypeAttr,
            object instance
        )
        {
            try
            {
                // 嘗試找到VarTag欄位
                var varTagField = parentType.GetField(
                    dynamicTypeAttr.VarTagFieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (
                    varTagField?.FieldType == typeof(MonoFSM.Variable.VariableTag)
                    && instance != null
                )
                {
                    // 取得實際的VarTag實例
                    var varTag = varTagField.GetValue(instance) as MonoFSM.Variable.VariableTag;
                    if (varTag?.ValueFilterType != null)
                    {
                        // 返回VarTag的RestrictType！
                        // Debug.Log(
                        //     $"動態型別：{varTag.ValueFilterType} 來自 VarTag {varTagField.Name}"
                        // );
                        return varTag.ValueFilterType;
                    }
                }

                // 嘗試透過TypeProvider方法取得動態型別
                var typeProviderMethod = parentType.GetMethod(
                    dynamicTypeAttr.TypeProviderMethod,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (typeProviderMethod?.ReturnType == typeof(Type) && instance != null)
                {
                    var dynamicType = typeProviderMethod.Invoke(instance, null) as Type;
                    if (dynamicType != null)
                    {
                        return dynamicType;
                    }
                }
            }
            catch
            {
                // 如果發生錯誤，回退到預設行為
            }

            return null;
        }

        /// <summary>
        /// 清除反射快取
        /// </summary>
        public static void ClearCache()
        {
            GetterCache.Clear();
            MemberTypeCache.Clear();
            IntGetterCache.Clear();
            FloatGetterCache.Clear();
            BoolGetterCache.Clear();
            DoubleGetterCache.Clear();
            LongGetterCache.Clear();
            StringGetterCache.Clear();
        }

        /// <summary>
        /// 取得快取中的項目數量
        /// </summary>
        public static int CacheCount =>
            GetterCache.Count
            + MemberTypeCache.Count
            + IntGetterCache.Count
            + FloatGetterCache.Count
            + BoolGetterCache.Count
            + DoubleGetterCache.Count
            + LongGetterCache.Count
            + StringGetterCache.Count;

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
                if (to.IsAssignableFrom(from))
                    return true;
                if (to == typeof(string))
                    return true; // 大部分型別都可以轉成字串

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
        public static (T, string) GetFieldValueFromPath<T>(
            object obj, //target
            List<FieldPathEntry> entries,
            Object logTarget = null
        )
        {
            T finalValue = default;
            var finalType = typeof(T);
            if (obj == null)
                return (default, "obj null");
            var currentObj = obj;

            var i = 0;
            foreach (var entry in entries)
            {
                if (currentObj == null)
                {
                    Debug.LogError($"在 '{entry._propertyName}' 層級遇到 null", logTarget);
                    if (Application.isPlaying == false)
                        return (default, $"在 '{entry._propertyName}' 層級遇到 null");
                    return (default, "propertyName null");
                }

                // 直接從 currentObj 獲取實際的 Type，而不依賴序列化的資料
                var objType = currentObj.GetType();

                if (entry._propertyName == null)
                    return (default, "欄位名稱為空");

                // 檢查欄位是否已重命名，如果是則更新 entry.fieldName FIXME: 這段應該抽出去？
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                {
                    var foundFormerMember =
                        RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
                            objType,
                            entry._propertyName
                        );
                    if (foundFormerMember != null && foundFormerMember.Name != entry._propertyName)
                    {
                        Debug.LogWarning(
                            $"欄位 '{entry._propertyName}' 已重命名為 '{foundFormerMember.Name}'，正在更新參考",
                            logTarget
                        );
                        entry._propertyName = foundFormerMember.Name;
                    }
                }
#endif

                //有可能最後一層不是primitive嗎？
                if (i == entries.Count - 1 && finalType.IsPrimitive) //最後一層 FIXME: nooo 可能拿到array....
                {
                    var (value, error) = GetValueFromCacheMemberGetter<T>(
                        currentObj,
                        objType,
                        entry._propertyName
                    );
                    if (!string.IsNullOrEmpty(error))
                    {
                        return (default, error);
                    }

                    return (value, "ok");
                }
                else
                {
                    var getter = GetMemberGetter(objType, entry._propertyName);
                    if (getter == null)
                    {
                        Debug.LogError(
                            $"找不到欄位 '{entry._propertyName}' 在 Type '{objType}' 中",
                            logTarget
                        );
                        return (default, $"找不到欄位 '{entry._propertyName}' 在 Type '{objType}' 中");
                    }
                    var value = getter(currentObj);
                    if (value == null)
                    {
                        if (entry._canBeNull)
                            return (default, "allow null");
                        //這段好像錯位置了？
                        Debug.LogError(
                            $"(可以把path上打勾標示可以是null) 期待Type{finalType} 最終欄位 currentObj {currentObj} objType:{objType} propert:'{entry._propertyName}' 為 null，但不允許 null",
                            logTarget
                        );
                        return (
                            default,
                            $"最終欄位 '{entry._propertyName}' 為 null，但不允許 null"
                        );
                    }

                    currentObj = getter(currentObj); // 可能拿到陣列
                }

                // 如果是陣列，取得指定index的element value
                if (currentObj is Array arr)
                {
                    if (entry.index < 0 || entry.index >= arr.Length)
                    {
                        Debug.LogError(
                            $"索引 {entry.index} 超出陣列 '{entry._propertyName}' 的範圍 (長度 {arr.Length})",
                            logTarget
                        );
                        return (
                            default,
                            $"索引 {entry.index} 超出陣列 '{entry._propertyName}' 的範圍 (長度 {arr.Length})"
                        );
                    }

                    currentObj = arr.GetValue(entry.index);
                }

                if (currentObj == null) //半途遇到 null
                    if (entry._canBeNull)
                        return (default, "allow null");
                i++;
#if UNITY_EDITOR
                entry._tempCurrentObject = currentObj; // 用於 Unity 編輯器的預覽
#endif
            }

            return ((T)currentObj, "obj ok");
            // return (currentObj, "ok");
        }

        /// <summary>
        /// 更新 pathEntries 中每一層的型別資訊
        /// </summary>
        /// <param name="pathEntries">欄位路徑項目</param>
        /// <param name="startType">起始型別</param>
        /// <param name="supportedTypes">支援的型別清單</param>
        /// <param name="indexInjector">索引注入器</param>
        public static void UpdatePathEntryTypes(
            List<FieldPathEntry> pathEntries,
            Type startType,
            List<Type> supportedTypes = null,
            IIndexInjector indexInjector = null
        ) //FIXME: 拿掉indexInjector?
        {
            if (pathEntries == null)
                return;

            var currentType = startType;

            for (var i = 0; i < pathEntries.Count; i++)
            {
                if (pathEntries[i]._serializedType == null)
                    pathEntries[i]._serializedType = new MySerializedType<Object>();
                pathEntries[i]._serializedType.SetType(currentType);
                if (supportedTypes != null)
                    pathEntries[i]._supportedTypes = supportedTypes;

                // 把index注入到pathEntries
                if (indexInjector != null && pathEntries[i].IsArray)
                    pathEntries[i].index = indexInjector.Index;

                if (currentType != null && !string.IsNullOrEmpty(pathEntries[i]._propertyName))
                {
                    // 嘗試 Property
                    var prop = currentType.GetProperty(
                        pathEntries[i]._propertyName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    );
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
        public static bool IsFieldPathTypeCompatible(
            Object obj,
            List<FieldPathEntry> pathEntries,
            Type targetType
        )
        {
            if (pathEntries == null || pathEntries.Count == 0)
                return true;
            if (obj == null)
                return false;

            try
            {
                var (fieldValue, _) = GetFieldValueFromPath<object>(obj, pathEntries);
                if (fieldValue == null)
                    return false;

                var resultType = fieldValue.GetType();
                Debug.Log($"最終欄位值型別：{resultType}, 目標型別：{targetType}", obj);
                return targetType.IsAssignableFrom(resultType)
                    || CanConvertType(resultType, targetType);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 根據 pathEntries 依序利用反射設定最終欄位值
        /// 支援若欄位為陣列時，根據 index 設定對應元素
        /// </summary>
        /// <param name="obj">起始物件</param>
        /// <param name="entries">欄位路徑項目</param>
        /// <param name="value">要設定的值</param>
        /// <param name="logTarget">用於 Debug.Log 的目標物件</param>
        /// <returns>是否設定成功</returns>
        public static bool SetFieldValueFromPath(
            object obj,
            List<FieldPathEntry> entries,
            object value,
            Object logTarget = null
        )
        {
            if (obj == null || entries == null || entries.Count == 0)
            {
                Debug.LogError("SetFieldValueFromPath: 無效的參數", logTarget);
                return false;
            }

            var currentObj = obj;
            var lastEntry = entries[entries.Count - 1];

            // 遍歷到倒數第二層
            for (var i = 0; i < entries.Count - 1; i++)
            {
                var entry = entries[i];
                if (currentObj == null)
                {
                    Debug.LogError($"在 '{entry._propertyName}' 層級遇到 null", logTarget);
                    return false;
                }

                var type = currentObj.GetType();
                var getter = GetMemberGetter(type, entry._propertyName);

                if (getter != null)
                {
                    currentObj = getter(currentObj);
                }
                else
                {
                    Debug.LogError(
                        $"在 {type.Name} 中找不到名稱為 '{entry._propertyName}' 的欄位或屬性",
                        logTarget
                    );
                    return false;
                }

                // 如果是陣列，取得指定index的element
                if (currentObj is Array arr)
                {
                    if (entry.index < 0 || entry.index >= arr.Length)
                    {
                        Debug.LogError(
                            $"索引 {entry.index} 超出陣列 '{entry._propertyName}' 的範圍",
                            logTarget
                        );
                        return false;
                    }
                    currentObj = arr.GetValue(entry.index);
                }
            }

            // 設定最終欄位值
            if (currentObj == null)
            {
                Debug.LogError($"目標物件為 null，無法設定 '{lastEntry._propertyName}'", logTarget);
                return false;
            }

            return SetMemberValue(currentObj, lastEntry._propertyName, value, logTarget);
        }

        /// <summary>
        /// 使用反射設定物件的成員值（支援 Field 和 Property）
        /// </summary>
        private static bool SetMemberValue(
            object obj,
            string memberName,
            object value,
            Object logTarget = null
        )
        {
            if (obj == null || string.IsNullOrEmpty(memberName))
                return false;

            var type = obj.GetType();

            // 使用 RefactorSafeNameResolver 查找成員（支援舊名稱）
            var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(type, memberName);

            if (member is PropertyInfo prop)
            {
                if (!prop.CanWrite)
                {
                    Debug.LogError($"屬性 '{memberName}' 是唯讀的，無法設定值", logTarget);
                    return false;
                }

                try
                {
                    // 嘗試型別轉換
                    var convertedValue = ConvertValueToType(value, prop.PropertyType);
                    prop.SetValue(obj, convertedValue);
                    Debug.Log($"成功設定屬性 '{memberName}' = {convertedValue}", logTarget);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"設定屬性 '{memberName}' 時發生錯誤: {e.Message}", logTarget);
                    return false;
                }
            }

            if (member is FieldInfo field)
            {
                try
                {
                    // 嘗試型別轉換
                    var convertedValue = ConvertValueToType(value, field.FieldType);
                    field.SetValue(obj, convertedValue);
                    Debug.Log($"成功設定欄位 '{memberName}' = {convertedValue}", logTarget);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"設定欄位 '{memberName}' 時發生錯誤: {e.Message}", logTarget);
                    return false;
                }
            }

            Debug.LogError($"在 {type.Name} 中找不到名稱為 '{memberName}' 的欄位或屬性", logTarget);
            return false;
        }

        /// <summary>
        /// 嘗試將值轉換為目標型別
        /// </summary>
        private static object ConvertValueToType(object value, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                // 如果轉換失敗，嘗試直接返回原值
                throw new InvalidCastException($"無法將 {value.GetType()} 轉換為 {targetType}");
            }
        }

        #endregion
    }
}
