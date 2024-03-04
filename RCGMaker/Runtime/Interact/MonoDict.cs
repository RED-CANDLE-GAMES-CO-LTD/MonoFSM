using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace RCGMaker.Core
{
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