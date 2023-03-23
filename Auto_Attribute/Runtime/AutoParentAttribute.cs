/* Author: Oran Bar
 * Summary: This attribute automatically assigns a class variable to one of the gameobject's components; if nothing is found, it will continue to look for it going up the scene hiearchy (parents). 
 * It acts as the equivalent of a GetComponentInParent call done in Awake.
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

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AutoParentAttribute : AutoFamily
{
    public AutoParentAttribute(bool getMadIfMissing = true) : base(getMadIfMissing)
    {

    }

    protected override object[] GetComponents(MonoBehaviour mb, GameObject go, Type componentType)
    {
        return mb.GetComponentsInParent(componentType, true) as object[];
    }

    protected override string GetMethodName()
    {
        // return "GetComponentsInParent";
        return "GetComponentsInParent";
    }

    protected override object GetTheSingleComponent(MonoBehaviour mb, Type componentType)
    {
        // Debug.Log("[AutoParent] GetTheSingleComponent" + mb.gameObject + mb.name, mb.gameObject);
        return mb.GetComponentInParent(componentType, true);
    }

}