using System;
using System.Collections.Generic;
using System.Diagnostics;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MonoFSM.Core
{
    //AutoDict?
    public abstract class MonoDict<T, TU> : MonoBehaviour, ISceneAwake
        where TU : IValueOfKey<T> where T : IStringKey
    {
        protected virtual bool isLog => false;


        //如果在autoReference 之前就不會進來...hmmm!?
        //有點討厭：spawned, player spawned (自己做reference & sceneAwake?), SceneAwake, SceneStart (並沒有拿到player)
        [CompRef]
        [PreviewInInspector] [AutoChildren] protected TU[] _collections; //disable也會被加進來

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
        
        protected readonly Dictionary<T, TU> _dict = new();
        protected readonly Dictionary<string, TU> _stringDict = new();

        //FIXME: 如果一個type有多個實例，要用List<TU>? firstOrDefault? 好像是耶
        //GetAll, 和GetComponentsInChildren<TU> 有點像 GetComponentsInChildren<TU>就回傳第一個
        //用List還是HashSet好？
        protected readonly Dictionary<Type, HashSet<TU>> _typeDict = new(); //這個抽出去另外做？
        
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
        protected virtual bool IsAddValid(TU value)
        {
            return true;
        }

        public void Add(T key, TU value)
        {
            if (key == null)
                return;
            if (Application.isPlaying && IsAddValid(value) == false)
            {
                Debug.LogWarning($"Key:{key} can't be added in {this}", this);
                return;
            }
            if (Contains(key))
            {
                //FIXME: 不確定要怎麼處理, mono tag一定會撞ㄅ
                Debug.LogWarning($"Key:{key} already exists in {this}", this);
                return;
            }

            // if (value is IGlobalInstance)
            // {
            var type = value.GetType();
            if (!_typeDict.TryGetValue(type, out var set))
            {
                set = new HashSet<TU>();
                _typeDict[type] = set;
            }

            set.Add(value);
            // }

            if (isLog)
            {
                Debug.Log($"Add key:{key} value:{value}", value as Object);
            }

            _dict.Add(key, value);
            if (IsStringDictEnable)
                _stringDict.TryAdd(value.Key.GetStringKey, value);
            AddImplement(value);
            // enabled = true;
        }

        public HashSet<TU> GetAll(Type type)
        {
            EditorPrepareCheck();
            return _typeDict.GetValueOrDefault(type);
        }

        public TU Get(Type type)
        {
            EditorPrepareCheck();
            //FIXME: 做得有點粗，要細再想一下
            var set = _typeDict.GetValueOrDefault(type);
            return set != null ? System.Linq.Enumerable.FirstOrDefault(set) : default;
        }

        public TU Get(string key)
        {
            EditorPrepareCheck();
            if (Contains(key))
                return _stringDict[key];
            return default;
        }

        public TU Get(T key)
        {
            EditorPrepareCheck();
            if (key == null)
                return default;
            //FIXME: 
            if (_dict.TryGetValue(key, out var value))
                return value;

            if (_isPrepared == false && Application.isPlaying)
            {
                Debug.LogError($"GetFrom {key} Dict, Not prepared", this);
                return default;
            }

            // Debug.LogError($"Key:{key} not found in {this}",this);
            return default;
        }

        //remove
        public bool Remove(T key)
        {
            if (key == null)
                return false;
            if (_dict.TryGetValue(key, out var item) == false)
                return false;

            try
            {
                if (item != null)
                    RemoveImplement(item);
                // Remove from _typeDict if present
                var type = item.GetType();
                if (_typeDict.TryGetValue(type, out var set))
                {
                    set.Remove(item);
                    if (set.Count == 0)
                        _typeDict.Remove(type);
                }
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

        protected abstract void AddImplement(TU item);
        protected abstract void RemoveImplement(TU item); //FIXME:為什麼需要這個？

        [InfoBox("Variable 要有 varTag才會被加入到Dict中")]
        [ShowInInspector] public List<string> GetStringKeys => new(_stringDict.Keys);
        [ShowInInspector] public List<T> GetKeys => new(_dict.Keys);

        [ShowInInspector]
        public List<TU> GetValues //FIXME: 效能不好
        {
            get
            {
                EditorPrepareCheck();
                return new List<TU>(_dict.Values);
            }
        }


        [Button]
        public void Refresh()
        {
            _isPrepared = false;
            Clear();
            PrepareDictCheck();
        }

        private bool IsNotPrepared => _isPrepared == false;

        [InfoBox("還沒準備好", nameof(IsNotPrepared), InfoMessageType = InfoMessageType.Error)]
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
                // Debug.Log("PrepareDictCheck?", this);
                _isPrepared = true;
                _collections = GetComponentsInChildren<TU>(true);
            }
#endif
            _isPrepared = true;
            // Debug.Log("PrepareDictCheck" + name + collections.Length, this);
            foreach (var item in _collections)
            {
                if (CanBeAdded(item) == false)
                {
                    if (isLog)
                        Debug.Log($"Can't add {item}", item as Object);
                    continue;
                }


                // item.GetKeys().ForEach(key =>
                // {
                //     if (key == null)
                //         return;
                //     Add(key, item);
                // });
                // Debug.Log($"PrepareDictCheck Add key:{item.Key} item:{item}", item as Object);
                Add(item.Key, item);
                // Debug.Log(_dict.Count);
                // Debug.Log($"Add key:{item.Key} item:{item}",item as Object);
            }

            // _isPrepared = true;
            // enabled = false;
        }

        protected abstract bool CanBeAdded(TU item);
        public void EnterSceneAwake()
        {
            PrepareDictCheck();
            // Debug.Log("MonoDict EnterSceneAwake Dict", this);
            // foreach (var key in _dict.Keys)
            // {
            //     Debug.Log("MonoDict Prepare" + key + " " + _dict[key], _dict[key] as Object);
            // }
        }
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