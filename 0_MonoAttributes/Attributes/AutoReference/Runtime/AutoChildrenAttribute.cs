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
 *f
 *		//[...]
 * }
 *
 */


using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

// [AttributeUsage(AttributeTargets.Field)]
[IncludeMyAttributes]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public class AutoChildrenAttribute : AutoFamilyAttribute
{
    // public bool runtimeIgnore = false; //FIXME: 之後如果想要做全Serialized的
    public bool DepthOneOnly = false; //只找一層, 且不找本身
    public bool _isSelfInclude = false; //default 要true嗎..但現在會讓DepthOneOnly的壞掉造成遞迴？

    /// <summary>
    /// 關著的節點也要撈出來
    /// </summary>
    public bool includeInactive = true;

    /// <summary>
    /// 遞迴抓取時，遇到掛著此 Component Type 的節點就停止往下鑽（scope boundary）。
    /// 用於 nested MonoObj：讓 root 的 scope 只收直屬 scope 內的 component，nested child 的 scope 由 child 自己管。
    /// null = 無邊界（原本行為）。
    /// </summary>
    public Type StopAtType = null;

    /// <summary>
    /// 搭配 <see cref="StopAtType"/>：邊界節點本身是否納入結果。
    /// true 適合用來抓「直屬 child boundary node」的情境。
    /// </summary>
    public bool IncludeStopNode = false;

    public AutoChildrenAttribute(bool logMissingAsError = false)
        : base(logMissingAsError) { }

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
                if (comp != null)
                    return comp;
                // all.AddRange(result);
            }

            return null;
        }

        if (StopAtType != null)
        {
            var targetType = LimitedType ?? componentType;
            return CollectWithBoundarySingle(mb.transform, targetType, StopAtType, IncludeStopNode, isRoot: true);
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

            //自己這層也找一下
            Component[] result;
            if (_isSelfInclude)
            {
                result = mb.transform.GetComponents(LimitedType ?? componentType);
                all.AddRange(result);
            }
            //只從children找
            foreach (Transform t in mb.transform)
            {
                result = t.GetComponents(LimitedType ?? componentType);
                all.AddRange(result);
            }

            var dest = Array.CreateInstance(componentType, all.Count);
            Array.Copy(all.ToArray(), dest, all.Count);
            return dest as object[];
        }

        if (StopAtType != null)
        {
            var targetType = LimitedType ?? componentType;
            var list = new List<Component>();
            CollectWithBoundary(mb.transform, targetType, StopAtType, IncludeStopNode, includeInactive, list, isRoot: true);
            var dest = Array.CreateInstance(componentType, list.Count);
            Array.Copy(list.ToArray(), dest, list.Count);
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
        return destinationArray as object[]; //Array.ConvertAll(results, item => Convert.ChangeType(item, componentType));
    }

    /// <summary>
    /// Scope-aware DFS：遞迴蒐集 target component，遇到 boundary 節點時根據 includeBoundary 決定是否收該節點自己，然後停止往下鑽。
    /// root 本身一定會被掃（不視為 boundary）。
    /// </summary>
    private static void CollectWithBoundary(Transform t, Type targetType, Type boundary, bool includeBoundary, bool includeInactive, List<Component> output, bool isRoot)
    {
        if (!includeInactive && !t.gameObject.activeInHierarchy)
            return;

        if (!isRoot && t.GetComponent(boundary) != null)
        {
            if (includeBoundary)
            {
                var comps = t.GetComponents(targetType);
                if (comps != null && comps.Length > 0) output.AddRange(comps);
            }
            return; //boundary 的子樹不進入
        }

        var own = t.GetComponents(targetType);
        if (own != null && own.Length > 0) output.AddRange(own);

        foreach (Transform child in t)
            CollectWithBoundary(child, targetType, boundary, includeBoundary, includeInactive, output, isRoot: false);
    }

    private static object CollectWithBoundarySingle(Transform t, Type targetType, Type boundary, bool includeBoundary, bool isRoot)
    {
        if (!isRoot && t.GetComponent(boundary) != null)
        {
            if (includeBoundary)
            {
                var comp = t.GetComponent(targetType);
                if (comp != null) return comp;
            }
            return null; //boundary 的子樹不進入
        }

        var own = t.GetComponent(targetType);
        if (own != null) return own;

        foreach (Transform child in t)
        {
            var found = CollectWithBoundarySingle(child, targetType, boundary, includeBoundary, isRoot: false);
            if (found != null) return found;
        }
        return null;
    }
}
