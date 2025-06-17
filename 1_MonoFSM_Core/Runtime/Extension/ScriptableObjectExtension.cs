using System;
using System.Reflection;
using UnityEngine;

namespace MonoFSM.Core
{
    public static class ScriptableObjectExtension
    {
        public static void DeepCopy<T>(this T target, T source) where T : ScriptableObject
        {
            //FIXME: 用這個就可以了，DescriptableData有做了CopySource EditorUtility.CopySerializedManagedFieldsOnly(source, this);

            // target.name = source.name;
            // target.hideFlags = source.hideFlags;
            //
            // FieldInfo[] fields =
            //     typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // foreach (FieldInfo field in fields)
            // {
            //     object value = field.GetValue(source);
            //     if (value is ICloneable cloneable) // Handle custom deep-copyable types
            //     {
            //         field.SetValue(target, cloneable.Clone());
            //     }
            //     else if (value is ScriptableObject so) // Handle nested ScriptableObjects
            //     {
            //         ScriptableObject copy = ScriptableObject.Instantiate(so);
            //         field.SetValue(target, copy);
            //     }
            //     else
            //     {
            //         field.SetValue(target, value);
            //     }
            // }
        }
    }
}