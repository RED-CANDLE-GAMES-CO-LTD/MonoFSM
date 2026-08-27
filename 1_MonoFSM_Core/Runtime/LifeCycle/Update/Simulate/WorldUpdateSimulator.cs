using System;
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

    /// <summary>
    ///     Level reset 的網路廣播入口（由 Fusion 層的 NetworkedLevelResetSync 實作並註冊）。
    ///     有連線時 ManualResetLevel 會路由到這裡：SA 端 bump 版本號後各 peer 各自本地 reset，
    ///     確保 client 端沒被 NetworkedVarSync 覆蓋的本地狀態（FSM state、非同步 Var、pool 物件）也被還原。
    /// </summary>
    public interface ILevelResetBroadcaster
    {
        /// <summary>是否為網路權威端（multi-peer 下用來優先挑 SA 那顆直接 bump，省一趟 RPC）。</summary>
        bool IsResetAuthority { get; }

        /// <summary>回傳 true 表示請求已交給網路層（本地 reset 由它回呼），false 表示目前無法處理。</summary>
        bool TryBroadcastReset(bool isHardReset);
    }

    public static class WorldUpdateSimulatorExtensions
    {
        public static MonoObj Spawn(
            this GameObject gObj,
            MonoObj obj,
            Vector3 position,
            Quaternion rotation,
            IPoolObjectPlayer player = null
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

            return simulator.Spawn(obj, position, rotation, player);
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
                gobj.transform.rotation,
                fromWho as IPoolObjectPlayer
            );
            return obj?.gameObject;
        }

        [Auto] PoolManager _poolManager;
        public PoolManager Pool => _poolManager;
        public MonoObj SpawnVisual(MonoObj obj, Vector3 position, Quaternion rotation,
            IPoolObjectPlayer player = null)
        {
            //FIXME: 還要做updateSimulator的註冊？
            var newObj = _poolManager.BorrowOrInstantiate(obj, position, rotation);
            //純 local visual 一律有 authority（pool 重用可能殘留舊值），要在 SpawnFromPool 之前設好
            //寫啥？都不可以有吧？
            if (newObj != null)
                newObj.AssignShouldSimulateForAllChildrenObj(false);
            AfterPoolSpawn(newObj);
            BindLastPlayer(newObj, player);
            return newObj;
        }

        /// <summary>
        ///     把「誰噴的」記在 PoolObject 上，Inspector 的「誰噴的」欄位才點得回來源。
        ///     統一在這裡綁，避免每個呼叫點自己 GetComponent 又漏掉（漏掉的症狀是場上出現孤兒物件卻查不到來源）。
        /// </summary>
        private static void BindLastPlayer(MonoObj newObj, IPoolObjectPlayer player)
        {
            if (newObj == null || player == null)
                return;

            if (!newObj.TryGetComponent<PoolObject>(out var poolObj))
            {
                //spawn 出來的東西照慣例都該有 PoolObject（不然回收路徑也是壞的），沒有就是 prefab 少掛
                Debug.LogWarning(
                    $"[WorldUpdateSimulator] {newObj.name} 沒有 PoolObject，無法記錄 lastPlayer（來源：{(player as Component)?.name}）",
                    newObj);
                return;
            }

            poolObj.lastPlayer = player;
#if UNITY_EDITOR
            poolObj._lastPlayerName = (player as Component) != null
                ? (player as Component).name + " (" + player.GetType().Name + ")"
                : player.GetType().Name;
#endif
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
        public MonoObj Spawn(MonoObj obj, Vector3 position, Quaternion rotation,
            IPoolObjectPlayer player = null)
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
            BindLastPlayer(result, player);

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
            // Debug.Log($"[DespawnImmediate] Processing despawn for: {obj.name}", obj);
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
                _monoObjectSetDirty = true;
#if UNITY_EDITOR
                //趁物件還活著記下身份，等它被 Destroy 成 fake-null 時才有東西可以印
                var id = target.GetInstanceID();
                if (!_registeredNames.ContainsKey(id))
                {
                    var parent = target.transform.parent;
                    _registeredNames[id] =
                        $"{target.name} (parent:{(parent != null ? parent.name : "none")}, poolObj:{target.isPoolObj})";
                }
#endif
                target.SetWorldUpdateSimulator(this);
                //所有 children都要？
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
            //來之前就auto過了

            //把下面的children也都註冊進去
            foreach (var obj in target.ChildrenObjs)
            {
                RegisterMonoObject(obj);
            }


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
                    if (_monoObjectSet.Remove(target))
                        _monoObjectSetDirty = true;
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
            var objs = BuildResetIterationBuffer(nameof(ResetLevelRestore));
            for (var i = 0; i < objs.Count; i++)
            {
                var mono = objs[i];
                if (mono == null) //buffer 建好之後才被 destroy 的
                    continue;
                try
                {
                    mono.ResetStateRestore(isHardReset);
                }
                catch (System.Exception e)
                {
                    //單顆壞掉不能拖垮整批（對齊 MonoObj.HandleIResetStateRestore 內層的做法）
                    Debug.LogException(e, this);
                }
            }

            Debug.Log(
                $"WorldUpdateSimulator ResetStateRestore called with {objs.Count} MonoPoolObjs.",
                this
            );
        }

        public void ResetLevelStart()
        {
            //FIXME: 有人在這個過程spawn?
            var objs = BuildResetIterationBuffer(nameof(ResetLevelStart));
            for (var i = 0; i < objs.Count; i++)
            {
                var mono = objs[i];
                if (mono == null)
                    continue;
                try
                {
                    mono.ResetStart();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        /// <summary>
        ///     清掉「已被 destroy 但沒 UnregisterMonoObject」的殘留註冊，並盡量指名是誰。
        ///     已 destroy 的 UnityEngine.Object 是 fake-null：name/transform 都不能再存取，
        ///     但 GetInstanceID() 還是原本的 id，所以拿 id 去查 register 時記下的名字表反推兇手。
        /// </summary>
        private int PurgeDestroyedRegistrations(string phase)
        {
            var removed = _monoObjectSet.RemoveWhere(mono =>
            {
                if (mono != null)
                    return false;
#if UNITY_EDITOR
                var id = mono.GetInstanceID();
                _registeredNames.TryGetValue(id, out var who);
                Debug.LogError(
                    $"[WorldUpdateSimulator] {phase}: 註冊表裡的 MonoObj 已被 Destroy 但沒 UnregisterMonoObject，"
                    + $"移除：{who ?? "(註冊時未記錄名字)"} id:{id}",
                    this
                );
                _registeredNames.Remove(id);
#endif
                return true;
            });

            if (removed > 0)
            {
                _monoObjectSetDirty = true;
#if !UNITY_EDITOR
                Debug.LogError(
                    $"[WorldUpdateSimulator] {phase}: 清掉 {removed} 個已被 destroy 但沒 unregister 的 MonoObj 註冊。",
                    this
                );
#endif
            }

            return removed;
        }

        //reset 專用的迭代 buffer：reset 過程中會大量 spawn/despawn 改動 _monoObjectSet，
        //不能共用 _iterationSnapshot（Simulate/Render 階段也會就地 Clear 重建同一個 List）
        private readonly List<MonoObj> _resetIterationBuffer = new();

        /// <summary>
        ///     迭代前先把「已被 destroy 但沒 unregister」的殘留註冊清掉，再複製成 buffer。
        ///     不清的話 foreach 撞到 destroyed MonoObj 會拋 MissingReferenceException，
        ///     整批 reset 從那顆中斷，排在它後面的物件完全不會被 reset（症狀是按幾次 reset 才成功一次，
        ///     因為 HashSet 的迭代順序會隨 ReturnAllObjects 改動而變）。
        ///     複製成 buffer 是為了讓 reset 過程中的 spawn/despawn 不會造成 Collection was modified。
        /// </summary>
        private List<MonoObj> BuildResetIterationBuffer(string phase)
        {
            PurgeDestroyedRegistrations(phase);

            _resetIterationBuffer.Clear();
            foreach (var mono in _monoObjectSet)
                _resetIterationBuffer.Add(mono);
            return _resetIterationBuffer;
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

        /// <summary>
        /// 切場景前把整個 world 拆乾淨，讓下一次 <see cref="WorldInit"/> 從空集合重建。
        ///
        /// 為什麼需要：WorldUpdateSimulator 掛在 NetworkRunner prefab 上，從大廳流程進遊戲場景時
        /// Runner 是 DontDestroyOnLoad 一路存活的，這顆 simulator 會被「重用」而不是重建。
        /// 沒有這個 teardown 的話：
        ///   1. 舊場景的 MonoObj 被 Destroy 但還留在 _monoObjectSet（非 poolObj 不會走 UnregisterMonoObject）
        ///      → Simulate 每幀噴 "A MonoPoolObj in the WorldUpdateSimulator set is null"
        ///   2. IsReady 一直是 true → 新場景載入的空窗期就開始 Simulate，物件還沒 SceneAwake
        ///   3. WorldInit 被跑第二次，SceneAwake/SceneStart 對殘留物件重複呼叫
        ///
        /// 由 driver 在場景載入開始時呼叫（ex: FusionSimulatorRunner.OnSceneLoadStart）。
        /// </summary>
        public void TeardownForSceneSwitch()
        {
            Debug.Log(
                $"[WorldUpdateSimulator] TeardownForSceneSwitch: 清除 {_monoObjectSet.Count} 個 MonoObj 註冊。",
                this
            );

            //擋住 Simulate/Render/AfterUpdate，直到下一次 WorldInit
            IsReady = false;

            //pool 物件是 DontDestroyOnLoad，切場景不會被銷毀，而 RegisterAllMonoPoolObjs 只掃新場景的 root，
            //所以借出中的 pool 物件如果不先收回，就會永遠脫離註冊表（active 但不再被 simulate）。
            if (_poolManager != null)
                _poolManager.ReturnAllObjects();

            //ReturnAllObjects 回收的物件不能被其他 handler 誤判為「被消耗」，比照 ManualResetLevel 通知一輪
            foreach (var resetHandler in GetComponents<ILevelResetSpawnHandler>())
                resetHandler.OnBeforeLevelReset();

            _pendingDespawns.Clear();
            _monoObjectSet.Clear();
#if UNITY_EDITOR
            _registeredNames.Clear();
#endif
            _currentUpdatingObjs.Clear();
            _iterationSnapshot.Clear();
            _previewObj.Clear();
            _monoObjectSetDirty = true;
            CurrentPhase = SimPhase.None;
        }

        //FIXME: 可能會動態移除
        // [PreviewInInspector] [AutoChildren] private IUpdateSimulate[] _localSimulators;

        // private readonly HashSet<IUpdateSimulate> _simulators = new(); //HashSet?

        // [PreviewInInspector] [AutoChildren] private IMonoObject[] _localMonoObjects; //FIXME這顆要掛在？
        private readonly HashSet<MonoObj> _monoObjectSet = new(); //這個是用來做reset的？還是要有一個MonoObjectRunner?
        private readonly List<MonoObj> _pendingDespawns = new();

#if UNITY_EDITOR
        //instanceID → 註冊時的物件身份描述。用來在物件被 Destroy 成 fake-null 之後還能指名是誰。
        private readonly Dictionary<int, string> _registeredNames = new();
#endif

        //迭代用 snapshot：避免 Render 中 SpawnVisual 註冊新 MonoObj 時 "Collection was modified"
        //只在 set 有增減時重建（dirty flag），不會每幀 ToList 產生 GC
        private readonly List<MonoObj> _iterationSnapshot = new();
        private bool _monoObjectSetDirty = true;

        private List<MonoObj> GetIterationSnapshot()
        {
            if (_monoObjectSetDirty)
            {
                _iterationSnapshot.Clear();
                foreach (var obj in _monoObjectSet)
                    _iterationSnapshot.Add(obj);
                _monoObjectSetDirty = false;
            }

            return _iterationSnapshot;
        }
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
            // if (Debug.isDebugBuild)
            // {
            //     // Debug.Log(
            //     //     $"WorldUpdateSimulator Simulate called with deltaTime: {deltaTime}, TimeScale: {TimeScale}",
            //     //     this
            //     // );
            //
            // }

        }

        private readonly List<MonoObj> _currentUpdatingObjs = new();
        private static float _deltaTime;

        public static float DeltaTime => _deltaTime * TimeScale;
        public static float LocalAlpha { get; private set; }

        /// <summary>
        /// 目前 Simulate phase 是否為 Fusion 的 resimulation tick。
        /// 由 driver（如 FusionSimulatorRunner）在 FixedUpdateNetwork 開頭設定。
        /// 用於讓某些不該在 resim 重複推進的邏輯（如 SplineMover 沿路徑前進）跳過。
        /// </summary>
        public static bool IsResimulation { get; set; }

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
                        monoObject.BeforeSimulate(DeltaTime);
                        Profiler.EndSample();
                    }
                }
        }

        public static int CurrentTick { get; set; }
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

            //Update 排程的本地 reset 在這裡執行（ManualResetLevelLocal 會一口氣重置所有 simulator，
            //所以由第一個跑到 Simulate 的 simulator 消耗掉即可）
            ConsumePendingLocalReset();

            ProcessPendingDespawns();

            TimeScaleCheck();
            _currentUpdatingObjs.Clear();

#if UNITY_EDITOR //FIXME: 亂call destroy可能導致這個
            PurgeDestroyedRegistrations(nameof(Simulate));
#endif

            foreach (var obj in _monoObjectSet)
                _currentUpdatingObjs.Add(obj);

            CurrentPhase = SimPhase.Simulate;

            //FIXME: isProxy? 要ㄇ 跳過模擬，或是regiester要兩階段
            foreach (var monoObject in _currentUpdatingObjs)
            {
                if (monoObject == null)
                    continue;

                //要在 IsUpdateSimulatesNeeded 之前：被 cull 時那個 property 回 false，
                //Simulate 不會被呼叫，culling 的邊緣就沒人偵測得到了
                monoObject.CullingStateCheck();

                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsUpdateSimulatesNeeded)
                    {
                        Profiler.BeginSample("Simulate", monoObject);
                        monoObject.Simulate(DeltaTime);
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
                        monoObject.AfterSimulate(DeltaTime);
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
            var objs = GetIterationSnapshot();
            for (var i = 0; i < objs.Count; i++)
            {
                var monoObject = objs[i];
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsUpdateSimulatesNeeded)
                    {
                        Profiler.BeginSample("AfterUpdate", monoObject);
                        monoObject.AfterUpdate();
                        Profiler.EndSample();
                    }
                }
            }

            // else
            //     Debug.LogWarning("A mono object is null or not active and enabled, skipping after update.");
        }

#if UNITY_EDITOR

        //這段是我把unity binding拿掉，然後又自己補回來...
        [MenuItem("MonoFSM/ResetLevel %_R")]
        public static void ManualResetLevelMenu()
        {
            if (!Application.isPlaying)
            {
                // CompilationPipeline.RequestScriptCompilation();
                //refresh editor
                Debug.Log("Refresh");
                AssetDatabase.Refresh();
                return;
            }
            //改走CheatManager了比較對？
            // ManualResetLevel();
        }
#endif

        // ===================== Level Reset 網路廣播（ILevelResetBroadcaster 註冊） =====================

        private static readonly List<ILevelResetBroadcaster> _resetBroadcasters = new();

        public static void RegisterResetBroadcaster(ILevelResetBroadcaster broadcaster)
        {
            if (broadcaster != null && !_resetBroadcasters.Contains(broadcaster))
                _resetBroadcasters.Add(broadcaster);
        }

        public static void UnregisterResetBroadcaster(ILevelResetBroadcaster broadcaster)
        {
            _resetBroadcasters.Remove(broadcaster);
        }

        /// <summary>
        ///     優先挑 SA 端的 broadcaster 直接 bump（multi-peer 下 host/client 兩顆都註冊著，
        ///     挑 SA 那顆可以省一趟 RPC，也讓同 frame 的重複請求在同一顆上被 tick 去重）。
        /// </summary>
        private static bool TryBroadcastResetToNetwork(bool isHardReset)
        {
            ILevelResetBroadcaster fallback = null;
            foreach (var b in _resetBroadcasters)
            {
                if (b is UnityEngine.Object o && o == null) continue; //已被 Destroy 的殘留註冊
                if (b.IsResetAuthority)
                    return b.TryBroadcastReset(isHardReset);
                fallback ??= b;
            }

            return fallback != null && fallback.TryBroadcastReset(isHardReset);
        }

        /// <summary>
        ///     Build 版的 reset 權限閘門：由網路層（Fusion）在啟動時掛上，回傳「本機是否有權發動 level reset」。
        ///     沒掛（純單機 build）時視為有權。Editor 不套用，方便 multi-peer 測試時從任一端 reset。
        /// </summary>
        public static Func<bool> LocalResetAuthorityCheck;

        private static bool IsResetAllowedOnThisPeer()
        {
#if UNITY_EDITOR
            return true;
#else
            if (LocalResetAuthorityCheck == null)
                return true;
            if (LocalResetAuthorityCheck())
                return true;
            Debug.Log("[ResetLevel] 非權威端（client），忽略 ManualResetLevel");
            return false;
#endif
        }

        public static void ManualResetLevel(bool isHardReset = false) //Cheat Reset?
        {
            Debug.Log("ResetLevel CMD+Shift+R isHardReset:" + isHardReset);
            if (!IsResetAllowedOnThisPeer())
                return;

            //有網路 broadcaster 就走 SA 廣播：SA bump 版本號，各 peer 收到後各自 ResetLevel 自己的 simulator
            if (TryBroadcastResetToNetwork(isHardReset))
            {
                Debug.Log("ResetLevel routed to network broadcaster (SA 廣播，各 peer 本地 reset)");
                return;
            }

            RequestLocalReset(isHardReset);
        }

        //按鍵偵測必須留在 Update（Input System 的 wasPressedThisFrame 只有 Update 準），
        //但 reset 不能在 tick 外執行 → 只記 pending，實際在 Simulate 開頭做。
        private static bool _pendingLocalReset;
        private static bool _pendingLocalHardReset;

        /// <summary>
        ///     排程一次本地 reset，延到下一次 Simulate 開頭執行（同 frame 的重複請求會被併成一次）。
        /// </summary>
        public static void RequestLocalReset(bool isHardReset = false)
        {
            _pendingLocalReset = true;
            _pendingLocalHardReset |= isHardReset;
            Debug.Log($"ResetLevel 請求已排程，延到下個 Simulate 執行 isHardReset:{isHardReset}");
        }

        private static void ConsumePendingLocalReset()
        {
            if (!_pendingLocalReset)
                return;

            _pendingLocalReset = false;
            var isHardReset = _pendingLocalHardReset;
            _pendingLocalHardReset = false;
            ManualResetLevelLocal(isHardReset);
        }

        /// <summary>
        ///     純本地 reset：把同進程所有 simulator 全部重置（無連線 / broadcaster 尚未 Spawned 的 fallback）。
        ///     只該從 Simulate 開頭（ConsumePendingLocalReset）呼叫，別直接從 Update 叫。
        /// </summary>
        public static void ManualResetLevelLocal(bool isHardReset = false)
        {
            var simulators = FindObjectsByType<WorldUpdateSimulator>(FindObjectsSortMode.None);
            //FIXME: 會拿到Temporary Runner Prefab所以才全拿
            if (simulators.Length == 0)
                Debug.LogError(
                    "No WorldUpdateSimulator found in the scene. Ensure it is present for proper reset."
                );
            else
            {
                foreach (var simulator in simulators)
                    simulator._poolManager.ReturnAllObjects(); //FIXME: 這個把玩家有回收了
                //讓 SpawnProcessor 還原被 despawn 的物件（ex: Fusion scene NetworkObject 重新註冊）
                //注意：不能只通知 _spawnProcessor，同物件上的其他 handler（ex: ProximitySpawnDirector）
                //也要清追蹤狀態，否則 ReturnAllObjects 回收的物件會被誤判為「被消耗」
                foreach (var simulator in simulators)
                foreach (var resetHandler in simulator.GetComponents<ILevelResetSpawnHandler>())
                    resetHandler.OnBeforeLevelReset();
                foreach (var simulator in simulators)
                    //這樣就可以reset了
                    simulator.ResetLevelRestore(isHardReset);
                foreach (var simulator in simulators)
                    //這樣就可以reset了
                    simulator.ResetLevelStart();
            }
        }

        /// <summary>
        ///     單一 simulator 的完整 reset 序列（給網路廣播的每個 peer 對自己 runner 上的 simulator 呼叫）。
        /// </summary>
        public void ResetLevel(bool isHardReset = false)
        {
            Debug.Log($"WorldUpdateSimulator.ResetLevel isHardReset:{isHardReset}", this);
            _poolManager.ReturnAllObjects();
            foreach (var resetHandler in GetComponents<ILevelResetSpawnHandler>())
                resetHandler.OnBeforeLevelReset();
            ResetLevelRestore(isHardReset);
            ResetLevelStart();
        }

        public void BeforeRender()
        {
            if (!IsReady)
                return;
            CurrentPhase = SimPhase.BeforeRender;
            var objs = GetIterationSnapshot();
            for (var i = 0; i < objs.Count; i++)
            {
                var monoObject = objs[i];
                if (monoObject is { isActiveAndEnabled: true })
                {
                    Profiler.BeginSample("Render", monoObject);
                    monoObject.AfterRender();
                    Profiler.EndSample();
                }
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
            _deltaTime = deltaTime;
            CurrentPhase = SimPhase.Render;
            //snapshot 迭代：Render 中 SpawnVisual 註冊新 MonoObj 不會打斷迭代，新物件下一輪才開始更新
            var objs = GetIterationSnapshot();
            for (var i = 0; i < objs.Count; i++)
            {
                var monoObject = objs[i];
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsRenderSimulatesNeeded)
                    {
                        Profiler.BeginSample("Render", monoObject);
                        monoObject.Render(DeltaTime * localAlpha);
                        Profiler.EndSample();
                    }
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
            var objs = GetIterationSnapshot();
            for (var i = 0; i < objs.Count; i++)
            {
                var monoObject = objs[i];
                if (monoObject is { isActiveAndEnabled: true })
                {
                    if (monoObject.IsAfterRendersNeeded)
                    {
                        Profiler.BeginSample("Render", monoObject);
                        monoObject.AfterRender();
                        Profiler.EndSample();
                    }
                }
            }
        }
    }
}
