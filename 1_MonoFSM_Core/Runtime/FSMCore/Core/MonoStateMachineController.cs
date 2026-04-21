using System;
using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Simulate;
using UnityEngine;
using UnityEngine.Profiling;

namespace Fusion.Addons.FSM
{
    //FIXME: 會不小心沒有轉成networked版的？
    //Local版的？
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StateMachineLogic))] // Ensure StateMachineLogic is present
    public class MonoStateMachineController : MonoBehaviour, IStateMachineController
    // IUpdateSimulate
    {
        public IReadOnlyList<IStateMachine> StateMachines => _fsmLogic.StateMachines;

        // PRIVATE MEMBERS
        //FIXME: 可以拿掉了ㄅ
        [Header("DEBUG")]
        [SerializeField]
        private bool _enableLogging; // Removed default initialization

        private StateMachineLogic _fsmLogic;

        private bool _initialized; // Removed default initialization

        // UNITY MESSAGES

        private void Awake()
        {
            _fsmLogic = GetComponent<StateMachineLogic>(); // Get the component
            _fsmLogic.EnableLogging = _enableLogging; // Sync editor value
        }

        private void Start()
        {
            Initialize();
#if FUSION2
            //不一定？
            Debug.Log("需要用networked的MonoStateMachineController嗎？ 這個只是local用的", this);
#endif
        }

        private void OnEnable()
        {
            // Ensure initialization if Start was missed (e.g. object enabled late)
            if (!_initialized)
                Initialize();
        }

        private void Update()
        {
            if (_fsmLogic._manualUpdateMode || !_initialized)
                return;

            RenderInternal();
        }

        //FIXME: 全部都從LocalSimulatorRunner打過來？
        private void FixedUpdate()
        {
            if (_fsmLogic._manualUpdateMode || !_initialized)
                return;

            FixedUpdateInternal();
        }

        private void OnDestroy()
        {
            if (_initialized)
                for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
                    _fsmLogic.StateMachines[i].Deinitialize(false); // Assuming 'false' for hasState in non-networked context
        }

        // PUBLIC METHODS
        public void Initialize()
        {
            if (_initialized)
                return;

            _fsmLogic.InitializeLogic(); // This calls CollectStateMachines

            foreach (var machine in _fsmLogic.StateMachines)
            {
                machine.Reset();
                // Initialize with null runner for non-networked context, or a mock/dummy runner if required by IStateMachine
                //FIXME:
                machine.Initialize(_fsmLogic, new LocalTickProvider()); // Assuming ITickProvider is not needed or handled internally
            }

            _initialized = true;
        }

        public void SetManualUpdate(bool manualUpdate)
        {
            _fsmLogic.SetManualUpdateMode(manualUpdate);
        }

        public void ManualFixedUpdate()
        {
            if (!_fsmLogic._manualUpdateMode)
                throw new InvalidOperationException("Manual update is not turned on");

            FixedUpdateInternal();
        }

        public void ManualRender()
        {
            if (!_fsmLogic._manualUpdateMode)
                throw new InvalidOperationException("Manual update is not turned on");

            RenderInternal();
        }

        // PRIVATE METHODS
        private void FixedUpdateInternal()
        {
            if (!_initialized)
                return;


            if (_fsmLogic.stateIdToRestore != -1)
            {
                //FIXME: 只有單一fsm, nested應該是假的沒有實作？
                _fsmLogic.StateMachines[0].ForceActivateState(_fsmLogic.stateIdToRestore, true);
                _fsmLogic.stateIdToRestore = -1; // Reset after restoring
            }
            for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
            {
                Profiler.BeginSample(
                    $"MonoStateMachineController.FixedUpdate", _fsmLogic
                );

                _fsmLogic.StateMachines[i].FixedUpdate(); // Assuming IStateMachine can handle null Runner
                Profiler.EndSample();
            }
        }

        private void RenderInternal()
        {
            if (!_initialized)
                return;
            for (var i = 0; i < _fsmLogic.StateMachines.Count; i++)
            {
                Profiler.BeginSample(
                    $"MonoStateMachineController.Render",
                    _fsmLogic);
                _fsmLogic.StateMachines[i].Render();
                Profiler.EndSample();
            }
        }

        public float DeltaTime => Time.deltaTime * WorldUpdateSimulator.TimeScale; //FIXME: 要從world那邊同步？

        // public void Simulate(float deltaTime)
        // {
        // }
        //
        // public void AfterUpdate()
        // {
        //     throw new NotImplementedException();
        // }
    }
}
