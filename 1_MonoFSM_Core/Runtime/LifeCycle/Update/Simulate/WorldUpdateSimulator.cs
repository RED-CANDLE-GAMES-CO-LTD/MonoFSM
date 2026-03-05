using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.LifeCycle;
using MonoFSM.Runtime;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

namespace MonoFSM.Core.Simulate
{
    public interface ISimulateRunner { }

    public static class WorldUpdateSimulatorExtensions
    {
        public static MonoObj Spawn(
            this GameObject gObj,
            MonoObj obj,
            Vector3 position,
            Quaternion rotation
        )
        {
            if (gObj == null)
            {
                Debug.LogError("Cannot spawn a MonoPoolObj from a null GameObject.", obj);
                return null;
            }

            var simulator = WorldUpdateSimulator.GetWorldUpdateSimulator(gObj);
            // var worldUpdateSimulator = gObj.GetComponent<WorldUpdateSimulator>();
            if (simulator == null)
            {
                Debug.LogError("WorldUpdateSimulator not found on the GameObject.", gObj);
                return null;
            }

            return simulator.Spawn(obj, position, rotation);
        }
    }

    //要當作世界系統中心嗎？但如果是runner旁邊的話，就不在scene上喔

    //NOTE: 放在runner上!
    //場上可以有收集器？還是另外自己做掉?
    [DefaultExecutionOrder(10000)] //確保在所有Update之後執行
    public sealed class WorldUpdateSimulator : MonoBehaviour
    {
        //typeDict?
        //反綁？
        //fsm reset?, simulate runner
        [Required]
        [CompRef]
        [Auto]
        private ISimulateRunner _simulateRunner;

        //FIXME: Spawn要不要過我？
        [Required]
        [CompRef]
        [Auto]
        private ISpawnProcessor _spawnProcessor; //logic Spawner, 和visual spawner要拆開？

        //interface dict?

        // Component cache for better performance
        private readonly ComponentCache _componentCache = new();

        private void Awake()
        {
            _spawnProcessor = GetComponent<ISpawnProcessor>();
            _simulateRunner = GetComponent<ISimulateRunner>();
            // _simulators.AddRange(_localSimulators);
            // FIXME: 不需要了？
            _binder = GetComponent<MonoEntityBinder>();
            _binder.EnterSceneAwake();

            // Debug.Log("MonoEntityBinder Init");
        }

        [CompRef]
        [Auto]
        private MonoEntityBinder _binder;

        public static WorldUpdateSimulator GetWorldUpdateSimulator(MonoObj me)
        {
            return me.WorldUpdateSimulator;
        }

        public static WorldUpdateSimulator GetWorldUpdateSimulator(GameObject me)
        {
            //這個是用來獲取當前的WorldUpdateSimulator
            var monoPoolObj = me.GetComponentInParent<MonoObj>(true);
            if (monoPoolObj == null)
            {
                Debug.LogError("MonoObj not found on the GameObject.", me);
                return null;
            }

            if (monoPoolObj.WorldUpdateSimulator == null)
            {
                Debug.LogError("WorldUpdateSimulator not set on MonoPoolObj.", monoPoolObj);
                return null;
            }

            return monoPoolObj.WorldUpdateSimulator;
        }

        public static GameObject SpawnObj(GameObject gobj, MonoBehaviour fromWho)
        {
            var simulator = GetWorldUpdateSimulator(fromWho.gameObject);
            if (simulator == null)
            {
                Debug.LogError("WorldUpdateSimulator not found on the GameObject.", gobj);
                return null;
            }

            //沒有的話要...加一個？
            var obj = simulator.Spawn(
                gobj.GetComponent<MonoObj>(),
                gobj.transform.position,
                gobj.transform.rotation
            );
            return obj?.gameObject;
        }

        public MonoObj SpawnVisual(MonoObj obj, Vector3 position, Quaternion rotation)
        {
            //FIXME: 還要做updateSimulator的註冊？
            var newObj = PoolManager.Instance.BorrowOrInstantiate(obj, position, rotation);
            AfterPoolSpawn(newObj);
            return newObj;
        }

        public void DespawnVisual(MonoObj obj)
        {
            if (obj == null)
                return;
            // Return the object to the pool
            PoolManager.Instance.ReturnToPool(obj);
        }

        //全世界都該透過這個spawn? 只有世界上的東西要透過這個？local 的不用(ex: Canvas)
        //FIXME: 好像不對，photon應該用他原本的Spawn方法，這個處理要在之後觸發？
        //1. 想收斂Spawn進入點
        //2. 還是會出現Runner直接Spawn沒辦法避免？
        public MonoObj Spawn(MonoObj obj, Vector3 position, Quaternion rotation)
        {
            if (obj == null)
            {
                Debug.LogError("Cannot spawn a null MonoPoolObj.", this);
                return null;
            }

            //Spawn Strategy? 透過 Fusion的PoolObject 系統...那何不都用他的就好?
            var result = _spawnProcessor.Spawn(obj, position, rotation);
            //FIXME: 在這裡做 Spawn後的初始化?
            // AfterPoolSpawn(result);

            if (result == null)
                return null;

            //FIXME: spawner本來就該來call這個？順便call auto?
            RegisterMonoObject(result);

            return result;
        }

        //FIXME: despawn都需要過這個？
        /// <summary>
        /// Deferred despawn: 加入待處理佇列，在下一次 Simulate 開頭統一處理
        /// </summary>
        public void Despawn(MonoObj obj)
        {
            if (obj == null)
                return;
            if (_pendingDespawns.Contains(obj))
                return;
            _pendingDespawns.Add(obj);
            Debug.Log($"[Despawn] Queued deferred despawn: {obj.name}", obj);
        }

        /// <summary>
        /// 立即執行 despawn（內部使用，由 ProcessPendingDespawns 呼叫）
        /// </summary>
        private void DespawnImmediate(MonoObj obj)
        {
            if (obj == null)
                return;
            _spawnProcessor.Despawn(obj);
            UnregisterMonoObject(obj);
        }

        private void ProcessPendingDespawns()
        {
            if (_pendingDespawns.Count == 0)
                return;

            for (int i = 0; i < _pendingDespawns.Count; i++)
                DespawnImmediate(_pendingDespawns[i]);

            _pendingDespawns.Clear();
        }

        public void RegisterMonoObject(MonoObj target)
        {
            if (_monoObjectSet.Add(target))
            {
                // Debug.Log(
                //     $"Registering MonoPoolObj: {target.name} in WorldUpdateSimulator.",
                //     target
                // );
                target.SetWorldUpdateSimulator(this);
                //重置狀態
                // target.ResetStateRestore();
                // target.ResetStart();
            }
        }

        //FIXME: local的沒有接到？
        public void AfterPoolSpawn(MonoObj target)
        {
            if (target == null)
                return;
            //FIXME: 在這auto?

            //這個是用來做初始化的？
            RegisterMonoObject(target);
            target.SpawnFromPool(); //ISceneAwake叫兩次？
        }

        public void UnregisterMonoObject(MonoObj target)
        {
            if (_monoObjectSet.Remove(target))
            {
                // Debug.Log(
                //     $"Unregistering MonoPoolObj: {target.name} from WorldUpdateSimulator.",
                //     target
                // );
                target.ResetStateRestore(); //FIXME: 需要這行嗎？OnReturnToPool?
                target.SetWorldUpdateSimulator(null); //清除引用
            }
            else
            {
                //現在 Despawn 時可能會call兩次，避免？
                // Debug.LogError(
                //     $"Attempted to unregister MonoPoolObj: {target.name}, but it was not found in the WorldUpdateSimulator set.",
                //     target
                // );
            }
        }

        private void SceneAwake()
        {
            //這個是用來做初始化的？
            foreach (var monoObject in _monoObjectSet)
                monoObject.SceneAwake(this);
            Debug.Log(
                $"WorldUpdateSimulator SceneAwake called with {_monoObjectSet.Count} MonoPoolObjs.",
                this
            );
        }

        private void SceneStart()
        {
            foreach (var monoObject in _monoObjectSet)
                monoObject.HandleSceneStart();
        }

        //從player進入？
        public void ResetLevelRestore()
        {
            //FIXME: Pool回收會
            // PoolManager.Instance.ReturnAllObjects(); //會把player也回收掉？
            foreach (var mono in _monoObjectSet)
                mono.ResetStateRestore();
            Debug.Log(
                $"WorldUpdateSimulator ResetStateRestore called with {_monoObjectSet.Count} MonoPoolObjs.",
                this
            );
        }

        public void ResetLevelStart()
        {
            //FIXME: 有人在這個過程spawn?
            var list = _monoObjectSet.ToList();
            foreach (var mono in list)
                mono.ResetStart();
            // foreach (var mono in _monoObjectSet) mono.ResetStart();
        }

        //世界進入點
        public void WorldInit() //FIXME: 這個應該要什麼時間點做？
        {
            Debug.Log("WorldUpdateSimulator WorldInit called.", this);
            //SceneAwake可以自己做ㄅ？
            IsReady = true;
            SceneAwake();
            SceneStart();
        }

        public void WorldReset()
        {

            ResetLevelRestore();
            ResetLevelStart();
        }

        //FIXME: 可能會動態移除
        // [PreviewInInspector] [AutoChildren] private IUpdateSimulate[] _localSimulators;

        // private readonly HashSet<IUpdateSimulate> _simulators = new(); //HashSet?

        // [PreviewInInspector] [AutoChildren] private IMonoObject[] _localMonoObjects; //FIXME這顆要掛在？
        private readonly HashSet<MonoObj> _monoObjectSet = new(); //這個是用來做reset的？還是要有一個MonoObjectRunner?
        private readonly List<MonoObj> _pendingDespawns = new();
#if UNITY_EDITOR
        // [PreviewInInspector] private IUpdateSimulate[] PreviewSimulators => _simulators.ToArray();
        [PreviewInInspector]
        private MonoObj[] PreviewMonoObjects => _monoObjectSet.ToArray();
#endif

        [ShowInInspector]
        public bool IsReady { get; private set; } = false;

        public static float TimeScale { get; set; } = 1f;

        //FIXME: runner要是?
        // public static float deltaTime => Time.deltaTime * TimeScale; //FIXME: 這個要從runner同步？

        private void TimeScaleCheck()
        {
            if (Debug.isDebugBuild)
            {
                // Debug.Log(
                //     $"WorldUpdateSimulator Simulate called with deltaTime: {deltaTime}, TimeScale: {TimeScale}",
                //     this
                // );
                if (Keyboard.current.digit0Key.IsPressed() || Mouse.current.middleButton.isPressed)
                    TimeScale = 5f;
                else
                    TimeScale = 1f;
            }
        }

        private readonly List<MonoObj> _currentUpdatingObjs = new();
        private static float _deltaTime;
        public static float DeltaTime => _deltaTime * TimeScale;

        public static SimPhase CurrentPhase { get; private set; } = SimPhase.None;

        public void BeforeSimulate(float time, float deltaTime, int tick)
        {
            CurrentPhase = SimPhase.BeforeSimulate;
            CurrentTick = tick;
            SimulationTime = time;
            _deltaTime = deltaTime;
            foreach (var monoObject in _currentUpdatingObjs)
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsBeforeSimulatesNeeded)
                    {
                        Profiler.BeginSample("BeforeSimulate", monoObject);
                        monoObject.BeforeSimulate(deltaTime);
                        Profiler.EndSample();
                    }
                }
        }

        public static int CurrentTick { get; private set; }
        public static float SimulationTime { get; private set; }

        /// <summary>
        /// 需要依照環境決定怎麼simulate
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Simulate(float deltaTime)
        {
            if (!IsReady)
                return;

            ProcessPendingDespawns();

            TimeScaleCheck();
            _currentUpdatingObjs.Clear();

#if UNITY_EDITOR //FIXME: 亂call destroy可能導致這個
            foreach (var mono in _monoObjectSet)
            {
                if (mono == null)
                {
                    Debug.LogError(
                        "A MonoPoolObj in the WorldUpdateSimulator set is null. It might have been destroyed without unregistering. Removing it from the set.",
                        this
                    );
                    //FIXME: 不能這樣，要有個toRemove list
                    _monoObjectSet.Remove(mono);
                }
            }
#endif

            foreach (var obj in _monoObjectSet)
                _currentUpdatingObjs.Add(obj);

            CurrentPhase = SimPhase.Simulate;

            //FIXME: isProxy? 要ㄇ 跳過模擬，或是regiester要兩階段
            foreach (var monoObject in _currentUpdatingObjs)
            {
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsUpdateSimulatesNeeded)
                    {
                        Profiler.BeginSample("Simulate", monoObject);
                        monoObject.Simulate(deltaTime);
                        Profiler.EndSample();
                    }
                }
            }

            CurrentPhase = SimPhase.AfterSimulate;

            foreach (var monoObject in _currentUpdatingObjs)
            {
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsAfterSimulatesNeeded)
                    {
                        Profiler.BeginSample("AfterSimulate", monoObject);
                        monoObject.AfterSimulate(deltaTime);
                        Profiler.EndSample();
                    }
                }
            }

            // else
            //     Debug.LogWarning("A mono object is null or not active and enabled, skipping simulation.");

            // foreach (var simulator in _simulators)
            //     if (simulator is { isActiveAndEnabled: true })
            //         simulator.Simulate(deltaTime);
        }

        public void AfterUpdate()
        {
            if (!IsReady)
                return;
            CurrentPhase = SimPhase.AfterUpdate;
            foreach (var monoObject in _monoObjectSet)
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsUpdateSimulatesNeeded)
                    {
                        Profiler.BeginSample("AfterUpdate", monoObject);
                        monoObject.AfterUpdate();
                        Profiler.EndSample();
                    }
                }

            // else
            //     Debug.LogWarning("A mono object is null or not active and enabled, skipping after update.");
        }

#if UNITY_EDITOR
        [MenuItem("MonoFSM/ResetLevel %R")]
        public static void ManualResetLevelMenu()
        {
            if (!Application.isPlaying)
            {
                // CompilationPipeline.RequestScriptCompilation();
                //refresh editor
                AssetDatabase.Refresh();
                return;
            }
            // ManualResetLevel();
        }
#endif
        public static void ManualResetLevel() //Cheat Reset?
        {
            PoolManager.Instance.ReturnAllObjects();

            Debug.Log("ResetLevel CMD+Shift+R");
            var simulators = FindObjectsByType<WorldUpdateSimulator>(FindObjectsSortMode.None);
            //FIXME: 會拿到Temporary Runner Prefab所以才全拿
            if (simulators.Length == 0)
                Debug.LogError(
                    "No WorldUpdateSimulator found in the scene. Ensure it is present for proper reset."
                );
            else
            {
                foreach (var simulator in simulators)
                    //這樣就可以reset了
                    simulator.ResetLevelRestore();
                foreach (var simulator in simulators)
                    //這樣就可以reset了
                    simulator.ResetLevelStart();
            }
        }

        public void BeforeRender()
        {
            if (!IsReady)
                return;
            CurrentPhase = SimPhase.BeforeRender;
            foreach (var monoObject in _monoObjectSet)
                if (monoObject is { isActiveAndEnabled: true })
                {
                    Profiler.BeginSample("Render", monoObject);
                    monoObject.BeforeRender();
                    Profiler.EndSample();
                }
        }

        public void Render(float runnerLocalRenderTime)
        {
            if (!IsReady)
                return;
            CurrentPhase = SimPhase.Render;
            foreach (var monoObject in _monoObjectSet)
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsRenderSimulatesNeeded)
                    {
                        Profiler.BeginSample("Render", monoObject);
                        monoObject.Render(runnerLocalRenderTime);
                        Profiler.EndSample();
                    }
                }
        }

        public T GetCompCache<T>()
        {
            return _componentCache.GetComponent<T>(gameObject);
        }

        //可以把一些常用的先直接列出來？
    }
}
