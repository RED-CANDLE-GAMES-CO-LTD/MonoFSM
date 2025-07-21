/* Author: Oran Bar
 * This class contains the shared code for Auto attributes that fetch arrays of multiple components of the same type
 */

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Auto.Utils;
using System.Collections.Generic;

public static class Extension
{
    public static bool IsList(this System.Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }
}


public abstract class AutoFamilyAttribute : AbstractAutoAttribute, IAutoAttribute
{
    private const string MonoBehaviourNameColor = "green";
    private static ReflectionHelperMethods Rhm = new ReflectionHelperMethods();
    private bool logErrorIfMissing = true;

    public Type LimitedType; //想要撈interface
    // private Component targetComponent;

    public AutoFamilyAttribute(bool getMadIfMissing = true)
    {
        this.logErrorIfMissing = getMadIfMissing;
    }

    public override bool Execute(MonoBehaviour mb, FieldInfo field)
    {
        var componentType = field.FieldType;
        GameObject go = mb.gameObject;

        if (componentType.IsArray)
        {
            return AssignArray(mb, go, field);
        }
        else if (
            Rhm.IsList(componentType))
        {
            // Can't handle lists without using dynamic keyword. 
            // Arrays will have to be enough.
            // return false;
            Debug.LogError("List is not allowed! use Array" + mb + "." + field.Name, go);
            return AssignList(mb, go, field);
        }
        else
        {
            field.SetValue(mb, GetTheSingleComponent(mb, componentType));
            // setVariable(mb, GetTheSingleComponent(mb, componentType));
            return true;
        }
    }

    public abstract object GetTheSingleComponent(MonoBehaviour mb, Type componentType);

    // protected abstract string GetMethodName();
    protected abstract object[] GetComponents(MonoBehaviour mb, GameObject go, Type componentType);

    public object[] GetComponentsToReference(MonoBehaviour mb, GameObject go, Type componentType)
    {
        Type listElementType = AutoUtils.GetElementType(componentType);
        var comps = GetComponents(mb, go, listElementType);
        ;
        return comps;
        // MethodInfo method = typeof(GameObject).GetMethods()
        //     .First(m =>
        //     {
        //         bool result = true;
        //         result = result && m.Name == GetMethodName();
        //         result = result && m.IsGenericMethod;
        //         result = result && m.GetParameters().Length == 1;
        //         result = result && m.GetParameters()[0].ParameterType == typeof(bool);
        //         return result;
        //     });
        // //we want to pass true as arg, to get from inactive objs too
        // MethodInfo generic = method.MakeGenericMethod(listElementType);
        // object[] componentsToReference = generic.Invoke(go, new object[] { true }) as object[];

        // return componentsToReference;
    }
    // private object[] GetComponentsToReferenceOld(MonoBehaviour mb, GameObject go, Type componentType)
    // {
    //     Type listElementType = AutoUtils.GetElementType(componentType);
    //     MethodInfo method = typeof(GameObject).GetMethods()
    //         .First(m =>
    //         {
    //             bool result = true;
    //             result = result && m.Name == GetMethodName();
    //             result = result && m.IsGenericMethod;
    //             result = result && m.GetParameters().Length == 1;
    //             result = result && m.GetParameters()[0].ParameterType == typeof(bool);
    //             return result;
    //         });
    //     //we want to pass true as arg, to get from inactive objs too
    //     MethodInfo generic = method.MakeGenericMethod(listElementType);

    //     // foreach (Transform t in go.transform)
    //     // {
    //     //     object[] comps = generic.Invoke(t.gameObject, new object[] { true }) as object[];
    //     // }

    //     object[] componentsToReference = generic.Invoke(go, new object[] { true }) as object[];

    //     return componentsToReference;
    // }

    private bool AssignArray(MonoBehaviour mb, GameObject go, FieldInfo field)
    {
        var componentType = field.FieldType;
        object[] componentsToReference = GetComponentsToReference(mb, go, componentType);
        // object[] componentsToReferenceOld = GetComponentsToReferenceOld(mb, go, componentType);
        // Debug.Log("AutoAssign:" + componentsToReference, go);
        // Debug.Log("AutoAssign:" + componentsToReferenceOld, go);
        if (logErrorIfMissing && componentsToReference.Length == 0)
        {
            Debug.LogError(
                string.Format(
                    "[Auto]: <color={3}><b>{1}</b></color> couldn't find any components <color=#cc3300><b>{0}</b></color> on <color=#e68a00>{2}.</color>",
                    componentType.Name, mb.GetType().Name, go.name, MonoBehaviourNameColor)
                , go);
            return false;
        }

        field.SetValue(mb, componentsToReference);
        // setVariable(mb, (dynamic)componentsToReference);
        return true;
    }

    private bool AssignList(MonoBehaviour mb, GameObject go, FieldInfo field)
    {
        var componentType = field.FieldType;
        object[] componentsToReference = GetComponentsToReference(mb, go, componentType);
        // var d1 = typeof(List<>);
        // Type[] typeArgs = { componentType };
        // var makeme = d1.MakeGenericType(typeArgs);
        // object o = Activator.CreateInstance(makeme);

        if (logErrorIfMissing && componentsToReference.Length == 0)
        {
            Debug.LogError(
                string.Format(
                    "[Auto]: <color={3}><b>{1}</b></color> couldn't find any components <color=#cc3300><b>{0}</b></color> on <color=#e68a00>{2}.</color>",
                    componentType.Name, mb.GetType().Name, go.name, MonoBehaviourNameColor)
                , go);
            return false;
        }

        var list = new List<dynamic>(componentsToReference);
        field.SetValue(mb, list);
        return true;
    }
}