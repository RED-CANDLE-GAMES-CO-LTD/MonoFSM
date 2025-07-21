using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Auto.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;


namespace Auto_Attribute.Runtime
{
    public static class FieldCache
    {
        public static Dictionary<Type, IEnumerable<FieldInfo>> fieldDict = new();
        public static Dictionary<FieldInfo, object[]> attributeDict = new();
        public static Dictionary<(Type, string), FieldInfo> fieldDictByName = new();

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
        [HideInInspector]
        public List<FieldValueCache> fieldCaches = new();

        [ShowInInspector] public MonoBehaviour TargetMb;
        public string TargetMbName;

        public int SaveFieldsToCache(MonoBehaviour targetMb)
        {
            TargetMb = targetMb;
            TargetMbName = targetMb.name;
            // Debug.Log("SaveFieldsToCache:" + targetMb.name, TargetMb);
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
                if (!cache.SaveFieldToCache(targetMb, field, v)) continue;
                count++;
                fieldCaches.Add(cache);
            }

            return count;
        }

        public void RestoreCacheToFields()
        {
            foreach (var cache in fieldCaches)
            {
                cache.RestoreCacheToField(TargetMb);
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


        // [SerializeField] private MonoBehaviour targetMb;
        [SerializeField] private Component[] valueArray;
        [SerializeField] private Component value;

        public bool SaveFieldToCache(MonoBehaviour targetMb, FieldInfo field, object v)
        {
            // this.targetMb = targetMb;
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

//灌回去

        public void RestoreCacheToField(MonoBehaviour targetMb)
        {
            var targetMbType = targetMb.GetType();
            var tuple = (targetMbType, fieldName);
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
                            Debug.LogError($"ValueArray[{i}] element is null: elementType:" + elementType +
                                           ",fieldName:" +
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
                                       ",monoName:" + targetName + ",typeName:" + typeName, targetMb);
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
        [ShowInInspector] private string lastUpdateTimeStr => lastUpdateTime.DateTimeString;
        public SerializableDateTime lastUpdateTime;
#endif
        [HideInInspector] public List<MonoValueCache> monoValueCaches = new();

        [Button]
        private void CheckNullCache()
        {
            foreach (var cache in monoValueCaches)
            {
                if (cache.TargetMb == null)
                    Debug.LogError("TargetMb is null:" + cache.TargetMbName);
            }

            Debug.Log("CheckNullCache Done" + monoValueCaches.Count);
        }
        
        public GameObject RootObj;

        // public void ClearRefs()
        // {
        //     monoValueCaches.Clear();
        //     CachedMonoBehaviours = null;
        // }

        [HideInInspector] public MonoBehaviour[] CachedMonoBehaviours;

        [ShowInInspector] public int CachedMonoBehavioursCount => CachedMonoBehaviours?.Length ?? -1;
        [ShowInInspector] public int MonoValueCachesCount => monoValueCaches?.Count ?? -1;
        
        [PropertyOrder(-1)]
        [Button]
        public void SaveReferenceCache() //Editor time
        {
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

            var validMonoBehaviours = new List<MonoBehaviour>();
            
            foreach (var mono in CachedMonoBehaviours)
            {
                //不一定都沒有...只有被刪掉的要拿掉
                // var parentHasStrip = mono.GetComponentInParent<IEditorOnlyStrip>();
                // if (parentHasStrip != null)
                // {
                //     Debug.LogError("Parent has IEditorOnlyStrip, skip:" + mono.name);
                //     continue;
                // }
                    
                if (mono is IEditorOnly)
                {
                    continue;
                }
                
                var cache = new MonoValueCache();
                var fetchCount = cache.SaveFieldsToCache(mono);
                if (fetchCount > 0)
                {
                    monoValueCaches.Add(cache);
                    validMonoBehaviours.Add(mono);
                }
                    
            }

            CachedMonoBehaviours = validMonoBehaviours.ToArray();
#if UNITY_EDITOR
            lastUpdateTime = new SerializableDateTime(DateTime.Now);
#endif
        }

        [PropertyOrder(-1)]
        // [Button]
        public void RestoreReferenceCacheToMonoFields() //Runtime
        {
            // Debug.Log("GetAllMonoBehavioursWithAuto start:" + FieldCache.fieldDictByName.Count);
            Profiler.BeginSample("Build Field Cache");
            AutoAttributeManager.BuildFieldCache(CachedMonoBehaviours); //建立field cache, 可以copy時再做？
            Profiler.EndSample();
            // Debug.Log("GetAllMonoBehavioursWithAuto end:" + FieldCache.fieldDictByName.Count);
            Profiler.BeginSample("CopyCacheToFields");
            for (var i = 0; i < monoValueCaches.Count(); i++)
            {
                if (monoValueCaches[i].TargetMb == null)
                {
                    Debug.LogError("TargetMb is null:" + i + monoValueCaches[i].TargetMbName);
                    continue;
                }

                monoValueCaches[i].RestoreCacheToFields();
            }

            Profiler.EndSample();
        }
    }
}