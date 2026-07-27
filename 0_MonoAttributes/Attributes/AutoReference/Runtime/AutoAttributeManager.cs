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


// #define DEB

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Auto_Attribute.Runtime;
using Auto.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
// using Cysharp.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[ScriptTiming(-20000)]
public class AutoAttributeManager : MonoBehaviour
{
    public static IEnumerable<MonoBehaviour> GetAllMonoBehavioursOfCurrentScene()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        Debug.Log("Roots:" + roots.Length);
        var monos = roots.SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true));
        //只找有
        monos = monos.Where(mb => GetFieldsWithAutoAndBuildCache(mb)?.Count() > 0);
        return monos;
    }

    public static void BuildFieldCache(MonoBehaviour[] monos)
    {
        foreach (var mono in monos)
            GetFieldsWithAutoAndBuildCache(mono);
    }

    // [PropertyOrder(-1)]
    // [Button]
    // public void StoreReferenceCache() //Editor time
    // {
    //     SweepScene();
    //     monoValueCaches.Clear();
    //     var monos = GetAllMonoBehavioursWithAuto();
    //     foreach (var mono in monos)
    //     {
    //         var cache = new MonoValueCache();
    //         var fetchCount = cache.CopyFieldsToCache(mono);
    //         if (fetchCount > 0)
    //             monoValueCaches.Add(cache);
    //     }
    // }
    //
    // [PropertyOrder(-1)]
    // [Button]
    // public void RestoreReferenceCacheToMonos() //Runtime
    // {
    //     Debug.Log("GetAllMonoBehavioursWithAuto start:" + FieldCache.fieldDictByName.Count);
    //     var monos = AutoAttributeManager.GetAllMonoBehavioursOfCurrentScene(); //建立dict
    //     Debug.Log("GetAllMonoBehavioursWithAuto end:" + FieldCache.fieldDictByName.Count);
    //     for (var i = 0; i < monoValueCaches.Count(); i++)
    //     {
    //         monoValueCaches[i].CopyCacheToFields();
    //     }
    // }
    // // public bool IsFindAllBehavior = true;
    // private List<MonoBehaviour> monoBehavioursInSceneWithAuto = new List<MonoBehaviour>();

    [ShowInInspector]
    private int GetAllGameObjectCount()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        return roots.SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Count();
    }

    [Title("Reference Cache")]
    [SerializeField]
    [Tooltip("勾選後 Awake 只讀 cache 不重爬 scene；Editor 端的 scene-open/domain-reload sweep 也會跳過。需要先按 Bake Reference Cache。")]
    private bool _useCacheOnly;

    public bool UseCacheOnly => _useCacheOnly;

    public MonoReferenceCache monoReferenceCache = new();

    [PropertyOrder(-2)]
    [Button("Bake Reference Cache", ButtonSizes.Large)]
    [GUIColor(0.6f, 1f, 0.6f)]
    private void BakeReferenceCache()
    {
        monoReferenceCache.RootObj = null; // 用整個 active scene
        monoReferenceCache.SaveReferenceCache();
        Debug.Log(
            $"[Auto] Bake done. monoValueCaches:{monoReferenceCache.MonoValueCachesCount}, monos:{monoReferenceCache.CachedMonoBehavioursCount}",
            this
        );
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    [PropertyOrder(-2)]
    [Button("Clear Reference Cache", ButtonSizes.Large)]
    [GUIColor(1f, 0.7f, 0.7f)]
    [InfoBox(
        "_useCacheOnly 開著時清掉 cache，Awake 會 fallback 成 SweepScene。",
        InfoMessageType.Warning,
        nameof(_useCacheOnly)
    )]
    private void ClearReferenceCache()
    {
        var before = monoReferenceCache.MonoValueCachesCount;
        monoReferenceCache.ClearRefs();
        Debug.Log($"[Auto] Reference cache cleared. monoValueCaches:{before} -> 0", this);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void Awake()
    {
        if (_useCacheOnly)
        {
            if (
                monoReferenceCache?.CachedMonoBehaviours == null
                || monoReferenceCache.CachedMonoBehaviours.Length == 0
            )
            {
                Debug.LogWarning(
                    "[Auto] _useCacheOnly=true 但沒有 cache，fallback 用 SweepScene。請先按 Bake Reference Cache。",
                    this
                );
                SweepScene();
                return;
            }

            monoReferenceCache.RestoreReferenceCacheToMonoFields();
            return;
        }

        SweepScene();
    }

    private void OnDestroy()
    {
        // monoReferenceCache.ClearRefs();
    }

    //async版本的auto
    //FIXME: 真的會用到這個嗎？ async auto ref, 感覺bottle neck不在這
    //     public static async UniTask AsyncAutoReferenceAllChildren(GameObject targetGo)
    //     {
    //         var startFrame = Time.frameCount;
    //         var componentsInChildren = targetGo.GetComponentsInChildren<MonoBehaviour>(true);
    //         var stopwatch = new Stopwatch();
    //         stopwatch.Start();
    //
    //         foreach (var mono in componentsInChildren)
    //         {
    //             AutoReference(mono);
    //
    //             if (stopwatch.Elapsed.TotalSeconds >= 0.016f) // Maximum time per frame in seconds (60fps)
    //             {
    //                 await UniTask.Yield(targetGo.GetCancellationTokenOnDestroy());
    //
    //                 stopwatch.Reset();
    //                 stopwatch.Start();
    //             }
    //
    // #if UNITY_EDITOR
    //             Debug.Log("AsyncAutoReferenceAllChildren" + mono.name + ",frame:" + (Time.frameCount - startFrame));
    // #endif
    //         }
    //
    //         stopwatch.Stop();
    //     }

    public static void AutoReference(GameObject targetGo)
    {
        AutoReference(targetGo, out _, out _);
    }

    /// <summary>
    /// Assigns all Auto variables on the given MonoBehaviour
    /// </summary>
    /// <param name="mb">The MonoBehaviour to assign the Auto variables of</param>
    public static void AutoReference(MonoBehaviour mb)
    {
        AutoReference(mb, out _, out _);
    }

    public static void AutoReferenceAllChildren(GameObject targetGo) //把所有的children都綁看看
    {
        var monos = targetGo.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mono in monos)
            AutoReference(mono);
    }

    public static void AutoReference(
        GameObject targetGo,
        out int successfulAssigments,
        out int failedAssignments
    )
    {
        successfulAssigments = 0;
        failedAssignments = 0;
        //先把所有的
        var comps = targetGo.GetComponents<MonoBehaviour>(true);
        foreach (var mb in comps)
        {
            AutoReference(mb, out var successes, out var failures);
            successfulAssigments += successes;
            failedAssignments += failures;
        }
    }

    private void AutoResolveField(
        MonoBehaviour mb,
        [System.Runtime.CompilerServices.CallerMemberName] string fieldName = ""
    )
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            AutoAttributeManager.AutoReferenceFieldEditor(mb, fieldName);
        }
#endif
    }

    [Conditional("UNITY_EDITOR")]
    public static void AutoReferenceFieldEditor(MonoBehaviour mb, string fieldName)
    {
        if (Application.isPlaying)
            return;
        // success = false;
        if (mb == null || string.IsNullOrEmpty(fieldName))
            return;
        var t = mb.GetType();

        var fieldDictByName = FieldCache.fieldDictByName;

        //先看看有沒有這個type的內容，沒有的話重新拉
        if (!fieldDictByName.ContainsKey((t, fieldName)))
        {
            GetFieldsWithAutoAndBuildCache(mb);
        }

        if (!fieldDictByName.TryGetValue((t, fieldName), out var field))
        {
            Debug.LogError("[Auto] Cannot find field " + fieldName + " in " + t, mb);
            return;
        }

        var attributes = field.GetCustomAttributes(typeof(IAutoAttribute), true);
        foreach (IAutoAttribute autoAttribute in attributes)
        {
            autoAttribute.Execute(mb, field);
            // success =  || success;
        }
    }

    private static void AutoReference(
        MonoBehaviour targetMb,
        out int successfullyAssignments,
        out int failedAssignments
    )
    {
        successfullyAssignments = 0;
        failedAssignments = 0;
        if (targetMb == null)
            return;
        // var fieldCount = 0;
        // var propCount = 0;
        //Fields
        var fields = GetFieldsWithAutoAndBuildCache(targetMb);
        var attributeDict = FieldCache.attributeDict;

        foreach (var field in fields)
        {
            if (!attributeDict.ContainsKey(field))
                attributeDict[field] = field.GetCustomAttributes(typeof(IAutoAttribute), true);

            //FIXME: nested class悲劇..
            // if (field.FieldType.IsAssignableFrom(typeof(IAutoAttributeClass)))
            // {
            //     //
            //     var obj = field.GetValue(targetMb);
            // }
            //FIXME: 還是讓全世界都serialize就好了？
            // if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)))
            // {
            //     continue; //skip serialized fields
            // }

            var attributes = attributeDict[field];
            //TODO: 這個也可以cache with dict
            // var attributes =
            foreach (IAutoAttribute autoAttribute in attributes)
            {
                var result = autoAttribute.Execute(targetMb, field);
                if (result)
                    successfullyAssignments++;
                else
                    failedAssignments++;
            }
        }
    }

    [Button("Clear Cache")]
    private void Clear()
    {
        FieldCache.Clear();
    }

    public static void AutoReferenceAll(IEnumerable<MonoBehaviour> monos)
    {
        var sw = new Stopwatch();
        sw.Start();
        var autoVariablesAssignedCount = 0;
        var autoVariablesNotAssignedCount = 0;
        var monoBehaviours = monos as MonoBehaviour[] ?? monos.ToArray();
        foreach (var mono in monoBehaviours)
        {
            AutoReference(mono, out var succ, out var fail);
            autoVariablesAssignedCount += succ;
            autoVariablesNotAssignedCount += fail;
        }

        sw.Stop();
        var result_color = autoVariablesNotAssignedCount > 0 ? "red" : "green";
        Debug.LogFormat(
            $"[Auto] Assigned <color={result_color}><b>{autoVariablesAssignedCount}/..</b></color> [Auto*] variables in <color=#cc3300><b>{sw.ElapsedMilliseconds} Milliseconds </b></color> - Analized {monoBehaviours.Count()} MonoBehaviours and .. variables"
        );
    }

    [Button("Bind")]
    public void SweepScene()
    {
        // fieldDict.Clear();
        //get root gameobject of the scene
        // var root = SceneManager.GetActiveScene().GetRootGameObjects();
#if DEB
        Stopwatch sw = new Stopwatch();

        sw.Start();
#endif
        IEnumerable<MonoBehaviour> monoBehaviours = null;
        // if (monoBehavioursInSceneWithAuto?.Any() != true)
        // {
        //     //Fallback if, for some reason, the monobehaviours were not previously cached
        monoBehaviours = GetAllMonoBehavioursWithAuto();
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

        var autoVarialbesAssigned_count = 0;
        var autoVarialbesNotAssigned_count = 0;
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
            AutoReference(mb, out var succ, out var fail);
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
        UnityEngine.Debug.LogFormat(
            $"[Auto] Assigned <color={result_color}><b>{autoVarialbesAssigned_count}/..</b></color> [Auto*] variables in <color=#cc3300><b>{sw.ElapsedMilliseconds} Milliseconds </b></color> - Analized {monoBehaviours.Count()} MonoBehaviours and .. variables"
        );
#endif
    }

    // public void CacheMonobehavioursWithAuto(){
    // 	var start = Time.time;
    // 	monoBehavioursInSceneWithAuto = GetAllMonobehavioursWithAuto().ToList();
    // 	UnityEngine.Debug.Log($"Cached {monoBehavioursInSceneWithAuto.Count} MonoBehaviours in {Time.time - start} mills");
    // }
    // private IEnumerable<MonoBehaviour> GetAllAutoCaches()
    // {
    //
    //     IEnumerable<AutoCache> autoCaches = GameObject.FindObjectsOfType<AutoCache>(true)
    //             .Where(mb => mb.gameObject.scene == this.gameObject.scene);
    //
    //     // autoCaches = autoCaches.Where(mb => GetFieldsWithAuto(mb).Count() + GetPropertiesWithAuto(mb).Count() > 0);
    //
    //     return autoCaches;
    // }

    public IEnumerable<MonoBehaviour> GetAllMonoBehavioursWithAuto() //(GameObject[] roots)
    {
#if UNITY_EDITOR
        var sw = new Stopwatch();
#endif
        // sw.Start();

        //get all monobehaviours from root
        var roots = gameObject.scene.GetRootGameObjects();
        var monoBehaviours = roots.SelectMany(go =>
            go.GetComponentsInChildren<MonoBehaviour>(true)
        );

        // var monoBehaviours = FindObjectsOfType<MonoBehaviour>(true)
        //     .Where(mb => mb != null && mb.gameObject != null && mb.gameObject.scene == gameObject.scene);

        // sw.Stop();
        // UnityEngine.Debug.Log("[Auto]: Find All Obj" + sw.ElapsedMilliseconds + ",mb Count:" + monoBehaviours.Count());
        // sw.Reset();
        // sw.Start();
        // monoBehaviours = monoBehaviours.Where(mb => GetFieldsWithAuto(mb).Count() + GetPropertiesWithAuto(mb).Count() > 0);

        //FIXME: 會有null嗎？
        monoBehaviours = monoBehaviours.Where(mb =>
            GetFieldsWithAutoAndBuildCache(mb)?.Count() > 0
        );

#if UNITY_EDITOR
        sw.Stop();
        Debug.Log(
            "[Auto]: Mono with Fields with auto time:"
                + sw.ElapsedMilliseconds
                + ",mb Count:"
                + monoBehaviours.Count()
        );
#endif
        return monoBehaviours;
    }

    public static IEnumerable<FieldInfo> GetFieldsWithAutoAndBuildCache(Object mb) //FIXME: 應該吃type, mb誤會
    {
        if (mb == null)
            return default;
        var t = mb.GetType();
        var fieldDict = FieldCache.fieldDict;

        //一個type只需要做一次
        if (fieldDict.TryGetValue(t, out var auto))
            // Debug.Log("Cached Field");
            return auto;

        // ReflectionHelperMethods rhm = new ReflectionHelperMethods();
        // var list = mb.GetType()
        //             .GetFields(BindingFlags.Instance | BindingFlags.Public).Where(prop => prop.FieldType.IsGenericType && prop.FieldType.GetGenericTypeDefinition() == typeof(List<>));

        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(prop => prop.FieldType.IsPrimitive == false)
            .Where(prop =>
                Attribute.IsDefined(prop, typeof(PreventAutoCacheAttribute)) == false
                && (
                    Attribute.IsDefined(prop, typeof(AutoAttribute))
                    || Attribute.IsDefined(prop, typeof(AutoChildrenAttribute))
                    || Attribute.IsDefined(prop, typeof(AutoParentAttribute))
                    || Attribute.IsDefined(prop, typeof(AutoNestedAttribute))
                )
            )
            .Concat(
                ReflectionHelperMethods
                    .GetNonPublicFieldsInBaseClasses(t)
                    // .Where(prop => prop.FieldType.IsPrimitive == false)
                    .Where(prop =>
                        Attribute.IsDefined(prop, typeof(PreventAutoCacheAttribute)) == false
                        && (
                            Attribute.IsDefined(prop, typeof(AutoAttribute))
                            || Attribute.IsDefined(prop, typeof(AutoChildrenAttribute))
                            || Attribute.IsDefined(prop, typeof(AutoParentAttribute))
                            || Attribute.IsDefined(prop, typeof(AutoNestedAttribute))
                        )
                    )
            );
        var fieldsWithAuto = fields as FieldInfo[] ?? fields.ToArray();
        fieldDict.TryAdd(t, fieldsWithAuto.ToList());
        var fieldDictByName = FieldCache.fieldDictByName;
        foreach (var field in fieldsWithAuto)
            fieldDictByName.TryAdd((t, field.Name), field);
        // Debug.Log("Add Field Tuple:" + t + field.Name);
        return fieldsWithAuto;
    }
}
