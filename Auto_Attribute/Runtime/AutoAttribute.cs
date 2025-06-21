/* Author: Oran Bar
 * Summary: This attribute automatically assigns a class variable to one of the gameobject's component.
 * It acts as the equivalent of a GetComponent call done in Awake.
 * Components that Auto has not been able to find are logged as errors in the console.
 * Using [Auto(true)], Auto will log warnings as opposed to errors.
 * This is important because, allowing Auto to log error will result in builds being halted whenever one of the [Auto] variables assignments was unsuccessful.
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
using System.Reflection;
using Auto.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

[IncludeMyAttributes]
[AttributeUsage(AttributeTargets.Field)]
public class AutoAttribute : AutoFamilyAttribute
{
    private const string MonoBehaviourNameColor = "green"; //Changeme

    // Constructor calls the base constructor with the logMissingAsError parameter
    public AutoAttribute(bool logMissingAsError = true) : base(logMissingAsError)
    {
    }

    // Implementation of abstract method required by AutoFamily
    public override object GetTheSingleComponent(MonoBehaviour mb, Type componentType)
    {
        return mb.GetComponent(LimitedType ?? componentType);
    }

    // Implementation of abstract method required by AutoFamily
    protected override object[] GetComponents(MonoBehaviour mb, GameObject go, Type componentType)
    {
        var results = go.GetComponents(LimitedType ?? componentType) as object[];
        var destinationArray = Array.CreateInstance(componentType, results.Length);
        Array.Copy(results, destinationArray, results.Length);
        return destinationArray as object[];
    }

    // LogMissingComponent method is now handled by the base AutoFamily class
    // through its logErrorIfMissing functionality
}