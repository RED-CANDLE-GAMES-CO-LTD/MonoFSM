
#pragma warning disable 0414

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

namespace MonoFSM.Core.Deprecated
{
    public enum StateTransition
    {
        Safe,
        Overwrite,
    }

    public interface IStateMachine
    {
        // MonoBehaviour Component { get; } //蛤？這不就強迫綁定了...
        bool IsEnabled { get; }
        
        StateMapping CurrentStateMap { get; }
        bool IsInTransition { get; }
        bool isPaused { get; }
        void ClearReferences();
        void SetLastActiveTime(float time);
        float LastActiveTime { get; }
    }


    /// <summary>
    /// 這個已經確定是monobehaviour的狀態機，會有一個runner來管理
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete]
    public class StateMachine<T> : IStateMachine where T : class //這個弄死了
    {
        public event Action<T> Changed;
        public bool isPaused => _isPaused;
        public float LastActiveTime { get; private set; }
        public void SetLastActiveTime(float time)
        {
            LastActiveTime = time;
        }
        public void ClearReferences()
        {
            //這個最重要, state裡面有事件，要清掉
            var states = stateLookup.Values;
            foreach (var state in states)
            {
                state.ClearReferences();
            }

            component = null;
            engine = null;
            lastState = null;
            currentState = null;
            destinationState = null;
            // Changed = null;
        }

        bool _isPaused = false;
        public void Pause()
        {
            _isPaused = true;
        }
        public void Resume()
        {
            _isPaused = false;
        }
        private StateMachineRunner engine;
        private MonoBehaviour component;


        #region Netcode minimal state machine variables

        private int _activeStateId;
        private int _previousStateId;
        private int _defaultStateId;
        private int _stateChangeTick;
        private int _bitState;

        #endregion
       
        
        private StateMapping lastState;
        private StateMapping currentState;
        private StateMapping destinationState;

        public Dictionary<object, StateMapping> stateLookup;
        // public StateMapping ActiveState      => _activeStateId >= 0 ? _states[_activeStateId] : null;

        private readonly string[] ignoredNames = new[] { "add", "remove", "get", "set" };

        private bool isInTransition = false;
        private IEnumerator currentTransition;
        private IEnumerator exitRoutine;
        private IEnumerator enterRoutine;
        private IEnumerator queuedChange;

        public bool HasState(T state)
        {
            return _stateMapping.HasState(state);
        }

        public AbstractState<T> FindMappingState(T state)
        {
            return _stateMapping.FindStateBehavior(state);
        }


        public StateMapping<T> _stateMapping;

        public StateMachine(StateMachineRunner engine, MonoBehaviour context, StateMapping<T> stateMapping = null)
        {
            this.engine = engine;
            component = context;
            _stateMapping = stateMapping;
            var values = stateMapping.getAllStates.ConvertAll((entry) => entry.state);

            //Define States
            // var values = Enum.GetValues(typeof(T));
            // if (values.Length < 1) { throw new ArgumentException("Enum provided to Initialize must have at least 1 visible definition"); }

            stateLookup = new Dictionary<object, StateMapping>();
            foreach (var value in values)
            {
                var mapping = new StateMapping(value);
                stateLookup.Add(mapping.state, mapping);
            }

            if (stateMapping != null)
            {
                for (int i = 0; i < values.Count; i++)
                {

                    AbstractState<T> stateBehavior = stateMapping.FindStateBehavior(values[i], false);

                    if (stateBehavior != null)
                    {
                        var targetState = stateLookup[values[i]];

                        targetState.hasEnterRoutine = false;
                        targetState.EnterCall = stateBehavior.ResolveProxy().OnStateEnter;
                        targetState.EnterRenderCall = stateBehavior.ResolveProxy().OnEnterStateRender; 
                        targetState.hasExitRoutine = false;

                        targetState.ExitCall = () =>
                        {
                            stateBehavior.ResolveProxy().OnStateExit();

                            // if (stateBehavior.ResolveProxy().stateEvents.StateExitEvent != null)
                            // {
                            //     stateBehavior.ResolveProxy().stateEvents.StateExitEvent.Invoke();
                            //     // stateBehavior.ResolveProxy().stateEvents.StateExitEvent.RemoveAllListeners();
                            // }

                        };

                        targetState.Finally = () =>
                        {
                            stateBehavior.ResolveProxy().OnStateFinally();

                            // if (stateBehavior.ResolveProxy().stateEvents.StateFinallyEvent != null)
                            //     stateBehavior.ResolveProxy().stateEvents.StateFinallyEvent.Invoke();
                        };


                        targetState.Update = stateBehavior.ResolveProxy().OnStateUpdate;
                        targetState.Simulate = stateBehavior.ResolveProxy().OnStateSimulate;
                        targetState.RenderUpdate = () => stateBehavior.ResolveProxy().OnRenderUpdate();

                        targetState.LateUpdate = () =>
                        {
                            // stateBehavior.ResolveProxy().OnStateLateUpdate();

                            // if (stateBehavior.ResolveProxy().stateEvents.StateLateUpdateEvent != null)
                            //     stateBehavior.ResolveProxy().stateEvents.StateLateUpdateEvent.Invoke();



                            // if (stateBehavior.ResolveProxy().stateEvents.StateSpriteUpdateEvent != null)
                            //     stateBehavior.ResolveProxy().stateEvents.StateSpriteUpdateEvent.Invoke();

                        };


                        targetState.FixedUpdate = () =>
                        {

                            stateBehavior.ResolveProxy().OnStateFixedUpdate();

                            // if (stateBehavior.ResolveProxy().stateEvents.StateFixedUpdateEvent != null)
                            //     stateBehavior.ResolveProxy().stateEvents.StateFixedUpdateEvent.Invoke();

                        };


                        targetState.OnCollisionEnter = (c) =>
                        {
                            stateBehavior.ResolveProxy().OnStateCollisionEnter(c);

                            // if (stateBehavior.ResolveProxy().stateEvents.StateCollisionEnterEvent != null)
                            //     stateBehavior.ResolveProxy().stateEvents.StateCollisionEnterEvent.Invoke(c);

                        };
                       

                    }

                }
            }

            //Create nil state mapping
            currentState = new StateMapping(null);
        }

        private V CreateDelegate<V>(MethodInfo method, Object target) where V : class
        {
            var ret = (Delegate.CreateDelegate(typeof(V), target, method) as V);

            if (ret == null)
            {
                throw new ArgumentException("Unabled to create delegate for method called " + method.Name);
            }
            return ret;

        }

 
        public void ChangeState(T newState, bool forceSameState = false)
        {
            // if(engine!=null)
            //      Debug.Log(engine.gameObject.name+" Change State:"+ newState);

            ChangeState(newState, StateTransition.Safe, forceSameState);
        }

        //FIXME: //如何避免遞迴 changeState 過去又回來，condition都符合就一直跳過去又跳回來
        //bool ?
        public void ChangeState(T newState, StateTransition transition, bool forceSameState = false)
        {
            // Debug.Log("Change to state" + newState);
            if (stateLookup == null)
            {
                throw new Exception("States have not been configured, please call initialized before trying to set state");
            }

            if (!stateLookup.ContainsKey(newState))
            {
                Debug.LogError(
                    "No state with the name " + newState.ToString() +
                    " can be found. Please make sure you are called the correct type the statemachine was initialized with",
                    newState as MonoBehaviour);
                throw new Exception("No state with the name " + newState.ToString() + " can be found. Please make sure you are called the correct type the statemachine was initialized with");
                
            }

            var nextState = stateLookup[newState];
          

            if (currentState == nextState)
            {
                if (forceSameState == false)
                    return;
            }

            //Cancel any queued changes.
            if (queuedChange != null)
            {
                engine.StopCoroutine(queuedChange);
                queuedChange = null;
            }

            switch (transition)
            {
                //case StateMachineTransition.Blend:
                //Do nothing - allows the state transitions to overlap each other. This is a dumb idea, as previous state might trigger new changes. 
                //A better way would be to start the two couroutines at the same time. IE don't wait for exit before starting start.
                //How does this work in terms of overwrite?
                //Is there a way to make this safe, I don't think so? 
                //break;
                case StateTransition.Safe:
                    if (isInTransition)
                    {
                        // Debug.Log("[StateMachine] isInTransition, WaitForPreviousTransition?" + currentState);
                        if (exitRoutine != null) //We are already exiting current state on our way to our previous target state
                        {
                            //Overwrite with our new target
                            destinationState = nextState;
                            return;
                        }

                        if (enterRoutine != null) //We are already entering our previous target state. Need to wait for that to finish and call the exit routine.
                        {
                            //Damn, I need to test this hard
                            queuedChange = WaitForPreviousTransition(nextState);
                            engine.StartCoroutine(queuedChange);
                            return;
                        }
                    }
                    break;
                case StateTransition.Overwrite:
                    if (currentTransition != null)
                    {
                        engine.StopCoroutine(currentTransition);
                    }
                    if (exitRoutine != null)
                    {
                        engine.StopCoroutine(exitRoutine);
                    }
                    if (enterRoutine != null)
                    {
                        engine.StopCoroutine(enterRoutine);
                    }

                    //Note: if we are currently in an EnterRoutine and Exit is also a routine, this will be skipped in ChangeToNewStateRoutine()
                    break;
            }


            if ((currentState != null && currentState.hasExitRoutine) || nextState.hasEnterRoutine)
            {
                isInTransition = true;
                currentTransition = ChangeToNewStateRoutine(nextState, transition);
                engine.StartCoroutine(currentTransition);
            }
            else //Same frame transition, no coroutines are present
            {
                // Debug.Log("[StateMachine] Same Frame Transition" + currentState.state + ",to:" + nextState.state);
                lastState = currentState;
                currentState = nextState;

                //OnStateExit時，currentState就已經換了
                if (lastState != null)
                {
                    lastState.ExitCall();
                    lastState.Finally();
                }
                if (currentState != null)
                {
                    //先call changedEvent再callEnter
                    Changed?.Invoke((T)currentState.state);     
                    currentState.EnterCall();
                }
                isInTransition = false;
            }
        }

        private IEnumerator ChangeToNewStateRoutine(StateMapping newState, StateTransition transition)
        {
            destinationState = newState; //Cache this so that we can overwrite it and hijack a transition

            if (currentState != null)
            {
                if (currentState.hasExitRoutine)
                {
                    exitRoutine = currentState.ExitRoutine();

                    if (exitRoutine != null && transition != StateTransition.Overwrite) //Don't wait for exit if we are overwriting
                    {
                        yield return engine.StartCoroutine(exitRoutine);
                    }

                    exitRoutine = null;
                }
                else
                {
                    currentState.ExitCall();
                }

                currentState.Finally();
            }

            lastState = currentState;
            currentState = destinationState;

            if (currentState != null)
            {
                if (currentState.hasEnterRoutine)
                {
                    enterRoutine = currentState.EnterRoutine();

                    if (enterRoutine != null)
                    {
                        // Add Yield Return here to wait for the routine to end before updating
                        engine.StartCoroutine(enterRoutine);
                    }

                    enterRoutine = null;
                }
                else
                {
                    currentState.EnterCall();
                }

                //Broadcast change only after enter transition has begun. 
                if (Changed != null)
                {
                    Changed((T)currentState.state);
                }
            }

            isInTransition = false;
        }

        IEnumerator WaitForPreviousTransition(StateMapping nextState)
        {
            while (isInTransition)
            {
                yield return null;
            }

            ChangeState((T)nextState.state);
        }

        
        public T LastState
        {
            get
            {
                if (lastState == null) return default(T);

                if (lastState.state == null) return default(T);

                return (T)lastState.state;
            }
        }

        public T State
        {
            get { return (T)currentState.state; }
        }

        public bool IsInTransition => isInTransition;


        public bool IsEnabled => component.enabled;

        public StateMapping CurrentStateMap => currentState;

        public MonoBehaviour Component => component;

        //Static Methods

        /// <summary>
        /// Inspects a MonoBehaviour for state methods as definied by the supplied Enum, and returns a stateMachine instance used to trasition states.
        /// </summary>
        /// <param name="component">The component with defined state methods</param>
        /// <returns>A valid stateMachine instance to manage MonoBehaviour state transitions</returns>
        public static StateMachine<T> Initialize(MonoBehaviour component, StateMapping<T> stateMapping = null)
        {
            var engine = component.GetComponent<StateMachineRunner>();
            //FIXME: 理想不要runtime add component
            if (engine == null) engine = component.gameObject.AddComponent<StateMachineRunner>();

            var machine = engine.Initialize<T>(component, stateMapping);
            machine.runner = engine;
            return machine;
        }
        public StateMachineRunner runner;
        /// <summary>
        /// Inspects a MonoBehaviour for state methods as definied by the supplied Enum, and returns a stateMachine instance used to trasition states. 
        /// </summary>
        /// <param name="component">The component with defined state methods</param>
        /// <param name="startState">The default starting state</param>
        /// <returns>A valid stateMachine instance to manage MonoBehaviour state transitions</returns>
        // public static StateMachine<T> Initialize(MonoBehaviour component, T startState, StateMapping<T> stateMapping = null)
        // {
        //     var engine = component.GetComponent<StateMachineRunner>();
        //     if (engine == null) engine = component.gameObject.AddComponent<StateMachineRunner>();
        //
        //     return engine.Initialize<T>(component, startState, stateMapping);
        // }

    }

}
