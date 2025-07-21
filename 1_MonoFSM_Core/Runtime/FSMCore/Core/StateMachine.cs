using System;
using System.Diagnostics;
using MonoDebugSetting;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Fusion.Addons.FSM
{
    public interface ITickProvider
    {
        int Tick { get; }

        float DeltaTime { get; }

        bool IsStage { get; }
        // Stage Stage { get; }
    }

    public partial class StateMachine<TState> : IStateMachine where TState : class, IState
    {
        // PUBLIC MEMBERS
        public void RestoreState(int stateId)
        {
            _activeStateId = stateId;
            _previousStateId = stateId;
        }
        public string Name { get; private set; }
        // public NetworkRunner Runner { get; private set; } //FIXME: 被強迫network了!

        public bool? EnableLogging { get; set; }

        public TState[] States => _states;

        public int ActiveStateId => _activeStateId;
        public int PreviousStateId => _previousStateId;
        public int DefaultStateId => _defaultStateId;
        public int StateChangeTick => _stateChangeTick;

        public float StateTime => _activeStateId >= 0 ? GetStateTime() : 0f;
        public int StateTicks => _activeStateId >= 0 ? GetStateTicks() : 0;

        public TState ActiveState => _activeStateId >= 0 ? _states[_activeStateId] : null;
        public TState PreviousState => _previousStateId >= 0 ? _states[_previousStateId] : null;

        public bool IsPaused
        {
            get => _bitState.IsBitSet(0);
            set => _bitState = _bitState.SetBitNoRef(0, value);
        }

        // PRIVATE MEMBERS

        private readonly TState[] _states;
        private readonly int _stateCount;
        private const int _noneStateId = -1;

        private int _lastRenderStateId = -1;
        private int _lastRenderStateChangeTick;
        private float _interpolationTick;

        // private NetworkStateMachineController _controller;
        public StateMachineLogic Logic => _logic;
        private StateMachineLogic _logic;
        private ITickProvider _tickProvider;
        public ITickProvider TickProvider => _tickProvider;

        // CONSTRUCTORS

        public StateMachine(string name, params TState[] states)
        {
            Name = name;

            _states = states;
            _stateCount = _states.Length;

            for (var i = 0; i < _states.Length; i++)
            {
                var state = _states[i];

                state.StateId = i;

                if (state is IOwnedState<TState> ownedState) ownedState.Machine = this;
            }
        }

        // PUBLIC METHODS

        public bool TryActivateState(int stateId, bool allowReset = false)
        {
            return TryActivateState(stateId, allowReset, false);
        }

        public bool ForceActivateState(int stateId, bool allowReset = false)
        {
            if (stateId == _activeStateId && allowReset == false)
                return false;

            ChangeState(stateId);
            return true;
        }

        public bool TryDeactivateState(int stateId)
        {
            if (stateId != _activeStateId)
                return true;

            return TryActivateState(_defaultStateId, false, true);
        }

        public bool ForceDeactivateState(int stateId)
        {
            if (stateId != _activeStateId)
                return true;

            return ForceActivateState(_defaultStateId);
        }

        public bool TryToggleState(int stateId, bool value)
        {
            var stateIsActive = stateId == _activeStateId;

            if (stateIsActive == value)
                return true;

            var targetState = value == true ? stateId : _defaultStateId;
            return TryActivateState(targetState, false, value == false);
        }

        public void ForceToggleState(int stateId, bool value)
        {
            var stateIsActive = stateId == _activeStateId;

            if (stateIsActive == value)
                return;

            var targetState = value == true ? stateId : _defaultStateId;
            ForceActivateState(targetState, false);
        }

        public bool HasState(TState state)
        {
            for (var i = 0; i < _stateCount; i++)
                if (_states[i].StateId == state.StateId && _states[i] == state)
                    return true;

            return false;
        }

        public TState GetState(int stateId)
        {
            if (stateId < 0 || stateId >= _stateCount)
                return default;

            return _states[stateId];
        }

        public T GetState<T>() where T : TState
        {
            for (var i = 0; i < _stateCount; i++)
                if (_states[i] is T state)
                    return state;

            return default;
        }

        public void SetDefaultState(int stateId)
        {
            if (stateId < 0 || stateId >= _stateCount)
            {
                Debug.LogError($"SetDefaultState: Invalid state Id {stateId}");
                return;
            }

            _defaultStateId = stateId;
        }

        public void Reset()
        {
            _activeStateId = _noneStateId;
            _previousStateId = _noneStateId;
            _stateChangeTick = 0;
            _bitState = 0;

            _lastRenderStateId = _noneStateId;

            LogReset();

            // if (_hasChildMachines == true)
            //     for (var i = 0; i < _stateCount; i++)
            //     {
            //         var state = _states[i];
            //
            //         for (var j = 0; j < state.ChildMachines.Length; j++) state.ChildMachines[j].Reset();
            //     }
        }

        // IStateMachine INTERFACE

        // void IStateMachine.Initialize(NetworkStateMachineController controller, NetworkRunner runner)
        void IStateMachine.Initialize(StateMachineLogic logic, ITickProvider tickProvider)
        {
            _tickProvider = tickProvider;
            Debug.Log("tickProvider"+tickProvider);
            _logic = logic;

            for (var i = 0; i < _stateCount; i++)
            {
                var state = _states[i];

                // for (var j = 0; j < state.ChildMachines.Length; j++)
                //     state.ChildMachines[j].Initialize(controller, runner);

                state.Initialize();
            }
        }

        void IStateMachine.FixedUpdateNetwork() //FIXME: rename
        {
            if (IsPaused == true)
                return;

            if (_activeStateId < 0) ChangeState(_defaultStateId);

            // Active state could be changed in state's fixed update
            // Do not update its child machines in that case
            var updateStateId = _activeStateId;

            ActiveState.OnFixedUpdate();

          
            // if (updateStateId == _activeStateId)
            //     for (var i = 0; i < ActiveState.ChildMachines.Length; i++)
            //         ActiveState.ChildMachines[i].FixedUpdateNetwork();
        }

        void IStateMachine.Render()
        {
            if (IsPaused == true)
                return;

            if (_lastRenderStateId != _activeStateId || _lastRenderStateChangeTick != _stateChangeTick)
            {
                LogRenderStateChange();

                if (_lastRenderStateId >= 0)
                {
                    var lastRenderState = _states[_lastRenderStateId];

                    lastRenderState.OnExitStateRender();

                    // When transitioning to a new state make sure Render is called one last time on all child machines
                    // - this will properly call OnExitStateRender callbacks and save render state change
                    // if (_lastRenderStateId != _activeStateId)
                    //     for (var i = 0; i < lastRenderState.ChildMachines.Length; i++)
                    //         lastRenderState.ChildMachines[i].Render();
                }

                if (_activeStateId >= 0) ActiveState.OnEnterStateRender();

                _lastRenderStateId = _activeStateId;
                _lastRenderStateChangeTick = _stateChangeTick;
            }

            if (_activeStateId < 0)
                return;

            ActiveState.OnRender();

            // for (var i = 0; i < ActiveState.ChildMachines.Length; i++) ActiveState.ChildMachines[i].Render();
        }

        void IStateMachine.Deinitialize(bool hasState)
        {
            for (var i = 0; i < _stateCount; i++)
            {
                var state = _states[i];

                state.Deinitialize(hasState);

                // for (var j = 0; j < state.ChildMachines.Length; j++) state.ChildMachines[j].Deinitialize(hasState);
            }

            _tickProvider = null;
        }

        // PRIVATE METHODS

        private bool TryActivateState(int stateId, bool allowReset, bool isExplicitDeactivation)
        {
            if (stateId == _activeStateId && allowReset == false)
                return false;

            var nextState = _states[stateId];

            if (ActiveState != null && ActiveState.CanExitState(nextState, isExplicitDeactivation) == false)
                return false;

            if (nextState.CanEnterState() == false)
                return false;

            ChangeState(stateId);
            return true;
        }

        private void ChangeState(int stateId)
        {
            if (stateId >= _stateCount)
                throw new InvalidOperationException($"State with ID {stateId} not present in the state machine {Name}");

            // Assert.Check(_tickProvider.Stage != default, "State changes are not allowed from Render calls");
            // Assert.Check(_tickProvider.IsStage, "State changes are not allowed from Render calls");

            _previousStateId = _activeStateId;
            _activeStateId = stateId;

            if (DebugSetting.IsDebugMode)
                LogStateChange();

            Profiler.BeginSample("Exit State");
            if (_previousStateId >= 0) PreviousState.OnExitState();
            Profiler.EndSample();
            // for (var i = 0; i < PreviousState.ChildMachines.Length; i++)
            //     // When parent state is deactivated, all child states are deactivated as well
            //     PreviousState.ChildMachines[i].ForceActivateState(_noneStateId);
            _stateChangeTick = _tickProvider.Tick;

            Profiler.BeginSample("Enter State");
            if (_activeStateId >= 0) ActiveState.OnEnterState();
            Profiler.EndSample();
            // for (var i = 0; i < ActiveState.ChildMachines.Length; i++)
            // {
            //     var childMachine = ActiveState.ChildMachines[i];
            //
            //     if (childMachine.ActiveState == null && childMachine.PreviousState != null)
            //         // When parent state is activated, all child states are re-activated as well
            //         childMachine.ForceActivateState(childMachine.PreviousState.StateId);
            // }
        }

        private int GetStateTicks()
        {
            var currentTick = _tickProvider.IsStage == false && _interpolationTick != 0f
                ? (int)_interpolationTick
                : _tickProvider.Tick;
            return currentTick - StateChangeTick;
        }

        private float GetStateTime() //沒搞懂這是啥
        {
            if (_tickProvider.IsStage || _interpolationTick == 0f)
                return (_tickProvider.Tick - StateChangeTick) * _tickProvider.DeltaTime;

            return (_interpolationTick - StateChangeTick) * _tickProvider.DeltaTime;
        }

        // LOGGING

        [Conditional("DEBUG")]
        private void LogStateChange()
        {
            if (_logic == null)
            {
                Debug.LogError(
                    $"{nameof(StateMachine<TState>)}: Logic is null for state machine {Name}. This should not happen.");
                return;
            }
                
            if (EnableLogging.HasValue == false && _logic.EnableLogging == false)
                return; // Global controller logging is disabled

            if (EnableLogging.HasValue == true && EnableLogging.Value == false)
                return; // Logging is specifically disabled for this machine

            var activeStateName = ActiveState != null ? ActiveState.Name : "None";
            var previousStateName = PreviousState != null ? PreviousState.Name : "None";
            Debug.Log(
                $"{_logic.gameObject.name} - <color=#F04C4C>State Machine <b>{Name}</b>: Change State to <b>{activeStateName}</b></color> - Previous: {previousStateName}, Tick: {_tickProvider.Tick}",
                _logic);
        }

        [Conditional("DEBUG")]
        private void LogReset()
        {
            if (_tickProvider == null)
                return;

            if (EnableLogging.HasValue == false && _logic.EnableLogging == false)
                return; // Global controller logging is disabled

            if (EnableLogging.HasValue == true && EnableLogging.Value == false)
                return; // Logging is specifically disabled for this machine

            Debug.Log(
                $"{_logic.gameObject.name} - <color=#F04C4C>State Machine <b>{Name}</b>: Machine <b>RESET</b></color> - Tick: {_tickProvider.Tick}",
                _logic);
        }

        [Conditional("DEBUG")]
        private void LogRenderStateChange()
        {
            if (EnableLogging.HasValue == false && _logic.EnableLogging == false)
                return; // Global controller logging is disabled

            if (EnableLogging.HasValue == true && EnableLogging.Value == false)
                return; // Logging is specifically disabled for this machine

            var activeStateName = ActiveState != null ? ActiveState.Name : "None";
            var previousStateName = _lastRenderStateId >= 0 ? _states[_lastRenderStateId].Name : "None";
            Debug.Log(
                $"{_logic.gameObject.name} - <color=#467DE7>State Machine <b>{Name}</b>: Change RENDER State to <b>{activeStateName}</b></color> - Previous: {previousStateName}, StateChangeTick: {_stateChangeTick}, RenderFrame: {Time.frameCount}",
                _logic);
        }
    }
}