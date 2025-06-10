using System;
using System.Collections.Generic;
using Fusion.Addons.FSM.Network;
using UnityEngine;
using UnityEngine.Profiling;

namespace Fusion.Addons.FSM
{
    //FIXME: 怎麼拆成  一般FSM和？
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StateMachineLogic))] // Ensure StateMachineLogic is present
    public sealed class NetworkStateMachineController : NetworkBehaviour, IBeforeAllTicks, IAfterTick,
        IStateMachineController
    {
        // PUBLIC MEMBERS

        public bool EnableLogging
        {
            get => _fsmLogic.EnableLogging;
            set => _fsmLogic.EnableLogging = value;
        }

        public IReadOnlyList<IStateMachine> StateMachines => _fsmLogic.StateMachines;

        // PRIVATE MEMBERS

        [Header("DEBUG")] [SerializeField] private bool
            _enableLogging; // Serialized for editor convenience, passed to logic. Removed default initialization.

        private StateMachineLogic _fsmLogic;
        // private BaseStateMachineControllerLogic _logic; // Replaced by _logicComponent
        // private List<IStateMachine> _stateMachines = new(32); // Moved to BaseStateMachineControllerLogic
        // private List<IState> _statePool; // Moved to BaseStateMachineControllerLogic

        // private bool _stateMachinesCollected; // Moved to BaseStateMachineControllerLogic
        private bool _manualUpdate; // Retained for network specific manual update logic

        // UNITY MESSAGES (Awake can be used for initialization)
        private void Awake()
        {
            _fsmLogic = GetComponent<StateMachineLogic>();
            // If you want to ensure it's added if not present, you could use:
            // _logicComponent = GetComponent<StateMachineLogic>();
            // if (_logicComponent == null) _logicComponent = gameObject.AddComponent<StateMachineLogic>();

            _fsmLogic.EnableLogging = _enableLogging; // Sync editor value
        }


        // PUBLIC METHODS

        public void SetManualUpdate(bool manualUpdate)
        {
            _manualUpdate = manualUpdate;
            _fsmLogic.SetManualUpdateMode(manualUpdate); // Also inform the base logic
        }

        public void ManualFixedUpdate()
        {
            if (!_manualUpdate) // Simplified
                throw new InvalidOperationException("Manual update is not turned on");

            if (Runner.Stage == default)
                throw new InvalidOperationException(
                    "ManualFixedUpdate needs to be called from simulation (from FixedUpdateNetwork call)");

            FixedUpdateInternal();
        }

        public void ManualRender()
        {
            if (!_manualUpdate) // Simplified
                throw new InvalidOperationException("Manual update is not turned on");

            if (Runner.Stage != default)
                throw new InvalidOperationException(
                    "ManualRender needs to be called outside of simulation (from Render call)");

            RenderInternal();
        }


        // NetworkBehaviour INTERFACE

        public override int? DynamicWordCount => GetNetworkDataWordCount();

        public override void Spawned()
        {
            var tickProvider = new NetworkTickProvider(Runner);
            // Ensure state machines are collected before use
            if (!_fsmLogic._stateMachinesCollected) _fsmLogic.CollectStateMachines();

            for (var i = 0; i < _fsmLogic.StateMachines.Count; i++) _fsmLogic.StateMachines[i].Reset();

            if (!HasStateAuthority) ReadNetworkData(); // Simplified

            for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
                _fsmLogic.StateMachines[i].Initialize(_fsmLogic, tickProvider);

            if (HasStateAuthority) WriteNetworkData(); // Simplified
        }

        public override void Render()
        {
            if (_manualUpdate) // Simplified
                return;

            RenderInternal();
        }

        public override void FixedUpdateNetwork()
        {
            if (_manualUpdate) // Simplified
                return;

            if (IsProxy) // Simplified
                return;

            FixedUpdateInternal();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (!hasState) // Simplified
                return;

            for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
                _fsmLogic.StateMachines[i].Deinitialize(hasState);
        }

        // IBeforeAllTicks INTERFACE

        void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
        {
            // Read before all ticks as state machine properties are used both for Render and FUN.
            // IAfterClientPredictionReset would not be enough.
            ReadNetworkData();
        }

        // IAfterTick INTERFACE

        void IAfterTick.AfterTick()
        {
            WriteNetworkData();
        }

        // PRIVATE METHODS

        private void FixedUpdateInternal()
        {
            if (!_fsmLogic._stateMachinesCollected) return; // Guard against calls before Spawned/collection
            for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
            {
                Profiler.BeginSample($"StateMachineController.FixedUpdate ({_fsmLogic.StateMachines[i].Name})");
                _fsmLogic.StateMachines[i].FixedUpdateNetwork();
                Profiler.EndSample();
            }
        }

        private void RenderInternal()
        {
            if (!_fsmLogic._stateMachinesCollected) return;
            if (!Interpolate()) // Simplified
                return;

            for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
            {
                Profiler.BeginSample($"StateMachineController.Render ({_fsmLogic.StateMachines[i].Name})");
                _fsmLogic.StateMachines[i].Render();
                Profiler.EndSample();
            }
        }

        private int GetNetworkDataWordCount() // Removed unsafe keyword if present, as it's not needed here.
        {
            if (!_fsmLogic._stateMachinesCollected) _fsmLogic.CollectStateMachines();

            var wordCount = 0;

            for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
                wordCount += _fsmLogic.StateMachines[i].WordCount;

            return wordCount;
        }

        private unsafe void ReadNetworkData() // Retained unsafe as ReinterpretState is used with pointers
        {
            if (!_fsmLogic._stateMachinesCollected) return;
            fixed (int* statePtr = &ReinterpretState<int>())
            {
                var ptr = statePtr;

                for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
                {
                    var stateMachine = _fsmLogic.StateMachines[i];

                    stateMachine.Read(ptr);
                    ptr += stateMachine.WordCount;
                }
            }
        }

        private unsafe void WriteNetworkData()
        {
            if (!_fsmLogic._stateMachinesCollected) return;
            fixed (int* statePtr = &ReinterpretState<int>())
            {
                var ptr = statePtr;

                for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
                {
                    var stateMachine = _fsmLogic.StateMachines[i];

                    stateMachine.Write(ptr);
                    ptr += stateMachine.WordCount;
                }
            }
        }

        private bool Interpolate() // Removed unsafe keyword
        {
            if (!_fsmLogic._stateMachinesCollected) return false;

            if (!GetInterpolationData(out var interpolationData))
                return false;

            for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
            {
                var stateMachine = _fsmLogic.StateMachines[i];

                stateMachine.Interpolate(interpolationData);

                interpolationData.From += stateMachine.WordCount;
                interpolationData.To += stateMachine.WordCount;
            }

            return true;
        }

        private unsafe bool GetInterpolationData(out InterpolationData data)
        {
            var buffersValid = TryGetSnapshotsBuffers(out var fromBuffer, out var toBuffer, out var alpha);

            data = new InterpolationData
            {
                FromBuffer = fromBuffer,
                ToBuffer = toBuffer,
                Alpha = alpha
            };

            return buffersValid;
        }

        public float DeltaTime => Runner.DeltaTime; // Expose Runner's DeltaTime for use in state machines
    }
}