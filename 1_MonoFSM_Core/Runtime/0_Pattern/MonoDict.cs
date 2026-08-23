using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MonoFSM.Core
{
    //AutoDict?
    public abstract class
        MonoDict<T, Tu> : AbstractDescriptionBehaviour,
        ISceneAwake //FIXME: 原本T : IStringKey的，但Type就不行了
        where Tu : IValueOfKey<T>
        where T : IEquatable<string>
    {
        [ShowInInspector] public MonoDict<T, Tu> _bindingRoot;
        protected virtual bool isLog => false;

        //如果在autoReference 之前就不會進來...hmmm!?
        //有點討厭：spawned, player spawned (自己做reference & sceneAwake?), SceneAwake, SceneStart (並沒有拿到player)
        [CompRef] [AutoChildren]
        // [AutoChildren(DepthOneOnly = true)] //fixme: 什麼情況要depth only 1?
        // [SerializeField]
        protected Tu[] _collections; //disable也會被加進來

        public Tu[] Collections //這個太晚了？應該要serialize?
        {
            get
            {
                EditorPrepareCheck();
                //_collections 沒有 SerializeField，runtime 全靠 AutoChildren/MonoReferenceCache 綁；
                //有人比 lifecycle 更早問（例如 Fusion 查 DynamicWordCount、或 MonoStateMachineController.Start）
                //或 reference cache 沒存到這個物件，就會是 null
                if (_collections == null)
                {
                    Debug.LogError(
                        $"[MonoDict] {DescriptionTag} '{name}' 的 _collections 還沒綁定就被讀取，fallback 做一次 AutoReference",
                        this);
                    AutoAttributeManager.AutoReference(this);
                    _collections ??= Array.Empty<Tu>();
                }

                return _collections;
            }
        }

        protected virtual bool IsStringDictEnable => false;

        //現在是一個runtime dict...有點爛
        public Tu this[T key]
        {
            get => Get(key); //走 virtual，MonoDictFolder 才問得到 external dict
            set => _dict[key] = value;
        }

        public bool ContainsKey(T key)
        {
            return Contains(key); //走 virtual Contains，才會含 external dict
        }

        [ShowInDebugMode] public virtual int Count => _dict.Count;

        protected readonly Dictionary<T, Tu> _dict = new();

        protected readonly Dictionary<string, Tu>
            _stringDict = new(); //FIXME: 可能會過期喔？要檢查看看null了要清掉？

        //FIXME: 如果一個type有多個實例，要用List<TU>? firstOrDefault? 好像是耶
        //GetAll, 和GetComponentsInChildren<TU> 有點像 GetComponentsInChildren<TU>就回傳第一個
        //用List還是HashSet好？

        protected readonly Dictionary<Type, HashSet<Tu>> _typeDict = new(); //這個抽出去另外做？

        protected readonly List<T> _tempRemoveList = new();

        public virtual bool Contains(T key)
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

        public virtual bool Contains(string stringKey)
        {
            if (stringKey == null)
                return false;
            EditorPrepareCheck();
            return _stringDict.ContainsKey(stringKey);
        }

        //add不行用string?
        protected virtual bool IsAddValid(Tu value)
        {
            return true;
        }

        public void Add(T key, Tu value)
        {
            if (key == null)
            {
                // Debug.LogError($"Key is null, can't add {value} in {this}", value as Object);
                return;
            }

            if (Application.isPlaying && IsAddValid(value) == false)
            {
                if (isLog)
                    Debug.LogWarning($"Key:{key} can't be added in {this}", this);
                return;
            }

            if (Contains(key))
            {
                //FIXME: 不確定要怎麼處理, mono tag一定會撞ㄅ
                // Debug.LogWarning($"Key:{key} already exists in {this}", this);
                return;
            }

            // if (value is IGlobalInstance)
            // {
            var type = value.GetType();
            if (!_typeDict.TryGetValue(type, out var set))
            {
                set = new HashSet<Tu>();
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
                _stringDict.TryAdd(value.Key.ToString(), value);
            AddImplement(value);
            // enabled = true;
        }

        public HashSet<Tu> GetAll(Type type)
        {
            EditorPrepareCheck();
            return _typeDict.GetValueOrDefault(type);
        }

        //蛤？啥意思？
        public Tu Get(Type type)
        {
            EditorPrepareCheck();
            if (_isPrepared == false && Application.isPlaying)
            {
                LogNotPreparedOnce(type);
                return default;
            }

            //FIXME: 做得有點粗，要細再想一下
            var set = _typeDict.GetValueOrDefault(type);
            if (set != null && set.Count > 0)
            {
                using var enumerator = set.GetEnumerator();
                if (enumerator.MoveNext())
                    return enumerator.Current;
            }

            return default;
        }

        public virtual Tt Get<Tt>() //用Generic來拿
            where Tt : class, Tu
        {
            return Get(typeof(Tt)) as Tt;
        }

        public virtual Tu Get(string key)
        {
            EditorPrepareCheck();
            if (Contains(key))
                return _stringDict[key];
            return default;
        }

        public virtual Tu Get(T key)
        {
            EditorPrepareCheck();
            if (key == null)
                return default;

            if (_isPrepared == false && Application.isPlaying)
            {
                LogNotPreparedOnce(key);
                return default;
            }

            //FIXME:
            return _dict.GetValueOrDefault(key);
            // Debug.LogError($"Key:{key} not found in {this}",this);
        }

        [NonSerialized] private bool _hasLoggedNotPrepared;

        //每幀都可能被 Inspector/Hierarchy 的 ValueInfo 問到，洪水式 LogError 會蓋掉真正的訊息。
        //只印第一次（Refresh 後會重置），字串也只在真的要印時才組（避免每幀 GC）。
        private void LogNotPreparedOnce(object keyOrType)
        {
            if (_hasLoggedNotPrepared)
                return;
            _hasLoggedNotPrepared = true;
            Debug.LogError(
                $"[MonoDict] GetFrom {keyOrType} Dict, Not prepared. path:{transform.GetPath()}（此訊息只印一次）",
                this);
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

            _tempRemoveList.Clear(); //不清會一路累積，下次 Clear 重複 Remove 舊 key
            _dict.Clear();
        }

        protected virtual void AddFailImplement(Tu item)
        {
        }

        protected abstract void AddImplement(Tu item);
        protected abstract void RemoveImplement(Tu item); //FIXME:為什麼需要這個？

        [InfoBox("Variable 要有 varTag才會被加入到Dict中")]
        [ShowInInspector]
        public virtual List<string> GetStringKeys => new(_stringDict.Keys);

        [ShowInInspector]
        public virtual List<T> GetKeys => new(_dict.Keys);

        [ShowInInspector]
        public virtual List<Tu> GetValues //FIXME: 效能不好
        {
            get
            {
                EditorPrepareCheck();
                return new List<Tu>(_dict.Values);
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
        [NonSerialized]
        [PreviewInInspector]
        bool _isPrepared = false; //這個值 reload domain後，為什麼沒有清掉？

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
                _collections = GetComponentsInChildren<Tu>(true);
            }
#endif
            // Debug.Log("PrepareDictCheck" + name + collections.Length, this);
            //Awake 這麼早（WorldUpdateSimulator.Awake 直接呼 binder.EnterSceneAwake）AutoChildren 可能還沒綁。
            //這裡不能因此 return，否則 _isPrepared 永遠是 false，之後 Get 全部回 default —— 像 MonoEntityBinder
            //這種內容靠 runtime Add（MonoEntity.OnInstantiated）的 dict 根本不依賴 _collections。
            if (_collections == null)
            {
                AutoAttributeManager.AutoReference(this);
                if (_collections == null)
                {
                    Debug.LogWarning(
                        $"[MonoDict] {DescriptionTag} '{name}' PrepareDictCheck 時 _collections 綁不到，以空集合 prepared",
                        this);
                    _collections = Array.Empty<Tu>();
                }
            }

            _isPrepared = true;
            _hasLoggedNotPrepared = false;
            foreach (var item in _collections)
            {
                if (CanBeAdded(item) == false)
                {
                    if (isLog)
                        Debug.Log($"Can't add {item}", item as Object);
                    AddFailImplement(item);
                    continue;

                }

                Add(item.Key, item);
            }
        }

        protected abstract bool CanBeAdded(Tu item);

        protected override void Awake()
        {
            base.Awake();
            PrepareDictCheck(); //FIXME:有危險！auto可能還沒做耶！
        }

        public virtual void EnterSceneAwake()
        {
            //Edit Mode 的 EditorPrepareCheck 建好的 dict 會連著 _isPrepared=true 一起帶進 Play Mode，
            //之後在 Inspector 改 Key 欄位（例如 receiver 的 _effectType）dict 不會重建，
            //runtime 就一直用舊 key 查不到值（靜默失敗）。
            //這裡是 AutoReference 已完成、遊戲還沒開始的時機點，無條件重建一次才是權威狀態。
            Refresh();
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
    { }
}
