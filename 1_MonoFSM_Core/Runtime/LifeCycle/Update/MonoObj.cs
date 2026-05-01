using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Auto.Utils;
using Cysharp.Threading.Tasks;
using Fusion;
using MonoDebugSetting;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.LifeCycle;
using MonoFSM.Core.Simulate;
using MonoFSM.Culling;
using MonoFSM.CustomAttributes;
using MonoFSM.Runtime;
using MonoFSM.Variable.Attributes;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSMCore.Runtime.LifeCycle
{
    public interface IMonoObjectProvider : ICompProvider<MonoObj> //FIXME:這個不是很好...trace不到，最好還是都過一層？
    {
        //FIXME: 需要提供 EntityTag嗎？還是說MonoPoolObj就有EntityTag了？那從 bindPrefab就要有EntityTag

        //這個是給MonoPoolObj用的
        // MonoPoolObj GetMonoObject();
    }

    //1. 先回狀態
    public interface IResetStateRestore //新規用這個，現在和上面都有call, exitLevelAndDestroy是為了換場景很煩可以拔掉
    {
        void ResetStateRestore(bool isHardReset);
    }

    //2. 再跑這個
    public interface IResetStart //摸別人,set 變數之類的，要不然會reset掉
    {
        void ResetStart(); //不管 active, 可以後綴 force?
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
    [FormerlyNamedAs("MonoPoolObj")]
    public sealed class MonoObj : MonoBehaviour, IPrefabSerializeCacheOwner, IDropdownRoot,
        ISceneAwake
    {
        private bool _isAwakeActive = true;

        [Auto] private PoolObject _poolObject;

        [ShowInInspector] public bool isSceneObj => _poolObject == null || !_poolObject.IsFromPool;
        [ShowInInspector] public bool isPoolObj => _poolObject != null && _poolObject.IsFromPool;
        [ShowInInspector]
        [field: AutoChildren] //Children? //FIXME: 要弄成必定同一層，還是因為MonoObj 包一層 FSM的case很多？
        public MonoEntity Entity { get; }

        public T GetCompCache<T>() where T : Component
        {
            return Entity.GetCompCache<T>();
        }
        // public
        //寫一個show error的Attribute，然後在這裡用
        [InfoBox(
            "WorldUpdateSimulator is required for MonoPoolObj to function properly",
            InfoMessageType.Error,
            nameof(RuntimeCheckNoWorldUpdateSimulator)
        )]
        [ShowInDebugMode]
        public WorldUpdateSimulator WorldUpdateSimulator
        {
            get
            {
                if (HasParent)
                    return _parentObj.WorldUpdateSimulator;
                return _worldUpdateSimulator;
            }
        }

        // set => _worldUpdateSimulator = value;
        public void SetWorldUpdateSimulator(WorldUpdateSimulator world)
        {
            // if (world == null)
            //     Debug.Log("Clearing WorldUpdateSimulator" + name, this);
            // else
            //     Debug.Log("Set WorldUpdateSimulator" + name + " world:" + world, this);
            _worldUpdateSimulator = world;
        }

        bool RuntimeCheckNoWorldUpdateSimulator =>
            WorldUpdateSimulator == null && Application.isPlaying;

        public void Despawn()
        {
            //會跑兩次嗎？
            // Debug.Log("Despawn" + name, this);
            if (WorldUpdateSimulator == null)
            {
                Debug.LogError(
                    "WorldUpdateSimulator is not set. Cannot despawn MonoPoolObj.",
                    this
                );

                Destroy(gameObject); //直接刪掉，因為沒有模擬器可以處理了
                return;
            }

            //回傳root mono Obj
            //下個 frame再做？
            WorldUpdateSimulator.Despawn(GetMonoObjRoot());
        }

        private MonoObj GetMonoObjRoot()
        {
            if (HasParent)
                return _parentObj.GetMonoObjRoot();
            return this;
        }

        private void OnDestroy()
        {
            //fixme: ??
            //play mode 被刪掉要怎麼處理？
            // Debug.Log("MonoObj OnDestroy" + name, this);
        }

        [PreviewInDebugMode]
        [AutoChildren]
        private ISceneAwake[] _sceneAwakes;

        [PreviewInDebugMode]
        [AutoChildren]
        private ISceneStart[] _sceneStarts;

        [PreviewInDebugMode]
        [AutoChildren]
        private ISceneDestroy[] _sceneDestroys;

        [PreviewInDebugMode]
        [AutoChildren]
        private IResetStateRestore[] _resetStateRestores;

        [PreviewInDebugMode]
        [AutoChildren]
        private IResetStart[] _resetStarts;

        [PreviewInDebugMode]
        [AutoChildren]
        private IInstantiated[] _instantiateds;

        [PreviewInDebugMode]
        [AutoChildren(StopAtType = typeof(MonoObj))]
        private IUpdateSimulate[] _updateSimulates;

        [PreviewInDebugMode]
        [AutoChildren(StopAtType = typeof(MonoObj))]
        private IBeforeSimulate[] _beforeSimulates;

        [PreviewInDebugMode]
        [AutoChildren(StopAtType = typeof(MonoObj))]
        private IAfterSimulate[] _afterSimulates;

        [PreviewInDebugMode] [AutoChildren(StopAtType = typeof(MonoObj))] private IRenderSimulate[] _renderSimulates;
        [PreviewInDebugMode] [AutoChildren(StopAtType = typeof(MonoObj))] private IAfterRenderMono[] _afterRenders;

        // [PreviewInInspector]
        // [AutoChildren]
        // private IAfterUpdate[] _updateSimulates;

        //遞迴檢查 scope + 所有直屬 child subtree，任一有 item 且該 node 未被 cull 就回 true
        //Root cull → false; 否則自己 scope 空但 child 有東西也要回 true（不然 WorldUpdateSimulator 會 skip 整個 tree）
        public bool IsUpdateSimulatesNeeded => CheckPhaseNeededRecursive(self => self._updateSimulates);
        public bool IsBeforeSimulatesNeeded => CheckPhaseNeededRecursive(self => self._beforeSimulates);
        public bool IsAfterSimulatesNeeded => CheckPhaseNeededRecursive(self => self._afterSimulates);
        public bool IsRenderSimulatesNeeded => CheckPhaseNeededRecursive(self => self._renderSimulates);

        private bool CheckPhaseNeededRecursive<T>(Func<MonoObj, T[]> getList) where T : class
        {
            if (IsCulling) return false;
            var list = getList(this);
            if (list != null && list.Length > 0) return true;
            if (_childrenObjs == null) return false;
            for (var i = 0; i < _childrenObjs.Length; i++)
            {
                var c = _childrenObjs[i];
                if (c != null && c != this && c.CheckPhaseNeededRecursive(getList)) return true;
            }
            return false;
        }

        //FIXME: PoolBeforeReturnToPool? OnReturnPool?

        [PreviewInDebugMode]
        private MonoObj _parentObj;

        [AutoChildren(StopAtType = typeof(MonoObj), IncludeStopNode = true)]
        MonoObj[] _childrenObjs;

        // [SerializeField]
        private WorldUpdateSimulator _worldUpdateSimulator;

        public bool HasParent => _parentObj != null; //有_parentObj就表示是nested的pool object，不作用，交給parent處理

        private void Awake()
        {
            // Init();
            //把 PrefabSerializeCache 的實作拿過來？
        }

        public void InitParentLinks()
        {
            //_childrenObjs 現在只含直屬 child MonoObj（StopAtType = typeof(MonoObj), IncludeStopNode = true）
            //遞迴 walk down，讓每個 MonoObj 的 _parentObj 指向直屬 parent
            if (_childrenObjs == null) return;
            foreach (var item in _childrenObjs)
            {
                if (item == null || item == this)
                    continue;
                item._parentObj = this;
                item.InitParentLinks();
            }
        }

        void Init()
        {
            InitParentLinks();

            if (HasParent)
                return;
// #if UNITY_EDITOR
//             AutoAttributeManager.AutoReferenceAllChildren(gameObject);
// #endif
            SortUpdateSimulates();
            //FIXME: prefab cache restore?
        }

        private void SortUpdateSimulates()
        {
            if (_updateSimulates is { Length: > 1 })
                Array.Sort(
                    _updateSimulates,
                    (a, b) =>
                    {
                        var orderA = a?.SimulateOrder ?? 0;
                        var orderB = b?.SimulateOrder ?? 0;
                        return orderA.CompareTo(orderB);
                    }
                );

            if (_beforeSimulates is { Length: > 1 })
                Array.Sort(
                    _beforeSimulates,
                    (a, b) =>
                    {
                        var orderA = a?.BeforeSimulateOrder ?? 0;
                        var orderB = b?.BeforeSimulateOrder ?? 0;
                        return orderA.CompareTo(orderB);
                    }
                );
        }

        public void SpawnFromPool() //必定是root吧
        {
            ResetStateRestore(false);
            ResetStart();
        }

        public void SceneAwake(WorldUpdateSimulator world) //可以自己sceneＡwake吧？
        {
            if (gameObject.activeSelf == false) //原本 scene上就關掉的物件 (測試用
            {
                _isAwakeActive = false; //不參與 reset activate
            }
            SetWorldUpdateSimulator(world);
            Init();
            if (HasParent)
                return;

            HandleIAwake();
            //這可以嗎？
            HandleIInstantiated(world); //和IAwake合併？
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

        public void
            ResetStateRestore(
                bool isHardReset) //還是要分兩階，先還原，再開始？ 還是說有這種dependency本身就不好...? life cycle集中化
        {
            if (HasParent)
                return;

            //在 scene 上的物件，回到初始狀態 (打開來)
            if (isSceneObj)
            {
                if (_isAwakeActive && gameObject.activeSelf == false) //原本打開的物件
                {
                    //WorldUpdateSimulator 沒有註冊的話也不會跑這個喔！
                    if (RuntimeDebugSetting.IsDebugMode)
                        Debug.Log("ResetStateRestore: Reactivating GameObject " + name, this);
                    // Debug.Break();
                    gameObject.SetActive(true);
                    WorldUpdateSimulator
                        .RegisterMonoObject(this);
                    // //回到pool的物件會被despawn刪掉，回到scene上的物件才需要註冊
                }
            }

            // Debug.Log("[MonoObj] HandleIResetStateRestore", this);
            foreach (var item in _resetStateRestores)
            {
                if (item == null)
                    continue;
                try
                {
                    item.ResetStateRestore(isHardReset);
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

        public void ResetStart()
        {
            if (HasParent)
                return;
            HandleIResetStart();
        }

        [AutoChildren(StopAtType = typeof(MonoObj))] public CullingActiveHandle _cullingHandle;

        public bool IsCulling =>
            _cullingHandle != null && !_cullingHandle.gameObject.activeSelf;

        [SerializeField] [AutoChildren] [CompRef]
        SpawnEventHandler _onSpawnHandler;

        public SpawnEventHandler OnSpawnHandler => _onSpawnHandler;


        [AutoChildren]
        [CompRef]
        private IAfterSpawnProcess[] _afterSpawnProcesses;

        /// <summary>
        /// 被 spawn 後由 SpawnAction 呼叫，讓物件自身的 IAfterSpawnProcess 也能處理
        /// </summary>
        public void HandleAfterSpawn(Vector3 position, Quaternion rotation,
            MonoFSM.Runtime.Interact.EffectHit.GeneralEffectHitData hitData)
        {
            OnSpawnHandler?.OnSpawn(this, position, rotation); //讓spawn出來的物件自己處理OnSpawn
            if (_afterSpawnProcesses == null) return;
            foreach (var process in _afterSpawnProcesses)
            {
                if (process == null) continue;
                try
                {
                    process.AfterSpawn(this, position, rotation, hitData);
                }
                catch (System.Exception e)
                {
                    if (process is MonoBehaviour mb)
                        Debug.LogException(e, mb);
                    else
                        Debug.LogException(e, this);
                }
            }
        }

        // [SerializeField] [AutoChildren] [CompRef]
        // private OnResetStartHandler _onResetStartHandler; //FIXME: 好像不用reference了吧？

        private void HandleIResetStart()
        {
            if (HasParent)
                return;
            foreach (var item in _resetStarts)
            {
                if (item == null)
                    continue;
                //FIXEM: 用trycatch不好debug? 但沒有trycatch會整個爛掉喔！
                try
                {
                    item.ResetStart();
                }
                catch (Exception e)
                {
                    if (item is MonoBehaviour)
                        Debug.LogException(e, item as MonoBehaviour);
                    // Debug.LogError(e.Message + "\n" + e.StackTrace, item as MonoBehaviour);
                    else
                        Debug.LogException(e, this);
                }
            }
        }

        [ShowInInspector] public bool IsProxy { get; set; } //沒在用？？

        //被 WorldUpdateSimulator 註冊後就不再反註冊；despawn 只把這個 flag 關掉，
        //各個 Simulate phase 在 root 層用此 flag 決定是否要跑。
        //ResetStateRestore 等 reset 流程仍會 iterate 整個 set，不受此 flag 影響。
        [ShowInInspector] public bool IsActiveInSimulator { get; set; } = true;

        public void BeforeSimulate(float deltaTime)
        {
            if (HasParent)
                return;
            if (IsProxy)
                return;
            if (!IsActiveInSimulator)
                return;
            TickBeforeSimulatePhase(deltaTime);
        }

        private void TickBeforeSimulatePhase(float deltaTime)
        {
            if (IsCulling) return; //我被 cull → 整棵子樹跳過
            if (_beforeSimulates != null)
            {
                foreach (var item in _beforeSimulates)
                {
                    if (item is not { isActiveAndEnabled: true })
                        continue;
                    item.BeforeSimulate(deltaTime);
                }
            }
            if (_childrenObjs == null) return;
            for (var i = 0; i < _childrenObjs.Length; i++)
            {
                var c = _childrenObjs[i];
                if (c != null && c != this) c.TickBeforeSimulatePhase(deltaTime);
            }
        }

        //理論上沒有註冊就不會call到這個
        public void Simulate(float deltaTime)
        {
            if (HasParent)
                return;
            //如果proxy就跳過？
            // if (IsProxy)
            //     return;
            if (!IsActiveInSimulator)
                return;
            TickSimulatePhase(deltaTime);
        }

        private void TickSimulatePhase(float deltaTime)
        {
            if (IsCulling) return; //我被 cull → 整棵子樹跳過
            if (_updateSimulates != null)
            {
                foreach (var item in _updateSimulates)
                {
                    if (item is not { IsValid: true })
                        continue;
                    Profiler.BeginSample("MonoObj.Simulate", item.gameObject);
                    try
                    {
                        item.Simulate(deltaTime);
                    }
                    catch (Exception e)
                    {
                        if (item is MonoBehaviour)
                            Debug.LogException(e, item as MonoBehaviour);
                        else
                            Debug.LogException(e, this);
                    }
                    Profiler.EndSample();
                }
            }
            if (_childrenObjs == null) return;
            for (var i = 0; i < _childrenObjs.Length; i++)
            {
                var c = _childrenObjs[i];
                if (c != null && c != this) c.TickSimulatePhase(deltaTime);
            }
        }

        public void AfterSimulate(float deltaTime)
        {
            if (HasParent)
                return;
            if (IsProxy)
                return;
            if (!IsActiveInSimulator)
                return;
            TickAfterSimulatePhase(deltaTime);
        }

        private void TickAfterSimulatePhase(float deltaTime)
        {
            if (IsCulling) return; //我被 cull → 整棵子樹跳過
            if (_afterSimulates != null)
            {
                foreach (var item in _afterSimulates)
                {
                    if (item is not { isActiveAndEnabled: true })
                        continue;
                    Profiler.BeginSample("MonoObj.AfterUpdate", item.gameObject);
                    item.AfterSimulate(deltaTime);
                    Profiler.EndSample();
                }
            }
            if (_childrenObjs == null) return;
            for (var i = 0; i < _childrenObjs.Length; i++)
            {
                var c = _childrenObjs[i];
                if (c != null && c != this) c.TickAfterSimulatePhase(deltaTime);
            }
        }

        public void Render(float deltaTimelocalAlpha)
        {
            if (HasParent)
                return;
            if (!IsActiveInSimulator)
                return;
            TickRenderPhase(deltaTimelocalAlpha);
        }

        private void TickRenderPhase(float deltaTimelocalAlpha)
        {
            if (IsCulling) return; //我被 cull → 整棵子樹跳過
            if (_renderSimulates != null)
            {
                foreach (var item in _renderSimulates) //如果 render有順序問題就哭惹？
                {
                    if (item is not { isActiveAndEnabled: true })
                        continue;
                    Profiler.BeginSample("MonoObj.Render", item.gameObject);
                    item.Render(deltaTimelocalAlpha);
                    Profiler.EndSample();
                }
            }
            if (_childrenObjs == null) return;
            for (var i = 0; i < _childrenObjs.Length; i++)
            {
                var c = _childrenObjs[i];
                if (c != null && c != this) c.TickRenderPhase(deltaTimelocalAlpha);
            }
        }

        //需要這個嗎？還是 AfterSimulate就好了？
        public void AfterUpdate()
        {
            if (HasParent)
                return;
            if (IsProxy)
                return;
            // foreach (var item in _updateSimulates)
            // {
            //     if (item is not { isActiveAndEnabled: true })
            //         continue;
            //     Profiler.BeginSample("MonoObj.AfterUpdate", item.gameObject);
            //     item.AfterUpdate();
            //     Profiler.EndSample();
            // }
        }

        /// <summary>
        /// 兩個進入點，SpawnFromPool 和 SceneAwake
        /// 1. SpawnFromPool 是從Pool中取出來的物件，
        /// 2. SceneAwake ?
        /// </summary>
        private void HandleIAwake()
        {
            var iLevelAwakes = new List<ISceneAwake>(_sceneAwakes);
            iLevelAwakes.Reverse();

            foreach (var item in iLevelAwakes)
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
            // if (WorldUpdateSimulator.IsReady == false)
            //     // Debug.LogError("WorldUpdateSimulator is not ready. Cannot proceed with SceneAwake.", this);
            //     return;
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

        [Button("Check Update Status")]
        private void CheckUpdateStatus()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MonoObj] CheckUpdateStatus 需在 Play Mode 下執行", this);
                return;
            }

            var root = GetMonoObjRoot();
            var assignedWorld = WorldUpdateSimulator;
            var allWorlds = FindObjectsByType<WorldUpdateSimulator>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            WorldUpdateSimulator owningWorld = null;
            foreach (var w in allWorlds)
            {
                if (w != null && w.IsRegistered(root))
                {
                    owningWorld = w;
                    break;
                }
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[MonoObj] Update Status for '{name}'");
            sb.AppendLine($"  HasParent: {HasParent} (root='{root.name}')");
            sb.AppendLine(
                $"  Assigned WorldUpdateSimulator: {(assignedWorld != null ? assignedWorld.name : "<null>")}");
            sb.AppendLine($"  Found in scene: {allWorlds.Length} WorldUpdateSimulator(s)");
            sb.AppendLine(
                $"  Registered in: {(owningWorld != null ? owningWorld.name : "<NONE — 不會被更新>")}");
            if (assignedWorld != null && owningWorld != assignedWorld)
                sb.AppendLine("  ⚠ Assigned 與實際註冊的 simulator 不一致");
            sb.AppendLine($"  IsActiveInSimulator: {IsActiveInSimulator}");
            sb.AppendLine($"  IsCulling: {IsCulling}");
            sb.AppendLine($"  IsProxy: {IsProxy}");
            sb.AppendLine(
                $"  Phase needed — Before:{IsBeforeSimulatesNeeded}  Update:{IsUpdateSimulatesNeeded}  After:{IsAfterSimulatesNeeded}  Render:{IsRenderSimulatesNeeded}");

            var willUpdate = owningWorld != null && !HasParent && IsActiveInSimulator && !IsCulling;
            sb.AppendLine($"  => 會被 WorldUpdateSimulator 更新嗎？ {(willUpdate ? "YES" : "NO")}");

            if (owningWorld != null)
                Debug.Log(sb.ToString(), this);
            else
                Debug.LogWarning(sb.ToString(), this);
        }

        [Button("Rename to Prefab Name")]
        private void RenameToPrefabName()
        {
#if UNITY_EDITOR
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab == null)
            {
                Debug.LogWarning("This GameObject is not a prefab instance.", this);
                return;
            }

            Undo.RecordObject(gameObject, "Rename to Prefab Name");
            gameObject.name = prefab.name;
#endif
        }

        public void AfterRender()
        {
            if (HasParent)
                return;
            if (!IsActiveInSimulator)
                return;
            TickAfterRenderPhase();
        }

        private void TickAfterRenderPhase()
        {
            if (IsCulling) return; //我被 cull → 整棵子樹跳過
            if (_afterRenders != null)
            {
                foreach (var item in _afterRenders) //如果 render有順序問題就哭惹？
                {
                    if (item is not { isActiveAndEnabled: true })
                        continue;
                    Profiler.BeginSample("MonoObj.Render", item.gameObject);
                    item.AfterRender();
                    Profiler.EndSample();
                }
            }
            if (_childrenObjs == null) return;
            for (var i = 0; i < _childrenObjs.Length; i++)
            {
                var c = _childrenObjs[i];
                if (c != null && c != this) c.TickAfterRenderPhase();
            }
        }

        public void EnterSceneAwake()
        {
        }
    }
}
