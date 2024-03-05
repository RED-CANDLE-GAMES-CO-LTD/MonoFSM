using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RCGMaker.Core
{
    //應該是讓Cache有生命週期，level層的cache和application層的cache, 這樣level層的cache被刪掉的時候就整個一起刪掉

    public interface ILevelProvider<K, V> where V : Component where K : Component
    {
        void RegisterCache(Cache<K, V> cache);
    }

//把資料放在物件上，不要中心化就沒有反註冊這個問題了
    public class Cache<K, V> where V : Component where K : Component
    {
        private readonly Dictionary<K, List<V>> cache = new();

        public void CacheStateSelfCheck()
        {
            try
            {
                var invalidPairs = new List<KeyValuePair<K, List<V>>>();
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


        public void Add(K key, V value, ILevelProvider<K, V> levelProvider = null)
        {
            //不要擋掉null喔
            if (!cache.ContainsKey(key)) cache.Add(key, new List<V>());
            cache[key].Add(value);
            //TODO:要找key or value的owner? 註冊到LevelProvider? 當LevelProvider被刪掉的時候要清掉dictionary
            if (levelProvider != null)
                levelProvider.RegisterCache(this);
        }


        public List<V> Get(K key)
        {
            return cache.GetValueOrDefault(key);
        }

        public bool Has(K key)
        {
            return cache.ContainsKey(key);
        }

        public void Remove(K key, V value)
        {
            if (cache.ContainsKey(key))
            {
                cache[key].Remove(value);
                if (cache[key].Count == 0) cache.Remove(key);
            }
        }

        public void RemoveAll(K key)
        {
            if (cache.ContainsKey(key)) cache.Remove(key);
        }
    }
}