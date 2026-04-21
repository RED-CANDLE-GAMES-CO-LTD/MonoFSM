using System;
using System.Collections.Generic;
using MonoFSM.Core.Simulate;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace MonoFSM.Runtime
{
    /// <summary>
    /// 場景生命週期管理器 - 負責處理場景中物件的生命週期事件
    /// 將原本在 PoolManager 中的場景管理邏輯分離出來
    /// </summary>
    public static class SceneLifecycleManager
    {
        /// <summary>
        /// 準備池物件的完整實作流程
        /// </summary>
        public static void PreparePoolObjectImplementation(PoolObject obj,
            WorldUpdateSimulator world)
        {
            if (obj.TryGetComponent<PrefabSerializeCache>(out var cache))
            {
                cache.RestoreReferenceCache();
            }
            else
            {
                AutoAttributeManager.AutoReferenceAllChildren(obj.gameObject);
            }

            //走MonoObj?
            if (obj.TryGetComponent<MonoObj>(out var monoObj))
            {
                monoObj.SceneAwake(world);
                monoObj.HandleSceneStart();
                return;
            }

            Debug.LogError(
                $"PoolObject {obj.name} is missing MonoObj component. Ensure that the prefab has a MonoObj component attached.",
                obj);
            // HandleGameLevelAwakeReverse(obj.gameObject);
            HandleGameLevelAwake(obj.gameObject);
            // HandleGameLevelStartReverse(obj.gameObject);
            HandleGameLevelStart(obj.gameObject);
            // LevelResetChildrenReload(obj.gameObject);
            obj.OnPrepare();
        }

        /// <summary>
        /// 場景重置和重新載入處理
        /// </summary>
        public static void ResetReload(GameObject root)
        {
            LevelResetChildrenReload(root);
            LevelResetStart(root);
        }

        /// <summary>
        /// 場景銷毀前的清理工作
        /// </summary>
        public static void OnBeforeDestroyScene(Scene s)
        {
            foreach (var g in s.GetRootGameObjects())
            {
                foreach (var rcgOnDestroy in g.GetComponentsInChildren<ISceneDestroy>(true))
                {
                    try
                    {
                        rcgOnDestroy.OnSceneDestroy();
                    }
                    catch (Exception e)
                    {
                        PoolLogger.LogComponentError(rcgOnDestroy, "OnSceneDestroy", e);
                    }
                }
            }
        }

        /// <summary>
        /// 處理子物件的狀態重置和恢復
        /// </summary>
        public static void LevelResetChildrenReload(GameObject gObj)
        {

            var levelResets = new List<IResetStateRestore>();
            //FIXME: cache?
            gObj.GetComponentsInChildren(true, levelResets);
            levelResets.Reverse();
            Debug.Log(
                $"LevelResetChildrenReload found {levelResets.Count} IResetStateRestore in Root: {gObj.name}",
                gObj);
            foreach (var item in levelResets)
            {
                if (item == null) continue;

                try
                {
                    item.ResetStateRestore(false);
                }
                catch (Exception e)
                {
                    PoolLogger.LogComponentError(item, "ResetStateRestore", e);
                }
            }
        }

        /// <summary>
        /// 處理重置開始事件
        /// </summary>
        public static void LevelResetStart(GameObject gObj)
        {
            var levelResets = new List<IResetStart>();
            gObj.GetComponentsInChildren(true, levelResets);
            levelResets.Reverse();

            foreach (var item in levelResets)
            {
                if (item == null) continue;

                try
                {
                    item.ResetStart();
                }
                catch (Exception e)
                {
                    PoolLogger.LogComponentError(item, "ResetStart", e);
                }
            }
        }

        /// <summary>
        /// 處理場景喚醒事件
        /// </summary>
        public static void HandleGameLevelAwake(GameObject level)
        {
            var levelAwakes = new List<ISceneAwake>(level.GetComponentsInChildren<ISceneAwake>(true));

            foreach (var item in levelAwakes)
            {
                if (item == null) continue;

                Profiler.BeginSample($"SceneAwake_{item}");
                try
                {
                    item.EnterSceneAwake();
                }
                catch (Exception e)
                {
                    PoolLogger.LogComponentError(item, "EnterSceneAwake", e);
                }
                Profiler.EndSample();
            }
        }

        /// <summary>
        /// 處理場景反向喚醒事件
        /// </summary>
        public static void HandleGameLevelAwakeReverse(GameObject level)
        {
            var levelAwakes = new List<ISceneAwakeReverse>(level.GetComponentsInChildren<ISceneAwakeReverse>(true));
            levelAwakes.Reverse();

            foreach (var item in levelAwakes)
            {
                if (item == null) continue;

                Profiler.BeginSample($"SceneAwakeReverse_{item}");
                try
                {
                    item.EnterSceneAwakeReverse();
                }
                catch (Exception e)
                {
                    PoolLogger.LogComponentError(item, "EnterSceneAwakeReverse", e);
                }
                Profiler.EndSample();
            }
        }

        /// <summary>
        /// 處理場景開始事件
        /// </summary>
        public static void HandleGameLevelStart(GameObject level)
        {
            var levelStarts = new List<ISceneStart>(level.GetComponentsInChildren<ISceneStart>(true));

            foreach (var item in levelStarts)
            {
                if (item == null) continue;

                try
                {
                    item.EnterSceneStart();
                }
                catch (Exception e)
                {
                    PoolLogger.LogComponentError(item, "EnterSceneStart", e);
                }
            }
        }

        /// <summary>
        /// 處理場景反向開始事件
        /// </summary>
        public static void HandleGameLevelStartReverse(GameObject level)
        {
            var levelStarts = new List<ISceneStartReverse>(level.GetComponentsInChildren<ISceneStartReverse>(true));
            levelStarts.Reverse();

            foreach (var item in levelStarts)
            {
                if (item == null) continue;

                try
                {
                    item.EnterSceneStartReverse();
                }
                catch (Exception e)
                {
                    PoolLogger.LogComponentError(item, "EnterSceneStartReverse", e);
                }
            }
        }
    }
}
