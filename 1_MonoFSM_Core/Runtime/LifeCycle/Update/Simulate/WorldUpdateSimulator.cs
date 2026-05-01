using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.LifeCycle;
using MonoFSM.Runtime;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
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
//每個world應該要有個PoolManager對吧？不要用singleton了
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
        // [Auto]
        private ISimulateRunner _simulateRunner;

        //FIXME: Spawn要不要過我？
        [Required]
        [CompRef]
        // [Auto]
        private ISpawnProcessor _spawnProcessor; //logic Spawner, 和visual spawner要拆開？

        //interface dict?

        // Component cache for better performance
        private readonly ComponentCache _componentCache = new();

        private void Awake()
        {
            _spawnProcessor = GetComponent<ISpawnProcessor>();
            _simulateRunner = GetComponent<ISimulateRunner>();
            _poolManager = GetComponent<PoolManager>();
            // _simulators.AddRange(_localSimulators);
            // FIXME: 不需要了？
            _binder = GetComponent<MonoEntityBinder>();
            _binder.EnterSceneAwake();

            // Debug.Log("MonoEntityBinder Init");
        }

        [CompRef]
        // [Auto]
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

        [Auto] PoolManager _poolManager;
        public PoolManager Pool => _poolManager;
        public MonoObj SpawnVisual(MonoObj obj, Vector3 position, Quaternion rotation)
        {
            //FIXME: 還要做updateSimulator的註冊？
            var newObj = _poolManager.BorrowOrInstantiate(obj, position, rotation);
            AfterPoolSpawn(newObj);
            return newObj;
        }

        public void DespawnVisual(MonoObj obj)
        {
            if (obj == null)
                return;
            // Return the object to the pool
            _poolManager.ReturnToPool(obj);
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
            //這裡可能去跑 poolObject 的初始化
            var result = _spawnProcessor.Spawn(obj, position, rotation);

            if (result == null)
                return null;

            //FIXME: spawner本來就該來call這個？順便call auto?
            //太晚？EnterSceneStart 已經做完了？
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
            Debug.Log($"[DespawnImmediate] Processing despawn for: {obj.name}", obj);
            //FIXME: 不是 pool 生出來的可以關掉就好嗎？
            _spawnProcessor.Despawn(obj);

            //FIXME: 還是不該反註冊？ 省 for loop?
            UnregisterMonoObject(obj); //這個hmm?
        }

        private void ProcessPendingDespawns()
        {
            if (_pendingDespawns.Count == 0)
                return;

            for (int i = 0; i < _pendingDespawns.Count; i++)
                DespawnImmediate(_pendingDespawns[i]);

            _pendingDespawns.Clear();
        }

        public bool IsRegistered(MonoObj target) =>
            target != null && _monoObjectSet.Contains(target);

        public int RegisteredCount => _monoObjectSet.Count;
        public void RegisterMonoObject(MonoObj target)
        {
            if (target == null) return;
            if (_monoObjectSet.Add(target))
            {
                target.SetWorldUpdateSimulator(this);
                //重置狀態
                // target.ResetStateRestore();
                // target.ResetStart();
            }

            //不論是新加入還是已存在（被 despawn 後 re-spawn），都把 update flag 開回來
            target.IsActiveInSimulator = true;
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
            if (target == null) return;
            // if (target.isSceneObj)
            // return;
            // 不再從 set 中移除，僅關閉 update flag；reset list 仍會 iterate 全 set
            if (target.IsActiveInSimulator)
            {
                target.IsActiveInSimulator = false;
                //FIXME: 需要這行嗎？OnReturnToPool?
                // target.ResetStateRestore(false);
                if (target.isPoolObj)
                {
                    _monoObjectSet.Remove(target);
                    target.SetWorldUpdateSimulator(null); //清除引用
                }
            }
        }

        private void SceneAwake()
        {
            // Pass 1: 先建立所有 parent-child 關係，避免 HashSet 遍歷順序導致 child 被當成 root
            foreach (var monoObject in _monoObjectSet)
                monoObject.InitParentLinks();

            // Pass 2: 初始化（child 已有正確的 _parentObj，會正確 early return）
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
        public void ResetLevelRestore(bool isHardReset = false)
        {
            _levelStartTime = SimulationTime;
            //FIXME: Pool回收會
            // PoolManager.Instance.ReturnAllObjects(); //會把player也回收掉？
            foreach (var mono in _monoObjectSet)
                mono.ResetStateRestore(isHardReset);
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
        public void WorldInit()
        {
            Debug.Log("WorldUpdateSimulator WorldInit called.", this);
            IsReady = true;
            SceneAwake();
            SceneStart();
            WorldReset(); //這裡就可以了嗎？
            // SceneStart 在 Unity Start() 中執行，確保所有 Awake 已完成
        }

        private void Start()
        {
            // if (IsReady)
            // {
            //
            // }
            // else
            // {
            //     Debug.LogError(
            //         "WorldUpdateSimulator Start called before WorldInit. Ensure WorldInit is called to properly initialize the world.",
            //         this
            //     );
            //     Debug.Break();
            // }

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
        [ShowInInspector] int monoObjCount => _monoObjectSet.Count;
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
        public static float LocalAlpha { get; private set; }

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

        public static float LevelSimulationTime
        {
            get
            {
                if (SimulationTime - _levelStartTime < 0)
                {
                    _levelStartTime = SimulationTime;
                }

                return SimulationTime - _levelStartTime;
            }
        }

        private static float _levelStartTime;

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

        [ShowInInspector] private readonly List<MonoObj> _previewObj = new();
        [ShowInInspector] int PreviewUpdatingCount => _previewObj.Count;

        [Button]
        void GetCurrentRunningObj()
        {
            var count = 0;
            _previewObj.Clear();
            foreach (var monoObject in _currentUpdatingObjs)
            {
                if (monoObject is not { isActiveAndEnabled: true }) continue;
                if (monoObject.IsUpdateSimulatesNeeded)
                {
                    _previewObj.Add(monoObject);
                    count++;
                }
            }

            Debug.Log($"Current updating MonoObjects count: {count}", this);
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

        public static void ManualResetLevel(bool isHardReset = false) //Cheat Reset?
        {
            Debug.Log("ResetLevel CMD+Shift+R isHardReset:" + isHardReset);
            var simulators = FindObjectsByType<WorldUpdateSimulator>(FindObjectsSortMode.None);
            //FIXME: 會拿到Temporary Runner Prefab所以才全拿
            if (simulators.Length == 0)
                Debug.LogError(
                    "No WorldUpdateSimulator found in the scene. Ensure it is present for proper reset."
                );
            else
            {
                foreach (var simulator in simulators)
                    simulator._poolManager.ReturnAllObjects();
                foreach (var simulator in simulators)
                    //這樣就可以reset了
                    simulator.ResetLevelRestore(isHardReset);
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
                    monoObject.AfterRender();
                    Profiler.EndSample();
                }
        }

        /// <summary>
        /// runnerLocalRenderTime已經乘過 local Alpha了？
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="localAlpha"></param>
        public void Render(float deltaTime, float localAlpha)
        {
            if (!IsReady)
                return;
            LocalAlpha = localAlpha;
            CurrentPhase = SimPhase.Render;
            foreach (var monoObject in _monoObjectSet)
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsRenderSimulatesNeeded)
                    {
                        Profiler.BeginSample("Render", monoObject);
                        monoObject.Render(deltaTime * localAlpha);
                        Profiler.EndSample();
                    }
                }
        }

        public T GetCompCache<T>()
        {
            return _componentCache.GetComponent<T>(gameObject);
        }

        //可以把一些常用的先直接列出來？
        public void AfterRender()
        {
            CurrentPhase = SimPhase.AfterRender;
            foreach (var monoObject in _monoObjectSet)
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsRenderSimulatesNeeded)
                    {
                        Profiler.BeginSample("Render", monoObject);
                        monoObject.AfterRender();
                        Profiler.EndSample();
                    }
                }
        }
    }
}
