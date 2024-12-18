using System;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Core
{
    
    public abstract class MonoDict<T, TU> : MonoBehaviour, ILevelResetPrepare where TU:IValueOfKey<T>
    {
        [PreviewInInspector]
        [AutoChildren] TU[] collections; //disable也會被加進來


        //現在是一個runtime dict...有點爛
        public TU this[T key]
        {
            get => _dict.GetValueOrDefault(key);
            set => _dict[key] = value;
        }
        // [ShowInInspector] protected IEnumerable<U> values => _dict.Values;
        // [ShowInInspector] private List<U> items = new();
        protected readonly Dictionary<T, TU> _dict = new();
        protected readonly List<T> _tempRemoveList = new();
        public bool Contains(T key)
        {
            if (key == null)
                return false;
            return _dict.ContainsKey(key);
        }

        public virtual void Add(T key, TU value)
        {
            if (Contains(key))
                return;
            if (key == null)
                return;
            _dict.Add(key, value);
            // enabled = true;
        }

        public TU Get(T key)
        {
            if (Contains(key))
                return _dict[key];
            return default;
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

        protected abstract void RemoveImplement(TU item); //FIXME:為什麼需要這個？

        [ShowInInspector] public List<T> GetKeys => new(_dict.Keys);
        [ShowInInspector] public List<TU> GetValues => new(_dict.Values);

        // public void EnterLevelReset()
        // {
        //    
        // }
        //
        // public void ExitLevelAndDestroy()
        // {
        // }
        private void Start()
        {
            // PrepareDict();
        }

        public void LevelResetPrepareRuntimeData()
        {
            PrepareDict();   
        }

        [Button]
        void Preview()
        {
            PrepareDict();
        }
        void PrepareDict()
        {
            Clear();
            foreach (var item in collections)
            {
                if(CanBeAdded(item) == false)
                    continue;
                Add(item.Key, item);
                Debug.Log($"Add key:{item.Key} item:{item}",item as Object);
            }
            // enabled = false;
        }
        
        protected abstract bool CanBeAdded(TU item);

    }

    public interface IValueOfKey<out T>
    {
        T Key { get; }
    }
}