using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;

namespace Auto.Utils
{

    public struct GetInterfaceQuery
    {
        public Type targetType;
        public string targetInterface;

        public GetInterfaceQuery(Type type, string targetInterface) : this()
        {
            this.targetType = type;
            this.targetInterface = targetInterface;
        }
    }

    public class ReflectionHelperMethods
    {

        public static Dictionary<GetInterfaceQuery, bool> getInterfaceCache = new Dictionary<GetInterfaceQuery, bool>();

        public bool ImplementsInterface(Type type, string @interface)
        {
            bool result = false;
            GetInterfaceQuery query = new GetInterfaceQuery(type, @interface);
            if (getInterfaceCache.ContainsKey(query))
            {
                result = getInterfaceCache[query];
            }
            else
            {
                result = type.GetInterface(@interface) != null;
                getInterfaceCache[query] = result;
            }

            return result;
        }

        public IEnumerable<MemberInfo> GetAllVariables(object instanceObj)
        {
            return GetAllVariables(instanceObj.GetType());
        }

        public IEnumerable<MemberInfo> GetAllVariables(Type objectType)
        {
            //Fields that are automatically generated backing fields will be serialized as properties, and skipped as fields. 
            FieldInfo[] componentFields = objectType
                .GetFields(/*BindingFlags.NonPublic |*/ BindingFlags.Instance | BindingFlags.Public)
                .Where(f => f.Name.Contains("__BackingField") == false)
                .ToArray();

            //Although it might sound like an incredible idea to ignore all properties, life is not so beautiful.... Unity's components only declare properties, and no fields. 
            //Those properties are connected to the C++ side of things.... And they do not contain the info as fields.
            //Because of this, it is essential to also get the properties. Else a Rigidbody or ConfigurableJoint will result in having 0 variables.
            //Also, we can take out of the equation all properties that can't be written or read. Nothing we can do about them. 
            //Oh, and, last thing, the third where (rop.GetIndexParameters().Length == 0) is used to filder out indexers. I do not support them, because never use them and couldn't be bothered
            PropertyInfo[] componentProperties = objectType.GetProperties(/*BindingFlags.NonPublic |*/ BindingFlags.Instance | BindingFlags.Public)
                .Where(prop => prop.DeclaringType != typeof(Component))
                .Where(prop => prop.CanRead && prop.CanWrite)
                .Where(prop => prop.GetIndexParameters().Length == 0)
                .ToArray();


            //Adding protected, internal and private members
            componentFields = componentFields.Concat(
                GetNonPublicFieldsInBaseClasses(objectType)
                .Where(f => f.Name.Contains("__BackingField") == false)
            ).ToArray();


            componentProperties = componentProperties.Concat(
                GetNonPublicPropertiesInBaseClasses(objectType)
                .Where(prop => prop.DeclaringType != typeof(Component))
                .Where(prop => prop.CanRead && prop.CanWrite)
                .Where(prop => prop.GetIndexParameters().Length == 0)
            ).ToArray();


            IEnumerable<MemberInfo> typeVariables =
            componentFields.Cast<MemberInfo>()
            .Concat(
                componentProperties.Cast<MemberInfo>()
            );

            return typeVariables;
        }

        public string GetName(MemberInfo variable)
        {
            if (variable is PropertyInfo)
            {
                PropertyInfo propInfo = (PropertyInfo)variable;
                return propInfo.Name;
            }
            else
            {
                if (variable.Name.Contains("BackingField"))
                {
                    return new string(variable.Name.Skip(1).TakeWhile(c => c != '>').ToArray());
                }
                return variable.Name;
            }
        }

        public object GetValue(MemberInfo info, object instance)
        {
            if (info as PropertyInfo != null)
            {
                PropertyInfo propInfo = (PropertyInfo)info;
                if (propInfo.GetIndexParameters().Length > 0)
                {
                    Debug.LogWarning("Sorry. We do not support indexed properties yet. It's not hard, it's just a lot of work, and we have lived without it until now.");
                    return null;
                }
                return propInfo.GetValue(instance, null);
            }
            if (info as FieldInfo != null)
            {
                FieldInfo memberInfo = (FieldInfo)info;
                return memberInfo.GetValue(instance);
            }
            else
            {
                throw new Exception("Couldn't Get the value");
            }
        }


        public void SetValue(MemberInfo info, object instance, object value)
        {
            if (info is PropertyInfo)
            {
                PropertyInfo propInfo = (PropertyInfo)info;
                if (propInfo.GetIndexParameters().Length > 0)
                {
                    Debug.LogWarning("Sorry. We do not support indexed properties yet. It's not hard, it's just a lot of work, and we have lived without it until now.");
                    return;
                }
                propInfo.SetValue(instance, value, null);
            }
            else if (info is FieldInfo)
            {
                FieldInfo memberInfo = (FieldInfo)info;
                memberInfo.SetValue(instance, value);
            }
            else
            {
                throw new Exception("Couldn't Set the value");
            }
        }

        public bool IsStruct(MemberInfo memberInfo)
        {
            var variableType = GetVariableType(memberInfo);
            return IsStruct(variableType);
        }

        public bool IsStruct(Type variableType)
        {
            return variableType.IsValueType && (variableType.IsPrimitive == false && variableType.IsEnum == false);
        }

        public bool IsList(Type componentType)
        {
            return componentType.IsGenericType && componentType.GetGenericTypeDefinition() == typeof(List<>);
        }

        public bool IsReferenceType(MemberInfo memberInfo)
        {
            var variableType = GetVariableType(memberInfo);
            return IsReferenceType(variableType);
        }

        public bool IsReferenceType(Type variableType)
        {
            return variableType.IsClass;
        }

        public bool IsCollectionType(MemberInfo variable)
        {
            var variableType = GetVariableType(variable);
            return IsCollectionType(variableType);
        }

        /// <summary>
        /// Arrays return false
        /// </summary>
        /// <param name="variableType"></param>
        /// <returns></returns>
        public bool IsCollectionType(Type variableType)
        {
            return ImplementsInterface(variableType, "ICollection`1") || ImplementsInterface(variableType, "ICollection");
        }


        public Type GetVariableType(MemberInfo info)
        {
            if (info is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)info;
                return propertyInfo.PropertyType;
            }
            if (info is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)info;
                return fieldInfo.FieldType;
            }
            throw new Exception("Sorry, I couldn't find out the Variable Type");
        }

        #region Singles
        public static MethodInfo GetNonPublicMethodInBaseClasses(Type type, string name, bool excludeOverriddenMethods = true)
        {
            Type baseType = type;
            while (baseType != typeof(object) && baseType != typeof(Component) && baseType != typeof(Behaviour))
            {
                var methodFound = baseType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

                //Obviously, if we didn't find anything, don't add nulls to my precious result list.
                if (methodFound != null)
                {
                    return methodFound;
                }

                baseType = baseType.BaseType;
                if (baseType == null) break;
            }

            return null;
        }

        public static FieldInfo GetNonPublicFieldInBaseClasses(Type type, string name,
            bool excludeOverriddenMethods = true)
        {
            Type baseType = type;
            while (baseType != typeof(object) && baseType != typeof(Component) && baseType != typeof(Behaviour))
            {
                var fieldFound = baseType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

                //Obviously, if we didn't find anything, don't add nulls to my precious result list.
                if (fieldFound != null)
                {
                    return fieldFound;
                }

                baseType = baseType.BaseType;
                if (baseType == null) break;
            }

            return null;
        }

        public static PropertyInfo GetNonPublicPropertyInBaseClasses(Type type, string name,
            bool excludeOverriddenMethods = true)
        {
            Type baseType = type;
            while (baseType != typeof(object) && baseType != typeof(Component) && baseType != typeof(Behaviour))
            {
                var fieldFound = baseType.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);

                //Obviously, if we didn't find anything, don't add nulls to my precious result list.
                if (fieldFound != null)
                {
                    return fieldFound;
                }

                baseType = baseType.BaseType;
                if (baseType == null) break;
            }

            return null;
        }

        #endregion

        public static List<MethodInfo> GetNonPublicMethodsInBaseClasses(Type type, bool excludeOverriddenMethods = true)
        {
            List<MethodInfo> result = new List<MethodInfo>();

            HashSet<string> methodsFoundSoFar = new HashSet<string>();

            Type baseType = type;
            while (baseType != typeof(object) && baseType != typeof(Component) && baseType != typeof(Behaviour))
            {
                var methodsFound = baseType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).ToList();

                //Obviously, if we didn't find anything, don't add nulls to my precious result list.
                if (methodsFound.Any())
                {
                    //Add the methods that have a name that I've never seen before. (If I have, they are probabily overrides)
                    IEnumerable<MethodInfo> methodsToAdd = methodsFound;
                    if (excludeOverriddenMethods)
                    {
                        methodsToAdd = methodsFound.Where(m => methodsFoundSoFar.Contains(m.Name) == false);
                    }

                    result.AddRange(methodsToAdd);
                    methodsFoundSoFar.UnionWith(methodsToAdd.Select(m => m.Name));
                }

                baseType = baseType.BaseType;
                if (baseType == null) break;
            }

            return result;
        }


        public static List<PropertyInfo> GetNonPublicPropertiesInBaseClasses(Type type,
            bool excludeOverriddenMethods = true)
        {
            List<PropertyInfo> result = new List<PropertyInfo>();

            HashSet<string> propertiesFoundSoFar = new HashSet<string>();

            Type baseType = type;
            while (baseType != typeof(object) && baseType != typeof(Component) && baseType != typeof(Behaviour))
            {
                var propertiesFound = baseType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).ToList();

                //Obviously, if we didn't find anything, don't add nulls to my precious result list.
                if (propertiesFound.Any())
                {
                    //Add the methods that have a name that I've never seen before. (If I have, they are probabily overrides)
                    IEnumerable<PropertyInfo> propertiesToAdd = propertiesFound;
                    if (excludeOverriddenMethods)
                    {
                        propertiesToAdd = propertiesFound.Where(m => propertiesFoundSoFar.Contains(m.Name) == false);
                    }

                    result.AddRange(propertiesToAdd);
                    propertiesFoundSoFar.UnionWith(propertiesToAdd.Select(m => m.Name));
                }

                baseType = baseType.BaseType;
                if (baseType == null) break;
            }

            return result;
        }
        static List<FieldInfo> result = new List<FieldInfo>();
        static HashSet<string> fieldsFoundSoFar = new HashSet<string>();
        
        //這個會把baseClass的field都找出來
        public static List<FieldInfo> GetNonPublicFieldsInBaseClasses(Type type, bool excludeOverriddenMethods = true)
        {
            result.Clear();
            fieldsFoundSoFar.Clear();
            Type baseType = type;
            while (baseType != typeof(object) && baseType != typeof(MonoBehaviour) && baseType != typeof(Component) && baseType != typeof(Behaviour))
            {
                var fieldsFound = baseType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(prop => prop.FieldType.IsPrimitive == false);

                //Obviously, if we didn't find anything, don't add nulls to my precious result list.
                if (fieldsFound.Any())
                {
                    //Add the methods that have a name that I've never seen before. (If I have, they are probabily overrides)
                    IEnumerable<FieldInfo> fieldsToAdd = fieldsFound;
                    if (excludeOverriddenMethods)
                    {
                        fieldsToAdd = fieldsFound.Where(m => fieldsFoundSoFar.Contains(m.Name) == false);
                    }

                    result.AddRange(fieldsToAdd);
                    // fieldsFoundSoFar.UnionWith(fieldsToAdd.Select(m => m.Name));
                }

                baseType = baseType.BaseType;
                if (baseType == null) break;
            }

            return result;
        }

        /// <summary>
        /// Finds a non-public field in the type hierarchy by field name
        /// </summary>
        /// <param name="type">The starting type to search from</param>
        /// <param name="fieldName">The name of the field to find</param>
        /// <returns>The FieldInfo if found, null otherwise</returns>
        public static FieldInfo FindNonPublicFieldInBaseClasses(Type type, string fieldName)
        {
            Type baseType = type;
            while (baseType != typeof(object) && baseType != typeof(MonoBehaviour) && baseType != typeof(Component) && baseType != typeof(Behaviour))
            {
                var fieldInfo = baseType.GetField(fieldName,BindingFlags.Public| BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (fieldInfo != null)
                {
                    return fieldInfo;
                }

                baseType = baseType.BaseType;
                if (baseType == null) break;
            }

            return null;
        }
        
        /// <summary>
        /// Finds a non-public field in the object's type hierarchy by field name
        /// </summary>
        /// <param name="obj">The object instance to search from</param>
        /// <param name="fieldName">The name of the field to find</param>
        /// <returns>The FieldInfo if found, null otherwise</returns>
        public static FieldInfo FindNonPublicFieldInBaseClasses(object obj, string fieldName)
        {
            if (obj == null)
                return null;
                
            return FindNonPublicFieldInBaseClasses(obj.GetType(), fieldName);
        }

        /// <summary>
        /// Uses reflection to call a method in a class. It will aggressively search all methods, and will be successful even if the method is a private AND in a base/upper class.
        /// This thing is really powerful. It is not cheap on performances, and is best only used for testing. It also breaks most OOP rules.
        /// Use it sparingly
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        public bool CallMethod<T>(T instance, string methodName) where T : class
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                //let's try to see if it's a private method declared in one of the parent classes
                method = GetNonPublicMethodInBaseClasses(instance.GetType(), methodName);
            }

            if (method != null)
            {
                method.Invoke(instance, null);
                return true;
            }
            return false;
        }

    }

}