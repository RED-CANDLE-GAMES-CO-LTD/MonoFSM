namespace RCGMaker.Editor
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
}