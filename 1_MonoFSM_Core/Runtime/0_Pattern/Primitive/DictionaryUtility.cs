using System.Collections.Generic;
using UnityEngine;

namespace MonoFSM.Runtime.Primitive
{
    public static class DictionaryUtility
    {
        //只對新增和刪除做處理，原本還在的保留
        public static void UpdateDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> oldDict,
            Dictionary<TKey, TValue> newDict)
        {
            // Add new entries
            foreach (var item in newDict)
            {
                if (!oldDict.ContainsKey(item.Key))
                {
                    Debug.Log($"Add {item.Key}");
                    oldDict.Add(item.Key, item.Value);
                }
            }

            // Remove non-existent entries
            var keysToRemove = new List<TKey>();
            foreach (var item in oldDict)
            {
                if (!newDict.ContainsKey(item.Key))
                {
                    keysToRemove.Add(item.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                Debug.Log($"Remove {key}");
                oldDict.Remove(key);
            }
        }
    }
}