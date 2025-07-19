using System;
using System.Collections.Generic;
using Auto.Utils;
using MonoFSM.Core;
using MonoFSM.Core.Simulate;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSMCore.Runtime.LifeCycle
{
    public interface IMonoObjectProvider : ICompProvider<MonoPoolObj> //FIXME:這個不是很好...trace不到，最好還是都過一層？
    {
        //這個是給MonoPoolObj用的
        // MonoPoolObj GetMonoObject();
    }
    //1. 先回狀態
    public interface IResetStateRestore //新規用這個，現在和上面都有call, exitLevelAndDestroy是為了換場景很煩可以拔掉
    {
        void ResetStateRestore();
    }

    //2. 在跑這個
    public interface IResetStart //摸別人,set 變數之類的，要不然會reset掉
    {
        void ResetStart();
    }

    public interface IInstantiated
    {
        void OnInstantiated(WorldUpdateSimulator world);
    }

    /// <summary>
    /// 1.LevelAwake,
    /// 2.LevelAwakeReverse
    /// 3.LevelStart,
    /// 4.LevelStartReverse
    /// </summary>
//關著也能call
    public interface ISceneAwake //摸自己, Prefab也需要(一次性
    {
        void EnterSceneAwake();
    }

    //FIXME: auto 怎麼處理？cache?
    //這個和MonoEntity結構會類似？但分別做不同的角色？
    [ScriptTiming(-20000)]
    [DisallowMultipleComponent]
    public sealed class MonoPoolObj : MonoBehaviour, IPrefabSerializeCacheOwner
    {
        // [Button]
        //等世界準備好？從 root 拿是不是比較快？
        //FIXME: 多個註冊進入點不好
        //會找不到world很蠢，functional的方式？
        [PreviewInDebugMode] public WorldUpdateSimulator WorldUpdateSimulator { get; set; }
        
        public void Despawn()
        {
            if (WorldUpdateSimulator == null)
            {
                Debug.LogError("WorldUpdateSimulator is not set. Cannot despawn MonoPoolObj.", this);
                return;
            }

            WorldUpdateSimulator.Despawn(this);
        }


        [PreviewInInspector] [AutoChildren] private ISceneAwake[] _sceneAwakes; 
        [PreviewInInspector][AutoChildren] private ISceneStart[] _sceneStarts;
        [PreviewInInspector] [AutoChildren] private ISceneDestroy[] _sceneDestroys;
        [PreviewInInspector][AutoChildren] private IResetStateRestore[] _resetStateRestores;
        [PreviewInInspector][AutoChildren] private IResetStart[] _resetStarts;
        [PreviewInInspector][AutoChildren] private IInstantiated[] _instantiateds;
        [PreviewInInspector][AutoChildren] private IUpdateSimulate[] _updateSimulates;
        //FIXME: PoolBeforeReturnToPool? OnReturnPool?

        private readonly List<MonoPoolObj> _parentObjs = new(2); //會拿到自己？
        public bool HasParent => _parentObjs.Count > 1; //有_parentObj就表示是nested的pool object，不作用，交給parent處理

        private void Awake()
        {
            GetComponentsInParent(true, _parentObjs);
            if (HasParent)
                return;
#if UNITY_EDITOR
            AutoAttributeManager.AutoReferenceAllChildren(gameObject);
#endif
            //FIXME: prefab cache restore?
            //把 PrefabSerializeCache 的實作拿過來？
        }

        public void SpawnFromPool() //必定是root吧
        {
            //這兩行 Pool 準備的時候就要先call了吧？
            HandleIAwake();
            HandleSceneStart();

            ResetStateRestore();
            ResetStart();
        }

        //FIXME: local還沒做這個喔
        public void SceneAwake(WorldUpdateSimulator world) //可以自己sceneＡwake吧？
        {
            WorldUpdateSimulator = world;
            if (HasParent)
                return;
            HandleIAwake();
            //這可以嗎？
            HandleIInstantiated(world);
            
        }

        //FIXME: 想把這個拿掉
        private void HandleIInstantiated(WorldUpdateSimulator world)
        {
            if (HasParent)
                return;
            // Debug.Log("HandleIInstantiated",this);
            foreach (var item in _instantiateds)
            {
                if (item == null)
                    continue;
                try
                {
                    item.OnInstantiated(world);
                }
                catch (Exception e)
                {
                    if (item is MonoBehaviour)
                        Debug.LogError(e.Message + "\n" + e.StackTrace, item as MonoBehaviour);
                    else
                        Debug.LogError(e.Message + "\n" + e.StackTrace);
                }
            }
        }

        public void ResetStateRestore() //還是要分兩階，先還原，再開始？ 還是說有這種dependency本身就不好...? life cycle集中化
        {
            if (HasParent)
                return;
            HandleIResetStateRestore();
        }

        public void ResetStart()
        {
            if (HasParent)
                return;
            HandleIResetStart();
        }


        private void HandleIResetStateRestore()
        {
            if (HasParent)
                return;
            foreach (var item in _resetStateRestores)
            {
                if (item == null)
                    continue;
                try
                {
                    item.ResetStateRestore();
                }
                catch (Exception e)
                {
                    if (item is MonoBehaviour)
                        Debug.LogError(e.StackTrace, item as MonoBehaviour);
                    else
                        Debug.LogError(e.StackTrace);
                }
            }
        }

        private void HandleIResetStart()
        {
            if (HasParent)
                return;
            foreach (var item in _resetStarts)
            {
                if (item == null)
                    continue;
                try
                {
                    item.ResetStart();
                }
                catch (Exception e)
                {
                    if (item is MonoBehaviour)
                        Debug.LogError(e.Message + "\n" + e.StackTrace, item as MonoBehaviour);
                    else
                        Debug.LogError(e.Message + "\n" + e.StackTrace);
                }
            }
        }

        public bool IsProxy { get; set; }

        //理論上沒有註冊就不會call到這個
        public void Simulate(float deltaTime)
        {
            if (HasParent)
                return;
            //如果proxy就跳過？
            if (IsProxy)
                return;
            foreach (var item in _updateSimulates)
            {
                if (item == null || item.isActiveAndEnabled == false)
                    continue;
                try
                {
                    item.Simulate(deltaTime);
                }
                catch (Exception e)
                {
                    if (item is MonoBehaviour)
                        Debug.LogError(e.Message + "\n" + e.StackTrace, item as MonoBehaviour);
                    else
                        Debug.LogError(e.Message + "\n" + e.StackTrace);
                }
            }
        }

        public void AfterUpdate()
        {
            if (HasParent)
                return;
            if (IsProxy)
                return;
            foreach (var item in _updateSimulates)
            {
                if (item == null || item.isActiveAndEnabled == false)
                    continue;
                //FIXME: 這個很難偵錯耶？集中update的壞處，要怎麼樣
                // try
                // {
                item.AfterUpdate();
                // }
                // catch (Exception e)
                // {
                //     if (item is MonoBehaviour)
                //         Debug.LogError(e.Message + "\n" + e.StackTrace, item as MonoBehaviour);
                //     else
                //         Debug.LogError(e.Message + "\n" + e.StackTrace);
                // }
            }
        }


        private void HandleIAwake()
        {
            var ILevelAwakes = new List<ISceneAwake>(_sceneAwakes);
            ILevelAwakes.Reverse();

            foreach (var item in ILevelAwakes)
            {
                if (item == null)
                    continue;
                try
                {
                    item.EnterSceneAwake();
                }
                catch (Exception e)
                {
                    if (item is MonoBehaviour)
                        Debug.LogError(e.StackTrace, item as MonoBehaviour);
                    else
                        Debug.LogError(e.StackTrace);
                }
            }
        }

        public void HandleSceneStart()
        {
            if (HasParent)
                return;
            if (WorldUpdateSimulator.IsReady == false)
                // Debug.LogError("WorldUpdateSimulator is not ready. Cannot proceed with SceneAwake.", this);
                return;
            var iLevelStarts = new List<ISceneStart>(_sceneStarts);
            iLevelStarts.Reverse();

            foreach (var item in iLevelStarts)
            {
                if (item == null)
                    continue;
                try
                {
                    item.EnterSceneStart();
                }
                catch (Exception e)
                {
                    if (item is MonoBehaviour)
                        Debug.LogError(e.Message + "\n" + e.StackTrace, item as MonoBehaviour);
                    else
                        Debug.LogError(e.Message + "\n" + e.StackTrace);
                }
            }
        }

        public void OnReturnPool() //會被despawn才需要？ 反註冊用？
        {
            if (HasParent)
                return;
        }
    }
}