/* Author: Oran Bar
 * Summary: This attribute automatically assigns a class variable to one of the gameobject's components; if nothing is found, it will continue to look for it going down the scene hiearchy (children).
 * It acts as the equivalent of a GetComponentInChildren call done in Awake.
 * Components that Auto has not been able to find are logged as errors in the console.
 * Using [Auto(true)], Auto will log warnings as opposed to errors.
 *
 * Usage example:
 *
 * public class Foo
 * {
 *		[Auto] public BoxCollier myBoxCollier;	//This assigns the variable to the BoxColider attached on your object
 *		[Auto(true)] public Camera myCamera;	//since we passed true as an argument, if the camera is not found, Auto will log a warning as opposed to an error, and won't halt the build.
 *
 *		//[...]
 * }
 *
 */


using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

// [AttributeUsage(AttributeTargets.Field)]
[IncludeMyAttributes]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public class AutoChildrenAttribute : AutoFamilyAttribute
{
    // public bool runtimeIgnore = false; //FIXME: 之後如果想要做全Serialized的
    public bool DepthOneOnly = false; //只找一層, 且不找本身

    /// <summary>
    /// 關著的節點也要撈出來
    /// </summary>
    public bool includeInactive = true;

    public AutoChildrenAttribute(bool logMissingAsError = false) : base(logMissingAsError)
    {
    }

    // protected override string GetMethodName()
    // {
    //     return "GetComponentsInChildren";
    // }

    public override object GetTheSingleComponent(MonoBehaviour mb, Type componentType)
    {
        //一定是最淺的...hmm
        if (DepthOneOnly)
        {
            //只從children找
            foreach (Transform t in mb.transform)
            {
                var comp = t.GetComponent(LimitedType ?? componentType);
                if (comp != null) return comp;
                // all.AddRange(result);
            }

            return null;
        }

        var result = mb.GetComponentInChildren(LimitedType ?? componentType, includeInactive);
        return result;
    }

    protected override object[] GetComponents(MonoBehaviour mb, GameObject go, Type componentType)
    {
        if (DepthOneOnly)
        {
            // var list = new List<Component>();
            var all = new List<object>();

            // var comps = mb.GetComponents(LimitedType ?? componentType);
            // all.AddRange(comps);

            //只從children找
            foreach (Transform t in mb.transform)
            {
                var result = t.GetComponents(LimitedType ?? componentType);
                all.AddRange(result);
            }

            var dest = Array.CreateInstance(componentType, all.Count);
            Array.Copy(all.ToArray(), dest, all.Count);
            return dest as object[];
        }

        // if (TargetType != null)
        // {
        //     Debug.Log("TargetType is not null" + TargetType, mb);
        // }
        // else
        // {
        //     Debug.Log("TargetType is null" + TargetType, mb);
        // }

        var results = mb.GetComponentsInChildren(LimitedType ?? componentType, includeInactive);
        var destinationArray = Array.CreateInstance(componentType, results.Length);
        Array.Copy(results, destinationArray, results.Length);
        return
            destinationArray as object[]; //Array.ConvertAll(results, item => Convert.ChangeType(item, componentType));
    }
}