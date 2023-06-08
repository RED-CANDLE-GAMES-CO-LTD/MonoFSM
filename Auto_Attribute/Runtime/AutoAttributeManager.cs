/* Author: Oran Bar
 * Summary: 
 * 
 * This class executes the code to automatically set the references of the variables with the Auto attribute.
 * The code is executed at the beginning of the scene, 500 milliseconds before other Awake calls. (This is done using the ScriptTiming attribute, and can be changed manually)
 * Afterwards, all Auto variables will be assigned, and, in case of errors, [Auto] will log on the console with more info.
 
 * AutoAttributeManager will sneak into your scene upon saving it. 
 * Don't be afraid of this little script. Apart from setting a few [Auto] variables, It's harmless. 
 * Let him live happly in your scene. You'll learn to like him.
 * 
 * If the #define DEB on top of this script is uncommented, Auto will log data about its performance in the console.
 * 
 * Copyrights to Oran Bar™
 */


#define DEB
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Auto.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class FieldCache
{
    public static Dictionary<Type, IEnumerable<FieldInfo>> fieldDict = new Dictionary<Type, IEnumerable<FieldInfo>>();
    public static Dictionary<FieldInfo, object[]> attributeDict = new Dictionary<FieldInfo, object[]>();
    static FieldCache()
    {

    }
    public static void Clear()
    {
        fieldDict.Clear();
        attributeDict.Clear();
    }
}

[Auto.Utils.ScriptTiming(-20000)]
public class AutoAttributeManager : MonoBehaviour
{
    // public bool IsFindAllBehavior = true;
    private List<MonoBehaviour> monoBehavioursInSceneWithAuto = new List<MonoBehaviour>();

    private void Awake()
    {
        SweepScene();
    }

    //async版本的auto
    public static async UniTask AsyncAutoReferenceAllChildren(GameObject targetGo)
    {
        int startFrame = Time.frameCount;
        var componentsInChildren = targetGo.GetComponentsInChildren<MonoBehaviour>(true);
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var mono in componentsInChildren)
        {
            AutoReference(mono);

            if (stopwatch.Elapsed.TotalSeconds >= 0.016f) // Maximum time per frame in seconds (60fps)
            {
                await UniTask.Yield(targetGo.GetCancellationTokenOnDestroy());

                stopwatch.Reset();
                stopwatch.Start();
            }

#if UNITY_EDITOR
            Debug.Log("AsyncAutoReferenceAllChildren" + mono.name + ",frame:" + (Time.frameCount - startFrame));
#endif
        }

        stopwatch.Stop();
    }

    public static void AutoReference(GameObject targetGo)
    {
        AutoReference(targetGo, out _, out _);
    }

    public static void AutoReference(MonoBehaviour mb)
    {
        AutoReference(mb, out _, out _);
    }
    public static void AutoReferenceAllChildren(GameObject targetGo)//把所有的children都綁看看
    {
        var monos = targetGo.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mono in monos)
        {
            AutoReference(mono);
        }
    }

    public static void AutoReference(GameObject targetGo, out int successfulAssigments, out int failedAssignments)
    {
        successfulAssigments = 0;
        failedAssignments = 0;
        var comps = targetGo.GetComponents<MonoBehaviour>(true);
        foreach (var mb in comps)
        {
            AutoReference(mb, out int successes, out int failures);
            successfulAssigments += successes;
            failedAssignments += failures;
        }
    }

    // void setValue(MonoBehaviour mb, object val){

    // }
    public static void AutoReference(MonoBehaviour targetMb, out int successfullyAssigments, out int failedAssignments)
    {
        successfullyAssigments = 0;
        failedAssignments = 0;
        if (targetMb == null) return;
        // var fieldCount = 0;
        // var propCount = 0;
        //Fields
        IEnumerable<FieldInfo> fields = GetFieldsWithAuto(targetMb);
        var attributeDict = FieldCache.attributeDict;

        foreach (var field in fields)
        {
            if (!attributeDict.ContainsKey(field))
                attributeDict[field] = field.GetCustomAttributes(typeof(IAutoAttribute), true);

            var attributes = attributeDict[field];
            //TODO: 這個也可以cache with dict
            // var attributes = 
            foreach (IAutoAttribute autoAttribute in attributes)
            {
                var result = autoAttribute.Execute(targetMb, field);
                if (result)
                {
                    successfullyAssigments++;
                }
                else
                {
                    failedAssignments++;
                }
            }
        }

        //Properties
        // IEnumerable<PropertyInfo> properties = GetPropertiesWithAuto(targetMb);

        // foreach (var prop in properties)
        // {
        //     foreach (IAutoAttribute autofind in prop.GetCustomAttributes(typeof(IAutoAttribute), true))
        //     {
        //         bool result = autofind.Execute(targetMb, prop.PropertyType, (mb, val) => prop.SetValue(mb, val));
        //         propCount++;
        //         if (result)
        //         {
        //             successfullyAssigments++;
        //         }
        //         else
        //         {
        //             failedAssignments++;
        //         }
        //     }
        // }

        // UnityEngine.Debug.Log("[Auto Ref] fieldCount:" + fieldCount + "propCount:" + propCount);
    }
    [Button("Clear Cache")]
    void Clear()
    {
        FieldCache.Clear();
    }
    [Button("Bind")]
    public void SweepScene()
    {
        // fieldDict.Clear();
#if DEB
        Stopwatch sw = new Stopwatch();

        sw.Start();
#endif
        IEnumerable<MonoBehaviour> monoBehaviours = null;
        // if (monoBehavioursInSceneWithAuto?.Any() != true)
        // {
        //     //Fallback if, for some reason, the monobehaviours were not previously cached
        monoBehaviours = GetAllMonobehavioursWithAuto();
        // }
        // else
        // {
        //     monoBehaviours = monoBehavioursInSceneWithAuto;
        // }
#if DEB
        sw.Stop();
        UnityEngine.Debug.LogFormat($"[Auto] Find Mono: {sw.ElapsedMilliseconds} milliseconds");
        sw.Reset();
        sw.Start();
#endif
        // var autoCaches = GetAllAutoCaches();
        //TODO: 如果monoBehaviour已經在autoCaches裡就不需要跑了?

        int autoVarialbesAssigned_count = 0;
        int autoVarialbesNotAssigned_count = 0;
        // var dict = new Dictionary<Type, int>();
        foreach (var mb in monoBehaviours)
        {
            // var type = mb.GetType();
            // if (!dict.TryAdd(type, 1))
            // {
            //     dict[type]++;
            // }
            // var stopwatch = new Stopwatch();
            // stopwatch.Start();
            AutoReference(mb, out int succ, out int fail);
            autoVarialbesAssigned_count += succ;
            autoVarialbesNotAssigned_count += fail;
            // stopwatch.Stop();
            // if (stopwatch.ElapsedMilliseconds > 0)
            // Debug.LogFormat($"[Auto] Ref: {mb}:{stopwatch.ElapsedMilliseconds} milliseconds");
            // stopwatch.Reset();
            // stopwatch.Start();
        }
        // foreach (var item in dict)
        // {
        //     UnityEngine.Debug.Log("[class]" + item.Key + ",count:" + item.Value);
        // }

#if DEB
        sw.Stop();

        // int variablesAnalized = monoBehaviours
        //     .Select(mb => mb.GetType())
        //     .Aggregate(0, (agg, mbType) =>
        //         agg = agg + mbType.GetFields().Count() //+ mbType.GetProperties().Count()
        //     );

        // int variablesWithAuto = monoBehaviours
        //     .Aggregate(0, (agg, mb) =>
        //         agg = agg + GetFieldsWithAuto(mb).Count() //+ GetPropertiesWithAuto(mb).Count()
        //     );

        string result_color = (autoVarialbesNotAssigned_count > 0) ? "red" : "green";
        //autoVarialbesAssigned_count + autoVarialbesNotAssigned_count
        // UnityEngine.Debug.LogFormat($"[Auto] Assigned <color={result_color}><b>{autoVarialbesAssigned_count}/{variablesWithAuto}</b></color> [Auto*] variables in <color=#cc3300><b>{sw.ElapsedMilliseconds} Milliseconds </b></color> - Analized {monoBehaviours.Count()} MonoBehaviours and {variablesAnalized} variables");
        UnityEngine.Debug.LogFormat($"[Auto] Assigned <color={result_color}><b>{autoVarialbesAssigned_count}/..</b></color> [Auto*] variables in <color=#cc3300><b>{sw.ElapsedMilliseconds} Milliseconds </b></color> - Analized {monoBehaviours.Count()} MonoBehaviours and .. variables");
#endif
    }

    // public void CacheMonobehavioursWithAuto(){
    // 	var start = Time.time;
    // 	monoBehavioursInSceneWithAuto = GetAllMonobehavioursWithAuto().ToList();
    // 	UnityEngine.Debug.Log($"Cached {monoBehavioursInSceneWithAuto.Count} MonoBehaviours in {Time.time - start} mills");
    // }
    private IEnumerable<MonoBehaviour> GetAllAutoCaches()
    {

        IEnumerable<AutoCache> autoCaches = GameObject.FindObjectsOfType<AutoCache>(true)
                .Where(mb => mb.gameObject.scene == this.gameObject.scene);

        // autoCaches = autoCaches.Where(mb => GetFieldsWithAuto(mb).Count() + GetPropertiesWithAuto(mb).Count() > 0);

        return autoCaches;
    }
    private IEnumerable<MonoBehaviour> GetAllMonobehavioursWithAuto()
    {
        Stopwatch sw = new Stopwatch();
        // sw.Start();
        IEnumerable<MonoBehaviour> monoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true)
            .Where(mb => mb != null && mb.gameObject.scene == gameObject.scene);

        // sw.Stop();
        // UnityEngine.Debug.Log("[Auto]: Find All Obj" + sw.ElapsedMilliseconds + ",mb Count:" + monoBehaviours.Count());
        // sw.Reset();
        // sw.Start();
        // monoBehaviours = monoBehaviours.Where(mb => GetFieldsWithAuto(mb).Count() + GetPropertiesWithAuto(mb).Count() > 0);

        //FIXME: 會有null嗎？
        monoBehaviours = monoBehaviours.Where(mb => GetFieldsWithAuto(mb)?.Count() > 0);
        // UnityEngine.Debug.Log("[Auto]: Mono with Fields with auto time:" + sw.ElapsedMilliseconds + ",mb Count:" + monoBehaviours.Count());
        // sw.Stop();

        return monoBehaviours;
    }


    private static IEnumerable<FieldInfo> GetFieldsWithAuto(MonoBehaviour mb)
    {
        if (mb == null)
            return default;
        var t = mb.GetType();
        var fieldDict = FieldCache.fieldDict;
        if (fieldDict.ContainsKey(t))
        {
            // Debug.Log("Cached Field");
            return fieldDict[t];
        }

        // ReflectionHelperMethods rhm = new ReflectionHelperMethods();
        // var list = mb.GetType()
        //             .GetFields(BindingFlags.Instance | BindingFlags.Public).Where(prop => prop.FieldType.IsGenericType && prop.FieldType.GetGenericTypeDefinition() == typeof(List<>));

        var fields =
            t.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(prop => prop.FieldType.IsPrimitive == false)
            .Where(prop => Attribute.IsDefined(prop, typeof(AutoAttribute)) ||
                            Attribute.IsDefined(prop, typeof(AutoChildrenAttribute)) ||
                            Attribute.IsDefined(prop, typeof(AutoParentAttribute)))
            .Concat(
            ReflectionHelperMethods.GetNonPublicFieldsInBaseClasses(t)
        // .Where(prop => prop.FieldType.IsPrimitive == false)
        .Where(prop => Attribute.IsDefined(prop, typeof(AutoAttribute)) ||
                        Attribute.IsDefined(prop, typeof(AutoChildrenAttribute)) ||
                        Attribute.IsDefined(prop, typeof(AutoParentAttribute))
        )
        );
        fieldDict.TryAdd(t, fields.ToList());
        return fields;
    }


    private static IEnumerable<PropertyInfo> GetPropertiesWithAuto(MonoBehaviour mb)
    {
        ReflectionHelperMethods rhm = new ReflectionHelperMethods();

        return mb.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(prop => prop.PropertyType.IsPrimitive == false)
            .Where(prop => Attribute.IsDefined(prop, typeof(AutoAttribute)) ||
                    Attribute.IsDefined(prop, typeof(AutoChildrenAttribute)) ||
                    Attribute.IsDefined(prop, typeof(AutoParentAttribute))
            )
            .Concat(
                rhm.GetNonPublicPropertiesInBaseClasses(mb.GetType())
                .Where(prop => prop.PropertyType.IsPrimitive == false)
                .Where(prop => Attribute.IsDefined(prop, typeof(AutoAttribute)) ||
                        Attribute.IsDefined(prop, typeof(AutoChildrenAttribute)) ||
                        Attribute.IsDefined(prop, typeof(AutoParentAttribute))
                )
            );
    }
}