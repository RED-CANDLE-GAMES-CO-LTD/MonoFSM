using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace RCGMaker.Core
{
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

        public void Add(K key, V value)
        {
            //不要擋掉null喔
            if (!cache.ContainsKey(key)) cache.Add(key, new List<V>());
            cache[key].Add(value);
        }

        public List<V> Get(K key)
        {
            return cache.TryGetValue(key, out var values) ? values : null;
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
    public abstract class MonoDict<T, U> : MonoBehaviour, IResetter
    {
        // [ShowInInspector] protected IEnumerable<U> values => _dict.Values;
        // [ShowInInspector] private List<U> items = new();
        protected readonly Dictionary<T, U> _dict = new();
        protected readonly List<T> _tempRemoveList = new();
        public bool Contains(T key)
        {
            return _dict.ContainsKey(key);
        }

        public virtual void Add(T key, U value)
        {
            _dict.Add(key, value);
            enabled = true;
        }

        public U Get(T key)
        {
            if (Contains(key))
                return _dict[key];
            return default;
            ;
        }

        //remove
        public bool Remove(T key)
        {
            if (key == null)
                return false;
            if (_dict.ContainsKey(key) == false)
                return false;
            var item = _dict[key];

            try
            {
                if (item != null)
                    RemoveImplement(item);
            }
            catch (Exception e)
            {
                //RemoveImplement implementation failed.
                Debug.LogError(e);
            }
            

            var result = _dict.Remove(key);
            return result;
        }

        public void Clear()
        {
            using var iterator = _dict.GetEnumerator();
            // var iterator = _dict.GFValueIterator();
            while (iterator.MoveNext())
            {
                var item = iterator.Current.Key;
                _tempRemoveList.Add(item);
            }

            foreach (var key in _tempRemoveList)
            {
                Remove(key);
            }
            
            _dict.Clear();
        }

        protected abstract void RemoveImplement(U item);

        [ShowInInspector] public List<T> GetKeys => new(_dict.Keys);

        public void EnterLevelReset()
        {
            Clear();
            enabled = false;
        }

        public void ExitLevelAndDestroy()
        {
        }
    }
}