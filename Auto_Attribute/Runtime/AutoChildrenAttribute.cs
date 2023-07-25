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

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AutoChildrenAttribute : AutoFamily
{
    public bool DepthOneOnly = false;//只找一層

    /// <summary>
    /// 關著的節點也要撈出來
    /// </summary>
    public bool includeInactive = true;

    public AutoChildrenAttribute(bool logMissingAsError = false) : base(logMissingAsError)
    {

    }

    protected override string GetMethodName()
    {
        return "GetComponentsInChildren";
    }

    protected override object GetTheSingleComponent(MonoBehaviour mb, Type componentType)
    {
        //一定是最淺的...hmm

        var result = mb.GetComponentInChildren(componentType, includeInactive);
        if (DepthOneOnly)
        {
            if (result && result.transform.parent != mb.transform)
            {
                return null;
            }
        }

        return result;
    }
    protected override object[] GetComponents(MonoBehaviour mb, GameObject go, Type componentType)
    {
        if (DepthOneOnly)
        {
            // var list = new List<Component>();
            var all = new List<object>();

            var comps = mb.GetComponents(componentType);
            all.AddRange(comps);
            
            foreach (Transform t in mb.transform)
            {
                var result = t.GetComponents(componentType);
                all.AddRange(result);
            }
            Array dest = Array.CreateInstance(componentType, all.Count);
            Array.Copy(all.ToArray(), dest, all.Count);
            return dest as object[];
        }

        var results = mb.GetComponentsInChildren(componentType, includeInactive);
        Array destinationArray = Array.CreateInstance(componentType, results.Length);
        Array.Copy(results, destinationArray, results.Length);
        return destinationArray as object[];//Array.ConvertAll(results, item => Convert.ChangeType(item, componentType));
    }
}