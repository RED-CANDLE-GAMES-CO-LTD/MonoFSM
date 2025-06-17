using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MonoFSM.Core
{
    public static class ObjectExtensions
    {
        public static T DeepClone<T>(this T original)
        {
            return (T)DeepClone((object)original);
        }

        private static object DeepClone(object original)
        {
            if (original == null)
            {
                return null;
            }

            var type = original.GetType();

            if (type.IsPrimitive || original is string)
            {
                return original;
            }

            if (original is IDictionary dictionary)
            {
                var cloneDictionary = (IDictionary)Activator.CreateInstance(type);
                foreach (var key in dictionary.Keys)
                {
                    cloneDictionary.Add(DeepClone(key), DeepClone(dictionary[key]));
                }

                return cloneDictionary;
            }

            if (original is IList list)
            {
                var cloneList = (IList)Activator.CreateInstance(type);
                foreach (var item in list)
                {
                    cloneList.Add(DeepClone(item));
                }

                return cloneList;
            }

            Debug.Log(type);
            var cloneObject = Activator.CreateInstance(type);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var fieldValue = field.GetValue(original);
                field.SetValue(cloneObject, DeepClone(fieldValue));
            }

            return cloneObject;
        }
    }
}