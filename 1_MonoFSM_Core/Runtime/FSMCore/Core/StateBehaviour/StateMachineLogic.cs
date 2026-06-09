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

namespace Fusion.Addons.FSM
{
    public interface IStateMachineController
    {
        public float DeltaTime { get; }
    }

    public interface IStateMachineOwner
    {
        void CollectStateMachines(List<IStateMachine> stateMachines);
        string name { get; }
        Transform transform { get; }
    }

    [DisallowMultipleComponent]
    public class StateMachineLogic : MonoBehaviour, IResetStart
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

        protected List<IStateMachine> _stateMachinesInternal = new(32);
        public List<IStateMachine> StateMachines => _stateMachinesInternal;

        protected List<IState> _statePool; // Used by CheckDuplicateStates

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

        public bool IsCurrentState(IState state)
        {
            if (state == null) return false;
            if (!_stateMachinesCollected) return false;
            if (_owners == null) return false;
            foreach (var owner in _owners)
                if (owner != null && owner.IsCurrentState(state))
                    return true;
            return false;
        }

        [ShowInInspector]
        private IState PreviousState
        {
            get
            {
                if (!_stateMachinesCollected) return null;
                if (_owners == null || _owners.Length == 0) return null;
                return _owners[0]?.PreviousState;
            }
        }

        [ShowInInspector]
        public IState CurrentState
        {
            get
            {
                if (!_stateMachinesCollected) return null;
                if (_owners == null || _owners.Length == 0) return null;
                return _owners[0]?.CurrentState;
            }
        }

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

        [SerializeField] MonoFSMOwner[] _owners;

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
                Debug.Log(
                    $"StateMachineLogic: Binding module pack folders for entity {entity.name}",
                    this);
                entity.BindModulePackFolders();
            }


            _stateMachinesInternal.Clear();
            if (_statePool != null)
                _statePool.Clear();

            // Get IStateMachineOwner components from children of this GameObject.
            // var owners = GetComponentsInChildren<IStateMachineOwner>(true);
            if (_owners.Length == 0)
                _owners = GetComponentsInChildren<MonoFSMOwner>(true);
            var owners = _owners;
            // Assuming ListPool is a static utility class available.
            // If not, replace with: var tempMachines = new List<IStateMachine>(32);
            // var tempMachines = new List<IStateMachine>(32); // Placeholder if ListPool is not found
            var tempMachines = ListPool.Get<IStateMachine>(32);

            for (var i = 0; i < owners.Length; i++)
            {
                var owner = owners[i] as IStateMachineOwner;
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
            List<IStateMachine> machines
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
        protected void CheckDuplicateStates(string stateMachineName, IState[] states)
        {
            if (states == null || states.Length == 0)
                return;

            if (_statePool == null)
                _statePool = new List<IState>(128);

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
