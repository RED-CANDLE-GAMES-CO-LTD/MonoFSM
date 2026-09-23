using System;
using System.Collections.Generic;
using System.Diagnostics;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Runtime;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MonoFSM.FSM
{
    public interface IStateMachineController
    {
        public float DeltaTime { get; }
    }

    public interface IStateMachineOwner
    {
        void CollectStateMachines(List<IMonoStateMachine> stateMachines);
        string name { get; }
        Transform transform { get; }
    }

    /// <summary>
    /// 驅動同一個 MonoEntity 範圍內所有 MonoFSMOwner 的 tick / restore 入口。
    /// owner 清單存 prefab 時自動從 parent MonoEntity 撈（CollectOwners），nested MonoEntity 底下的歸它們自己的 Logic 管。
    /// </summary>
    [DisallowMultipleComponent]
    public class StateMachineLogic : MonoBehaviour, IResetStart, IBeforePrefabSaveCallbackReceiver
    {
        [AutoParent]
        private MonoEntity _parentEntity;
        public MonoEntity ParentEntity => _parentEntity;

        // [ShowInInspector]
        public float DeltaTime => WorldUpdateSimulator.DeltaTime;

        // #if UNITY_EDITOR
        /// <summary>
        /// 確保有controller才會執行
        /// </summary>
        [CompRef]
        [Required]
        [Auto]
        private IStateMachineController _stateMachineController;

        // #endif
        [SerializeField]
        private bool _backingEnableLogging = false;

        public bool EnableLogging
        {
            get => _backingEnableLogging;
            set => _backingEnableLogging = value;
        }

        protected List<IMonoStateMachine> _stateMachinesInternal = new(32);
        public List<IMonoStateMachine> StateMachines => _stateMachinesInternal;

        protected List<IMonoState> _statePool; // Used by CheckDuplicateStates

        public void RestoreState(int stateId)
        {
            if (EnableLogging)
                Debug.Log($"Restoring state to ID {stateId} on {gameObject.name}", this);
            if (_owners != null)
                foreach (var owner in _owners)
                    if (owner != null)
                        owner.RestoreState(stateId);
        }

        public bool HasPendingRestore()
        {
            if (_owners == null) return false;
            foreach (var owner in _owners)
                if (owner != null && owner.stateIdToRestore != -1)
                    return true;
            return false;
        }

        public void RestoreAllPending()
        {
            if (_owners == null) return;
            foreach (var owner in _owners)
            {
                if (owner == null) continue;
                if (owner.stateIdToRestore == -1) continue;
                owner.ForceActivateState(owner.stateIdToRestore, true);
                owner.stateIdToRestore = -1;
            }
        }

        //FIXME: module pack也要？
        // [AutoChildren] public AnyState anyState;

        [PreviewInDebugMode]
        public bool _stateMachinesCollected { get; protected set; }
        public bool _manualUpdateMode { get; protected set; }

        // public bool IsCurrentState(IMonoState state)
        // {
        //     if (state == null) return false;
        //     if (!_stateMachinesCollected) return false;
        //     if (_owners == null) return false;
        //     foreach (var owner in _owners)
        //         if (owner != null && owner.IsCurrentState(state))
        //             return true;
        //     return false;
        // }

        // [ShowInInspector]
        // private IMonoState PreviousState
        // {
        //     get
        //     {
        //         if (!_stateMachinesCollected) return null;
        //         if (_owners == null || _owners.Length == 0) return null;
        //         return _owners[0]?.PreviousState;
        //     }
        // }
        //
        // [ShowInInspector]
        // public IMonoState CurrentState
        // {
        //     get
        //     {
        //         if (!_stateMachinesCollected) return null;
        //         if (_owners == null || _owners.Length == 0) return null;
        //         return _owners[0]?.CurrentState;
        //     }
        // }

        // Called by controllers to initialize.
        public void InitializeLogic()
        {
            if (!_stateMachinesCollected)
                CollectStateMachines();
            // Debug.Log($"Initializing MonoStateMachineController on {gameObject.name}");
        }

        public void SetManualUpdateMode(bool manualUpdate)
        {
            _manualUpdateMode = manualUpdate;
        }

        /// <summary>
        /// 這顆 Logic 要驅動的 owner。存 prefab 時由 CollectOwners 重算，不用手填。
        /// </summary>
        [SerializeField]
        [Tooltip("存 prefab 時自動從 parent MonoEntity 撈；nested MonoEntity 底下、或節點被關掉的不算")]
        MonoFSMOwner[] _owners;

        /// <summary>
        /// 自動規則撈不到、但還是要由這顆 Logic 驅動的 owner（例如擺在 entity 範圍外、或 nested entity 底下）。
        /// CollectOwners 會把它們併進 _owners，重複的只算一次。
        /// </summary>
        [SerializeField]
        [Tooltip("手動指定的 owner，CollectOwners 時併進 _owners（重複的會去掉）")]
        MonoFSMOwner[] _manualOwners;

        /// <summary>
        /// 從 parent MonoEntity 範圍內撈出所有該由這顆 Logic 驅動的 MonoFSMOwner。
        /// 規則跟 MonoEntity.BindModulePackFolders 一致：
        /// 1. owner 最近的 MonoEntity 必須是 parent entity（nested entity 有自己的 Logic）
        /// 2. owner 到 entity 之間任何節點 activeSelf == false 就跳過（關掉的 module 當註解）
        /// 只認 MonoFSMOwner，不看 StateFolder —— 掛 _bindingRoot 併進宿主的 StateFolder 沒有 owner，自然排除。
        /// Logic 可能是 owner 的兄弟節點（PPlayer 的 NetworkFSM Controller），所以起點是 entity 不是自己。
        /// 找不到 MonoEntity 時退回從自己往下撈。
        /// </summary>
        [Button]
        public void CollectOwners()
        {
            var entity = _parentEntity != null
                ? _parentEntity
                : GetComponentInParent<MonoEntity>(true);
            MonoFSMOwner[] candidates;
            var result = ListPool.Get<MonoFSMOwner>(16);
            if (entity == null)
            {
                candidates = GetComponentsInChildren<MonoFSMOwner>(true);
                result.AddRange(candidates);
            }
            else
            {
                candidates = entity.GetComponentsInChildren<MonoFSMOwner>(true);
                var entityTr = entity.transform;
                foreach (var owner in candidates)
                {
                    if (owner == null) continue;
                    //nested entity 底下的歸它自己的 Logic 管
                    if (owner.GetComponentInParent<MonoEntity>(true) != entity) continue;
                    //owner 到 entity 之間有節點被關掉 → 當註解跳過（含 owner 節點本身，不含 entity 節點）
                    if (HasInactiveNodeBelow(owner.transform, entityTr)) continue;
                    result.Add(owner);
                }
            }

            if (_manualOwners != null)
                foreach (var owner in _manualOwners)
                    if (owner != null && !result.Contains(owner))
                        result.Add(owner);

            _owners = result.ToArray();
            ListPool.Return(result);
        }

        private static bool HasInactiveNodeBelow(Transform from, Transform stopAt)
        {
            for (var t = from; t != null && t != stopAt; t = t.parent)
                if (!t.gameObject.activeSelf)
                    return true;
            return false;
        }

        public void OnBeforePrefabSave()
        {
            CollectOwners();
        }

        //FIXME: 到處亂叫，不爽, InitializeLogic & CollectStateMachines
        public void CollectStateMachines()
        {
            //先確保 ModulePack folder 已合併進 entity folders
            //Fusion 在 attach 時查 DynamicWordCount 就會打進來，比 Awake 還早；
            //root inactive 時 MonoEntity.Awake 更是完全不會跑，不能依賴 Awake 時序
            var entity = _parentEntity != null
                ? _parentEntity
                : GetComponentInParent<MonoEntity>(true);
            if (entity != null)
            {
                // Debug.Log(
                //     $"StateMachineLogic: Binding module pack folders for entity {entity.name}",
                //     this);
                entity.BindModulePackFolders();
            }


            _stateMachinesInternal.Clear();
            if (_statePool != null)
                _statePool.Clear();

            // 正常情況 _owners 在存 prefab 時就填好了（OnBeforePrefabSave → CollectOwners）。
            // 剛 AddComponent、或 scene 上直接擺的 FSM 沒存過 prefab 時才會是空的，runtime 補撈一次。
            if (_owners == null || _owners.Length == 0)
                CollectOwners();
            var owners = _owners;
            // Assuming ListPool is a static utility class available.
            // If not, replace with: var tempMachines = new List<IStateMachine>(32);
            // var tempMachines = new List<IStateMachine>(32); // Placeholder if ListPool is not found
            var tempMachines = ListPool.Get<IMonoStateMachine>(32);

            for (var i = 0; i < owners.Length; i++)
            {
                var owner = owners[i] as IStateMachineOwner;
                if (owner == null)
                {
                    Debug.LogError("owner is null", this);
                    continue;
                }
                owner.CollectStateMachines(tempMachines);
                CheckCollectedMachines(owners[i], tempMachines);

                for (var j = 0; j < tempMachines.Count; j++)
                {
                    var stateMachine = tempMachines[j];
                    if (_stateMachinesInternal.Contains(stateMachine))
                    {
                        Debug.LogError(
                            $"Trying to add already collected state machine for second time {stateMachine.Name}",
                            gameObject
                        );
                        continue;
                    }

                    CheckDuplicateStates(stateMachine.Name, stateMachine.States);
                    _stateMachinesInternal.Add(stateMachine);
                }

                tempMachines.Clear();
            }

            _stateMachinesCollected = true;
            // If using a real ListPool:
            ListPool.Return(tempMachines);
        }

        [Conditional("DEBUG")]
        protected void CheckCollectedMachines(
            IStateMachineOwner owner,
            List<IMonoStateMachine> machines
        )
        {
            if (machines.Count == 0)
            {
                var ownerObject = ((Component)owner).gameObject;
                Debug.LogWarning(
                    $"No state machines collected from the state machine owner {ownerObject.name}",
                    ownerObject
                );
            }
        }

        [Conditional("DEBUG")]
        protected void CheckDuplicateStates(string stateMachineName, IMonoState[] states)
        {
            if (states == null || states.Length == 0)
                return;

            if (_statePool == null)
                _statePool = new List<IMonoState>(128);

            foreach (var state in states)
            {
                if (state == null)
                    continue;

                if (_statePool.Contains(state) == true)
                    throw new InvalidOperationException(
                        $"State {state.Name} is used for multiple state machines, this is not allowed! State Machine: {stateMachineName}"
                    );

                _statePool.Add(state);
            }
        }

        public void ResetStart()
        {
            InitializeLogic();
            foreach (var stateMachine in StateMachines)
                stateMachine.Reset();
            //hmm depends太多了
            RestoreState(0);
            //network會失敗嗎？
        }
    }
}
