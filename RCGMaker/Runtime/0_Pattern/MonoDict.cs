using System;
using System.Collections.Generic;
using System.Diagnostics;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace RCGMaker.Core
{
    public abstract class MonoDict<T, TU> : MonoBehaviour, ILevelResetPrepare
        where TU : IValueOfKey<T> where T : IStringKey
    {
        [PreviewInInspector] [AutoChildren] TU[] collections; //disable也會被加進來

        protected virtual bool IsStringDictEnable => false;

        //現在是一個runtime dict...有點爛
        public TU this[T key]
        {
            get => _dict.GetValueOrDefault(key);
            set => _dict[key] = value;
        }

        public bool ContainsKey(T key)
        {
            return _dict.ContainsKey(key);
        }

        //FIXME: 我還想要兩種key....tag.string? 一定給一個基底string?
        // [ShowInInspector] protected IEnumerable<U> values => _dict.Values;
        // [ShowInInspector] private List<U> items = new();
        //FIXME: GetComponentInChildren?
        // protected readonly Dictionary<T, TU[]> _dictAll = new();

        protected readonly Dictionary<T, TU> _dict = new();
        protected readonly Dictionary<string, TU> _stringDict = new();
        protected readonly Dictionary<Type, TU> _typeDict = new();
        protected readonly List<T> _tempRemoveList = new();

        public bool Contains(T key)
        {
            if (key == null)
                return false;

            EditorPrepareCheck();
            return _dict.ContainsKey(key);
        }

        [Conditional("UNITY_EDITOR")]
        public void EditorPrepareCheck()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                PrepareDictCheck();
            }
#endif
        }

        public bool Contains(string stringKey)
        {
            if (stringKey == null)
                return false;
            EditorPrepareCheck();
            return _stringDict.ContainsKey(stringKey);
        }

        //add不行用string?

        public virtual void Add(T key, TU value)
        {
            if (Contains(key))
            {
                // Debug.LogError($"Key:{key} already exists in {this}", this);
                return;
            }

            if (key == null)
                return;
            if (value is IGlobalInstance) //
            {
                _typeDict[value.GetType()] = value;
            }

            _dict.Add(key, value);
            if (IsStringDictEnable)
                _stringDict.Add(value.Key.GetStringKey, value);
            // enabled = true;
        }

        public TU Get(Type type)
        {
            // EditorPrepareCheck();
            return _typeDict.GetValueOrDefault(type);
        }

        public TU Get(string key)
        {
            // EditorPrepareCheck();
            if (Contains(key))
                return _stringDict[key];
            return default;
        }

        public TU Get(T key)
        {
            // EditorPrepareCheck();
            //FIXME: 

            if (Contains(key))
                return _dict[key];
            this.LogError($"Key:{key} not found in {this}");
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

        [ShowInInspector] public List<string> GetStringKeys => new(_stringDict.Keys);
        [ShowInInspector] public List<T> GetKeys => new(_dict.Keys);
        [ShowInInspector] public List<TU> GetValues => new(_dict.Values);


        public void LevelResetPrepareRuntimeData()
        {
            _isPrepared = false;
            PrepareDictCheck();
        }

        [Button]
        public void Refresh()
        {
            _isPrepared = false;
            Clear();
            PrepareDictCheck();
        }

        [NonSerialized] [PreviewInInspector] bool _isPrepared = false; //這個值 reload domain後，為什麼沒有清掉？

        private void PrepareDictCheck()
        {
            if (_isPrepared)
            {
                // Debug.Log("PrepareDictCheck Already prepared",this);
                return;
            }
            //Auto還沒作用...好討厭...
#if UNITY_EDITOR
            if (Application.isPlaying == false) //reload domain完就空掉了...
            {
                Clear();
                Debug.Log("PrepareDictCheck?", this);
                _isPrepared = true;
                collections = GetComponentsInChildren<TU>(true);
            }
#endif
            foreach (var item in collections)
            {
                if (CanBeAdded(item) == false)
                    continue;
                // item.GetKeys().ForEach(key =>
                // {
                //     if (key == null)
                //         return;
                //     Add(key, item);
                // });

                Add(item.Key, item);
                // Debug.Log($"Add key:{item.Key} item:{item}",item as Object);
            }

            // enabled = false;
        }

        protected abstract bool CanBeAdded(TU item);
    }

    public interface IValueOfKey<out T>
    {
        T Key { get; }
        // T[] GetKeys();
    }

    public interface IGlobalInstance //一個binder只能有一個instance
    {
    }
}