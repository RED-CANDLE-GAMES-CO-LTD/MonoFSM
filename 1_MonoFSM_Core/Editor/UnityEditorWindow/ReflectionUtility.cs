using System.Linq;

namespace MonoFSM.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;


    public static class ReflectionUtil
    {
        private static FieldInfo GetFieldInfo(Type type, string fieldName, BindingFlags flags)
            => type.GetField(fieldName, flags);

        public static T GetFieldValue<T>(object instance, string fieldName, BindingFlags flags, Type type = null,
            Func<object, T> converter = null)
            => (converter ?? (o => (T)o)).Invoke(GetFieldInfo(type ?? instance.GetType(), fieldName, flags)
                .GetValue(instance));

        public static void SetFieldValue(object instance, string fieldName, object value, BindingFlags flags,
            Type type = null)
            => GetFieldInfo(type ?? instance.GetType(), fieldName, flags).SetValue(instance, value);

        private static PropertyInfo GetPropInfo(Type type, string fieldName, BindingFlags flags)
            => type.GetProperty(fieldName, flags);

        public static T GetPropValue<T>(object instance, string propName, BindingFlags flags, Type type = null,
            Func<object, T> converter = null)
            => (converter ?? (o => (T)o)).Invoke(GetPropInfo(type ?? instance.GetType(), propName, flags)
                .GetValue(instance));

        public static void SetPropValue(object instance, string propName, object value, BindingFlags flags,
            Type type = null)
            => GetPropInfo(type ?? instance.GetType(), propName, flags).SetValue(instance, value);

        private static MethodInfo GetMethodInfo(Type type, string methodName, BindingFlags flags)
            => type.GetMethod(methodName, flags);

        public static T InvokeMethod<T>(object instance, string methodName, object[] args, BindingFlags flags,
            Type type = null, Func<object, T> converter = null)
            => (converter ?? (o => (T)o)).Invoke(GetMethodInfo(type ?? instance.GetType(), methodName, flags)
                .Invoke(instance, args));

        public static void InvokeMethod(object instance, string methodName, object[] args, BindingFlags flags,
            Type type = null)
            => InvokeMethod<object>(instance, methodName, args, flags, type);

        public static T InvokeStaticMethod<T>(Type type, string methodName, object[] args, BindingFlags additionalFlags,
            Func<object, T> converter = null)
            => InvokeMethod(null, methodName, args, BindingFlags.Static | additionalFlags, type, converter);

        public static void InvokeStaticMethod(Type type, string methodName, object[] args, BindingFlags additionalFlags)
            => InvokeMethod<object>(null, methodName, args, BindingFlags.Static | additionalFlags, type);

        public static List<T> ConvertList<T>(object original, Func<object, T> convertItem)
        {
            var originalListType = original.GetType();
            var countProp = originalListType.GetProperty("Count");
            var count = (int)countProp.GetValue(original, null);
            var indexer = originalListType.GetProperty("Item");

            var result = new List<T>();
            for (var i = 0; i < count; i++)
            {
                var item = indexer.GetValue(original, new object[] { i });
                result.Add(convertItem(item));
            }

            return result;
        }

        public static T[] ConvertArray<T>(object original, Func<object, T> convertItem)
        {
            var array = (Array)original;
            var result = new T[array.Length];
            for (var i = 0; i < array.Length; i++)
            {
                result[i] = convertItem(array.GetValue(i));
            }

            return result;
        }
    }

    public static class ReflectionUtils
    {
        #region Reflection


        public static object GetFieldValue(this object o, string fieldName)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(fieldName) is FieldInfo fieldInfo)
                return fieldInfo.GetValue(target);


            throw new System.Exception($"Field '{fieldName}' not found in type '{type.Name}' and its parent types");

        }
        public static object GetPropertyValue(this object o, string propertyName)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetPropertyInfo(propertyName) is PropertyInfo propertyInfo)
                return propertyInfo.GetValue(target);


            throw new System.Exception($"Property '{propertyName}' not found in type '{type.Name}' and its parent types");

        }
        public static object GetMemberValue(this object o, string memberName)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(memberName) is FieldInfo fieldInfo)
                return fieldInfo.GetValue(target);

            if (type.GetPropertyInfo(memberName) is PropertyInfo propertyInfo)
                return propertyInfo.GetValue(target);


            throw new System.Exception($"Member '{memberName}' not found in type '{type.Name}' and its parent types");

        }

        public static void SetFieldValue(this object o, string fieldName, object value)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(fieldName) is FieldInfo fieldInfo)
                fieldInfo.SetValue(target, value);


            else throw new System.Exception($"Field '{fieldName}' not found in type '{type.Name}' and its parent types");

        }
        public static void SetPropertyValue(this object o, string propertyName, object value)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetPropertyInfo(propertyName) is PropertyInfo propertyInfo)
                propertyInfo.SetValue(target, value);


            else throw new System.Exception($"Property '{propertyName}' not found in type '{type.Name}' and its parent types");

        }
        public static void SetMemberValue(this object o, string memberName, object value)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(memberName) is FieldInfo fieldInfo)
                fieldInfo.SetValue(target, value);

            else if (type.GetPropertyInfo(memberName) is PropertyInfo propertyInfo)
                propertyInfo.SetValue(target, value);


            else throw new System.Exception($"Member '{memberName}' not found in type '{type.Name}' and its parent types");

        }

        public static object InvokeMethod(this object o, string methodName, params object[] parameters) // todo handle null params (can't get their type)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetMethodInfo(methodName, parameters.Select(r => r.GetType()).ToArray()) is MethodInfo methodInfo)
                return methodInfo.Invoke(target, parameters);


            throw new System.Exception($"Method '{methodName}' not found in type '{type.Name}', its parent types and interfaces");

        }


        public static T GetFieldValue<T>(this object o, string fieldName) => (T)o.GetFieldValue(fieldName);
        public static T GetPropertyValue<T>(this object o, string propertyName) => (T)o.GetPropertyValue(propertyName);
        public static T GetMemberValue<T>(this object o, string memberName) => (T)o.GetMemberValue(memberName);
        public static T InvokeMethod<T>(this object o, string methodName, params object[] parameters) => (T)o.InvokeMethod(methodName, parameters);




        public static FieldInfo GetFieldInfo(this Type type, string fieldName)
        {
            if (fieldInfoCache.TryGetValue(type, out var fieldInfosByNames))
                if (fieldInfosByNames.TryGetValue(fieldName, out var fieldInfo))
                    return fieldInfo;


            if (!fieldInfoCache.ContainsKey(type))
                fieldInfoCache[type] = new Dictionary<string, FieldInfo>();

            for (var curType = type; curType != null; curType = curType.BaseType)
                if (curType.GetField(fieldName, maxBindingFlags) is FieldInfo fieldInfo)
                    return fieldInfoCache[type][fieldName] = fieldInfo;


            return fieldInfoCache[type][fieldName] = null;

        }
        public static PropertyInfo GetPropertyInfo(this Type type, string propertyName)
        {
            if (propertyInfoCache.TryGetValue(type, out var propertyInfosByNames))
                if (propertyInfosByNames.TryGetValue(propertyName, out var propertyInfo))
                    return propertyInfo;


            if (!propertyInfoCache.ContainsKey(type))
                propertyInfoCache[type] = new Dictionary<string, PropertyInfo>();

            for (var curType = type; curType != null; curType = curType.BaseType)
                if (curType.GetProperty(propertyName, maxBindingFlags) is PropertyInfo propertyInfo)
                    return propertyInfoCache[type][propertyName] = propertyInfo;


            return propertyInfoCache[type][propertyName] = null;

        }
        public static MethodInfo GetMethodInfo(this Type type, string methodName, params Type[] argumentTypes)
        {
            var methodHash = methodName.GetHashCode() ^ argumentTypes.Aggregate(0, (hash, r) => hash ^= r.GetHashCode());


            if (methodInfoCache.TryGetValue(type, out var methodInfosByHashes))
                if (methodInfosByHashes.TryGetValue(methodHash, out var methodInfo))
                    return methodInfo;



            if (!methodInfoCache.ContainsKey(type))
                methodInfoCache[type] = new Dictionary<int, MethodInfo>();

            for (var curType = type; curType != null; curType = curType.BaseType)
                if (curType.GetMethod(methodName, maxBindingFlags, null, argumentTypes, null) is MethodInfo methodInfo)
                    return methodInfoCache[type][methodHash] = methodInfo;

            foreach (var interfaceType in type.GetInterfaces())
                if (interfaceType.GetMethod(methodName, maxBindingFlags, null, argumentTypes, null) is MethodInfo methodInfo)
                    return methodInfoCache[type][methodHash] = methodInfo;



            return methodInfoCache[type][methodHash] = null;

        }

        static Dictionary<Type, Dictionary<string, FieldInfo>> fieldInfoCache = new();
        static Dictionary<Type, Dictionary<string, PropertyInfo>> propertyInfoCache = new();
        static Dictionary<Type, Dictionary<int, MethodInfo>> methodInfoCache = new();




        public const BindingFlags maxBindingFlags = (BindingFlags)62;








        #endregion
    }
}