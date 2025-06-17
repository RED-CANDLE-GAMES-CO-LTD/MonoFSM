namespace MonoFSM.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public static class UnityEditorAssembly
    {
        private const string UnityEditor = "UnityEditor";
        private static readonly Assembly unityEditorAsm = Assembly.Load(UnityEditor);

        public static Type GetClassType(string className) => unityEditorAsm.GetType($"{UnityEditor}.{className}");
    }

    public abstract class NonPublicClassWrapper
    {
        protected object Original { get; }

        private static BindingFlags InstanceNonPublic => BindingFlags.Instance | BindingFlags.NonPublic;
        private static BindingFlags InstancePublic => BindingFlags.Instance | BindingFlags.Public;
        private Type OriginalType => _originalTypeCache ??= LoadOriginalType();
        private Type _originalTypeCache;

        protected NonPublicClassWrapper()
        {
        }

        protected NonPublicClassWrapper(object original)
        {
            Original = original;
            _originalTypeCache = original.GetType();
        }

        protected abstract Type LoadOriginalType();

        #region Fields

        protected T GetNonPublicField<T>(string fieldName, Func<object, T> converter = null)
            => ReflectionUtil.GetFieldValue(Original, fieldName, InstanceNonPublic, OriginalType, converter);

        protected T GetPublicField<T>(string fieldName, Func<object, T> converter = null)
            => ReflectionUtil.GetFieldValue(Original, fieldName, InstancePublic, OriginalType, converter);

        protected void SetNonPublicField(string fieldName, object value)
            => ReflectionUtil.SetFieldValue(Original, fieldName, value, InstanceNonPublic, OriginalType);

        protected void SetPublicField(string fieldName, object value)
            => ReflectionUtil.SetFieldValue(Original, fieldName, value, InstancePublic, OriginalType);

        #endregion

        #region Properties

        protected T GetNonPublicProp<T>(string propName, Func<object, T> converter = null)
            => ReflectionUtil.GetPropValue(Original, propName, InstanceNonPublic, OriginalType, converter);

        protected T GetPublicProp<T>(string propName, Func<object, T> converter = null)
            => ReflectionUtil.GetPropValue(Original, propName, InstancePublic, OriginalType, converter);

        protected void SetNonPublicProp(string propName, object value)
            => ReflectionUtil.SetPropValue(Original, propName, value, InstanceNonPublic, OriginalType);

        protected void SetPublicProp(string propName, object value)
            => ReflectionUtil.SetPropValue(Original, propName, value, InstancePublic, OriginalType);

        protected List<T> GetNonPublicListProp<T>(string propName, Func<object, T> convertItem)
            => ReflectionUtil.ConvertList(GetNonPublicProp<object>(propName), convertItem);

        protected List<T> GetPublicListProp<T>(string propName, Func<object, T> convertItem)
            => ReflectionUtil.ConvertList(GetPublicProp<object>(propName), convertItem);

        protected T[] GetNonPublicArrayProp<T>(string propName, Func<object, T> convertItem)
            => ReflectionUtil.ConvertArray(GetNonPublicProp<T>(propName), convertItem);

        protected T[] GetPublicArrayProp<T>(string propName, Func<object, T> convertItem)
            => ReflectionUtil.ConvertArray(GetPublicProp<object>(propName), convertItem);

        #endregion

        #region Methods

        protected T InvokeNonPublicMethod<T>(string methodName, object[] args = null)
            => ReflectionUtil.InvokeMethod<T>(Original, methodName, args ?? Array.Empty<object>(), InstanceNonPublic,
                OriginalType);

        protected void InvokeNonPublicMethod(string methodName, object[] args = null)
            => InvokeNonPublicMethod<object>(methodName, args);

        protected T InvokePublicMethod<T>(string methodName, object[] args = null)
            => ReflectionUtil.InvokeMethod<T>(Original, methodName, args ?? Array.Empty<object>(), InstancePublic,
                OriginalType);

        protected void InvokePublicMethod(string methodName, object[] args = null)
            => InvokePublicMethod<object>(methodName, args);

        protected T InvokeStaticMethod<T>(string methodName, object[] args = null)
            => ReflectionUtil.InvokeStaticMethod<T>(OriginalType, methodName, args ?? Array.Empty<object>(),
                BindingFlags.Static | BindingFlags.NonPublic, null);

        #endregion
    }

    public class UnityEditorWrapperBase : NonPublicClassWrapper
    {
        protected UnityEditorWrapperBase(object original) : base(original)
        {
        }

        protected override Type LoadOriginalType()
        {
            return Assembly.Load("UnityEditor").GetType($"UnityEditor.{GetType()}");
        }
    }
    
}