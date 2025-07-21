using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonoFSM.Core
{
    //FIXME: use case?
    // 應該是讓 Cache 有生命週期，level 層的 cache 和 application 層的 cache, 這樣 level 層的 cache 被刪掉的時候就整個一起刪掉
    public interface ILevelProvider<K, V> where V : Component where K : Component
    {
        void RegisterCache(Cache<K, V> cache);
    }

    // 把資料放在物件上，不要中心化就沒有反註冊這個問題了
    public class Cache<TK, TV> where TV : Component where TK : Component
    {
        private readonly Dictionary<TK, List<TV>> cache = new();

        public void Clear()
        {
            cache.Clear();
        }

        public void CacheStateSelfCheck()
        {
            try
            {
                var invalidPairs = new List<KeyValuePair<TK, List<TV>>>();
                foreach (var pair in cache)
                {
                    if (pair.Key == null)
                    {
                        invalidPairs.Add(pair);
                        continue;
                    }

                    if (pair.Value == null)
                    {
                        invalidPairs.Add(pair);
                        continue;
                    }

                    var listHasNull = false;
                    for (var i = 0; i < pair.Value.Count; i++)
                    {
                        if (pair.Value[i] == null)
                        {
                            listHasNull = true;
                        }
                    }

                    if (listHasNull)
                    {
                        invalidPairs.Add(pair);
                    }
                }

                foreach (var invalidPair in invalidPairs)
                {
                    cache.Remove(invalidPair.Key);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }


        public void Add(TK key, TV value, ILevelProvider<TK, TV> levelProvider = null)
        {
            //不要擋掉null喔
            if (!cache.ContainsKey(key)) cache.Add(key, new List<TV>());
            cache[key].Add(value);
            //TODO:要找key or value的owner? 註冊到LevelProvider? 當LevelProvider被刪掉的時候要清掉dictionary
            if (levelProvider != null)
                levelProvider.RegisterCache(this);
        }


        public List<TV> Get(TK key) 
            => cache.GetValueOrDefault(key);

        public bool Has(TK key) 
            => cache.ContainsKey(key);

        public void Remove(TK key, TV value)
        {
            if (cache.ContainsKey(key))
            {
                cache[key].Remove(value);
                if (cache[key].Count == 0) cache.Remove(key);
            }
        }

        public void RemoveAll(TK key) 
            => cache.Remove(key);
    }
}