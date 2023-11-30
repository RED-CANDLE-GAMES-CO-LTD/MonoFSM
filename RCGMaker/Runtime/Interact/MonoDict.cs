using System.Collections.Generic;
using UnityEngine;

namespace RCGMaker.Core
{
    public abstract class MonoDict<T, U> : MonoBehaviour, IResetter
    {
        // [ShowInInspector] protected IEnumerable<U> values => _dict.Values;
        // [ShowInInspector] private List<U> items = new();
        protected readonly Dictionary<T, U> _dict = new();

        public bool Contains(T key)
        {
            return _dict.ContainsKey(key);
        }

        public virtual void Add(T key, U value)
        {
            _dict.Add(key, value);
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
            RemoveImplement(item);
            var result = _dict.Remove(key);
            return result;
        }

        public void Clear()
        {
            _dict.Clear();
        }

        protected abstract void RemoveImplement(U item);

        public void EnterLevelReset()
        {
            foreach (var key in _dict.Keys)
            {
                Remove(key);
            }

            _dict.Clear();

            enabled = false;
        }

        public void ExitLevelAndDestroy()
        {
        }
    }
}