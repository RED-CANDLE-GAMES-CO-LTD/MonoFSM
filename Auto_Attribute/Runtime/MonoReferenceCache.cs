using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;

public interface IEditorOnly
{
}

public interface IEditorOnlyStrip
{
    public GameObject gameObject { get; }
    public void OnBuildStrip();
}
namespace Auto_Attribute.Runtime
{
    
   
    public class FieldCache
    {
        public static Dictionary<Type, IEnumerable<FieldInfo>> fieldDict = new();
        public static Dictionary<FieldInfo, object[]> attributeDict = new();
        public static Dictionary<Tuple<Type, string>, FieldInfo> fieldDictByName = new();

        public static bool IsAutoAttribute(FieldInfo field)
        {
            if (!attributeDict.ContainsKey(field))
                attributeDict[field] = field.GetCustomAttributes(typeof(IAutoAttribute), true);
            var attributes = attributeDict[field];
            return attributes is { Length: > 0 };
        }

        static FieldCache()
        {
        }

        public static void Clear()
        {
            fieldDict.Clear();
            attributeDict.Clear();
            fieldDictByName.Clear();
        }
    }


    [Serializable]
    [Searchable]
    public class MonoValueCache
    {
        public List<FieldValueCache> fieldCaches = new();

        public int CopyFieldsToCache(MonoBehaviour targetMb)
        {
            var count = 0;
            var fields = FieldCache.fieldDict[targetMb.GetType()];
            foreach (var field in fields)
            {
                var v = field.GetValue(targetMb);
                if (v == null) continue;

                //不是 IAutoFamily
                if (FieldCache.IsAutoAttribute(field) == false)
                {
                    continue;
                }

                var cache = new FieldValueCache();
                if (cache.CopyFieldToCache(targetMb, field, v))
                {
                    count++;
                    fieldCaches.Add(cache);
                }
            }

            return count;
        }

        public void CopyCacheToFields()
        {
            foreach (var cache in fieldCaches)
            {
                cache.CopyCacheToField();
            }
        }
    }

    [Serializable]
    public class FieldValueCache
    {
        public string targetName;

        public string typeName;

        // public FieldInfo field;
        public string fieldName;


        [SerializeField] private MonoBehaviour targetMb;
        [SerializeField] private Component[] valueArray;
        [SerializeField] private Component value;

        public bool CopyFieldToCache(MonoBehaviour targetMb, FieldInfo field, object v)
        {
            this.targetMb = targetMb;
            // this.field = field;

            targetName = targetMb.name;
            typeName = targetMb.GetType().Name;
            fieldName = field.Name;
            if (v.GetType().IsArray)
            {
                var array = v as object[];
                valueArray = Array.ConvertAll(array, x => x as Component);
            }
            else if (v is Component component)
            {
                value = component;
            }
            else if (field.FieldType.IsInterface)
            {
                var interfaceValue = (Component)v;
                if (interfaceValue != null)
                {
                    value = interfaceValue;
                }
                else
                {
                    Debug.LogError("Value is not a Component for the interface type: " + field.FieldType);
                    return false;
                }
            }
            else
            {
                Debug.LogError("Value is not a Component: " + field.FieldType);
                return false;
            }

            return true;
        }


        public void CopyCacheToField()
        {
            if (targetMb == null)
            {
                Debug.LogError(
                    "Target is null fieldName:" + fieldName + ",monoName:" + targetName + ",typeName:" + typeName);
                return;
            }

            var targetMbType = targetMb.GetType();
            var tuple = new Tuple<Type, string>(targetMbType, fieldName);
            if (!FieldCache.fieldDictByName.ContainsKey(tuple))
            {
                Debug.LogError("(editor only?) Field not found in FieldCache  :" + fieldName + ",monoName:" +
                               targetName +
                               ",typeName:" +
                               typeName);
                return;
            }

            var field = FieldCache.fieldDictByName[tuple];
            if (field == null)
            {
                Debug.LogError("Field not found:" + fieldName);
                return;
            }

            if (value != null)
            {
                field.SetValue(targetMb, value);
            }
            else if (valueArray != null && field.FieldType.IsArray)
            {
                var elementType = field.FieldType.GetElementType();
                if (elementType == null)
                {
                    //有可能value是null然後valueArray也不是
                    Debug.LogError(
                        "ElementType is null:" + field.Name + field.FieldType + ",MonoType:" + targetMb.GetType(),
                        targetMb);
                    return;
                }

                var array = Array.CreateInstance(elementType, valueArray.Length);
                for (var i = 0; i < valueArray.Length; i++)
                {
                    try
                    {
                        if (valueArray[i] == null)
                        {
                            Debug.LogError("ValueArray[i] is null: elementType:" + elementType + ",fieldName:" +
                                           fieldName +
                                           ",monoName:" + targetName + ",typeName:" + typeName);
                            continue;
                        }

                        array.SetValue(valueArray[i], i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("CopyCacheToFields Error:" + e + e.StackTrace + "ValueArray[i]" + valueArray[i] +
                                       ",elementType:"
                                       + elementType + ",fieldName:" + fieldName +
                                       ",monoName:" + targetName + ",typeName:" + typeName);
                    }
                }

                field.SetValue(targetMb, array);
            }
        }
    }

    [Serializable]
    public class MonoReferenceCache
    {
#if UNITY_EDITOR
        [ShowInInspector] private string lastUpdateTimeStr => lastUpdateTime.ToString("yyyy-MM-dd HH:mm:ss");
        public DateTime lastUpdateTime;
#endif
        [HideInInspector] public List<MonoValueCache> monoValueCaches = new();
        public GameObject RootObj;

        public void ClearRefs()
        {
            monoValueCaches.Clear();
            CachedMonoBehaviours = null;
        }

        [HideInInspector] public MonoBehaviour[] CachedMonoBehaviours;

        [ShowInInspector] public int CachedMonoBehavioursCount => CachedMonoBehaviours?.Length ?? -1;

        [PropertyOrder(-1)]
        [Button]
        public void StoreReferenceCache(GameObject rootObj = null) //Editor time
        {
            RootObj = rootObj;
            monoValueCaches.Clear();
            if (RootObj != null)
            {
                CachedMonoBehaviours = RootObj.GetComponentsInChildren<MonoBehaviour>(true);
                AutoAttributeManager.AutoReferenceAll(CachedMonoBehaviours);
            }
            else
            {
                CachedMonoBehaviours = AutoAttributeManager.GetAllMonoBehavioursOfCurrentScene().ToArray();
                AutoAttributeManager.AutoReferenceAll(CachedMonoBehaviours);
            }

            foreach (var mono in CachedMonoBehaviours)
            {
                if (mono is IEditorOnly)
                {
                    continue;
                }
                var cache = new MonoValueCache();
                var fetchCount = cache.CopyFieldsToCache(mono);
                if (fetchCount > 0)
                    monoValueCaches.Add(cache);
            }
#if UNITY_EDITOR
            lastUpdateTime = DateTime.Now;
#endif
        }

        [PropertyOrder(-1)]
        [Button]
        public void RestoreReferenceCacheToMonos() //Runtime
        {
            // Debug.Log("GetAllMonoBehavioursWithAuto start:" + FieldCache.fieldDictByName.Count);
            Profiler.BeginSample("Build Field Cache");
            AutoAttributeManager.BuildFieldCache(CachedMonoBehaviours); //建立field cache, 可以copy時再做？
            Profiler.EndSample();
            // Debug.Log("GetAllMonoBehavioursWithAuto end:" + FieldCache.fieldDictByName.Count);
            Profiler.BeginSample("CopyCacheToFields");
            for (var i = 0; i < monoValueCaches.Count(); i++)
            {
                monoValueCaches[i].CopyCacheToFields();
            }

            Profiler.EndSample();
        }
    }
}