using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.DataProvider
{
    public static class DataReflection
    {
        public static ValueDropdownList<string> GetProperties(object obj, List<Type> supportedTypes,
            bool isArray = false)
        {
            return GetProperties(obj.GetType(), supportedTypes, isArray);
        }

        public static ValueDropdownList<string> GetProperties(Type type, List<Type> supportedTypes,
            bool isArray = false)
        {
            // AppDomain.CurrentDomain.GetAssemblies().

            // Debug.Log(type);
            var fields = new List<string>();
            //FIXME: cache可以放在哪？
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dropdownList = new ValueDropdownList<string>();
            foreach (var property in properties)
            {
                if (isArray && !property.PropertyType.IsArray)
                {
                    // fields.Add(property.Name);
                    // dropdownList.Add(property.Name + " (" + property.PropertyType.Name + ")", property.Name);
                    continue;
                }

                if (supportedTypes != null && !supportedTypes.Contains(property.PropertyType))
                    continue;
                fields.Add(property.Name);
                dropdownList.Add(property.Name + " (" + property.PropertyType.Name + ")", property.Name);
            }

            return dropdownList;
        }
    }
}