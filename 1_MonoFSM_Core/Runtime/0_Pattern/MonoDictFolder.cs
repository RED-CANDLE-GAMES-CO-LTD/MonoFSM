using System;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// 非泛型介面，用於讓 MonoDictFolder 可以被統一處理
    /// </summary>
    public interface IMonoDictFolder
    {
        void AddExternalSource(object source);
        void RemoveExternalSource(object source);
        void ClearExternalSources();
    }

    public abstract class MonoDictFolder<T, Tu> : MonoDict<T, Tu>, IMonoDictFolder
        where Tu : IValueOfKey<T>
        where T : IEquatable<string>
    {
        public override void EnterSceneAwake()
        {
            base.EnterSceneAwake();
            //檢查有null?
            foreach (var value in AllValues)
            {
                if (value == null)
                {
                    Debug.LogError(
                        $"[MonoDictFolder] Found null value in {DescriptionTag} '{name}' during SceneAwake.",
                        this);
                }
            }
        }

        // [SerializeField]
        [LabelText("External Sources")] [ShowInInspector]
        protected List<MonoDict<T, Tu>> _externalDicts = new(); //FIXME 這個會dirty...

        public void AddExternalDict(MonoDict<T, Tu> dict)
        {
            if (dict != null && !_externalDicts.Contains(dict) && dict != this)
            {
                _externalDicts.Add(dict);
                dict._bindingRoot =
                    this; //讓外部dict知道它被綁定到這個folder了，這樣就可以在Get裡面往上找bindingRoot拿到folder的external dicts
            }
        }

        public void RemoveExternalDict(MonoDict<T, Tu> dict)
        {
            if (_externalDicts.Contains(dict))
                _externalDicts.Remove(dict);
        }

        public virtual void AddExternalSource(object source)
        {
            if (source is MonoDict<T, Tu> dict)
                AddExternalDict(dict);
        }

        public virtual void RemoveExternalSource(object source)
        {
            if (source is MonoDict<T, Tu> dict)
                RemoveExternalDict(dict);
        }

        public void ClearExternalSources()
        {
            _externalDicts.Clear();
        }

        [PreviewInInspector]
        public Tu[] AllValues
        {
            get
            {
                // Debug.Log(
                //     $"Collecting all values for {DescriptionTag} '{name}'. Local count: {Collections.Length}, External dicts: {_externalDicts.Count}");
                var results = new List<Tu>(Collections);
                foreach (var dict in _externalDicts)
                {
                    if (dict == null) continue;
                    results.AddRange(dict.Collections);
                    // Debug.Log($"Collected {dict.Collections.Length} items from external dict.");
                    // Debug.Break();
                }

                return results.ToArray();
            }
        }

        //_dict 只裝本地 Collections，external dict 的內容要另外問，
        //否則 ContainsKey / Count / inspector 的 GetKeys 會少掉 AllValues 裡看得到的那些
        public override bool Contains(T key)
        {
            if (base.Contains(key)) return true;

            foreach (var dict in _externalDicts)
            {
                if (dict == null) continue;
                if (dict.Contains(key)) return true;
            }

            return false;
        }

        public override bool Contains(string stringKey)
        {
            if (base.Contains(stringKey)) return true;

            foreach (var dict in _externalDicts)
            {
                if (dict == null) continue;
                if (dict.Contains(stringKey)) return true;
            }

            return false;
        }

        public override int Count
        {
            get
            {
                var count = base.Count;
                foreach (var dict in _externalDicts)
                {
                    if (dict == null) continue;
                    count += dict.Count;
                }

                return count;
            }
        }

        public override List<T> GetKeys
        {
            get
            {
                var results = base.GetKeys;
                foreach (var dict in _externalDicts)
                {
                    if (dict == null) continue;
                    results.AddRange(dict.GetKeys);
                }

                return results;
            }
        }

        public override List<Tu> GetValues
        {
            get
            {
                var results = base.GetValues;
                foreach (var dict in _externalDicts)
                {
                    if (dict == null) continue;
                    results.AddRange(dict.GetValues);
                }

                return results;
            }
        }

        public override List<string> GetStringKeys
        {
            get
            {
                var results = base.GetStringKeys;
                foreach (var dict in _externalDicts)
                {
                    if (dict == null) continue;
                    results.AddRange(dict.GetStringKeys);
                }

                return results;
            }
        }

        public override Tu Get(T key)
        {
            var local = base.Get(key);
            if (local != null) return local;

            foreach (var dict in _externalDicts)
            {
                if (dict == null) continue;
                var found = dict.Get(key);
                if (found != null) return found;
            }

            return default;
        }

        public override TT Get<TT>()
        {
            var local = base.Get<TT>();
            if (local != null) return local;

            foreach (var dict in _externalDicts)
            {
                if (dict == null) continue;
                var found = dict.Get<TT>();
                if (found != null) return found;
            }

            return default;
        }

        public override Tu Get(string key)
        {
            var local = base.Get(key);
            if (local != null) return local;

            foreach (var dict in _externalDicts)
            {
                if (dict == null) continue;
                var found = dict.Get(key);
                if (found != null) return found;
            }

            return default;
        }

        // /// <summary>
        // /// 通用的 Get 方法，先從本地查找，找不到再從外部字典查找
        // /// </summary>
        // private TResult GetWithExternal<TResult>(Func<MonoDict<T, Tu>, TResult> getter)
        // {
        //     var local = getter(this);
        //     if (!EqualityComparer<TResult>.Default.Equals(local, default)) return local;
        //
        //     foreach (var dict in _externalDicts)
        //     {
        //         if (dict == null) continue;
        //         var found = getter(dict);
        //         if (!EqualityComparer<TResult>.Default.Equals(found, default)) return found;
        //     }
        //
        //     return default;
        // }
        //
        // public override Tu Get(T key)
        // {
        //     return GetWithExternal(dict => dict.Get(key));
        // }
        //
        // public override Tt Get<Tt>()
        // {
        //     return GetWithExternal(dict => dict.Get<Tt>());
        // }
        //
        // public override Tu Get(string key)
        // {
        //     return GetWithExternal(dict => dict.Get(key));
        // }

        //FIXME: external folder不會跑這個喔
        protected override void AddImplement(Tu item)
        {
        }

        protected override void RemoveImplement(Tu item)
        {
        }

        protected override bool CanBeAdded(Tu item) => true;


    }
}
