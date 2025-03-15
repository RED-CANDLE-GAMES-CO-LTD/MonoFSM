

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using RCGMaker.Core.Attributes;
using UnityEngine;
using UnityEngine.Profiling;
using Object = System.Object;

namespace RCGMaker.Core
{
    public class StateMachineRunner : MonoBehaviour
    {
        public bool showCurrentState = false;
        public object currentState => stateMachineList[0].CurrentStateMap.state;
        private List<IStateMachine> stateMachineList = new List<IStateMachine>();
        
        /// <summary>
        /// Creates a stateMachine token object which is used to managed to the state of a monobehaviour. 
        /// </summary>
        /// <typeparam name="T">An Enum listing different state transitions</typeparam>
        /// <param name="component">The component whose state will be managed</param>
        /// <returns></returns>
        public StateMachine<T> Initialize<T>(MonoBehaviour component, StateMapping<T> stateMapping = null)
            where T : class
        {
            var fsm = new StateMachine<T>(this, component, stateMapping);

            stateMachineList.Add(fsm);
         
            return fsm;
        }

        private void OnEnable()
        {
            StateMachineManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (StateMachineManager.IsAvailable())
                StateMachineManager.Instance.Unregister(this);
            
        }

        private void OnDestroy()
        {
            foreach (var stateMachine in stateMachineList)
            {
                // stateMachine.CurrentStateMap
                //FIXME: 要清掉pure class memory leak?
                stateMachine.ClearReferences();
            }
        }

        /// <summary>
        /// Creates a stateMachine token object which is used to managed to the state of a monobehaviour. Will automatically transition the startState
        /// </summary>
        /// <typeparam name="T">An Enum listing different state transitions</typeparam>
        /// <param name="component">The component whose state will be managed</param>
        /// <param name="startState">The default start state</param>
        /// <returns></returns>
        public StateMachine<T> Initialize<T>(MonoBehaviour component, T startState, StateMapping<T> stateMapping = null)
            where T : class
        {
            var fsm = Initialize<T>(component, stateMapping);

            fsm.ChangeState(startState);
            return fsm;
        }

        //應該沒人在用吧
        // void FixedUpdate()
        // {
        //     for (int i = 0; i < stateMachineList.Count; i++)
        //     {
        //         var fsm = stateMachineList[i];
        //         if (!fsm.IsInTransition && fsm.Component.enabled) fsm.CurrentStateMap.FixedUpdate();
        //     }
        // }

        public void UpdateFromManager()
        {
            Profiler.BeginSample("StateMachineRunner.UpdateFromManager", this);
            
            for (var i = stateMachineList.Count - 1; i >= 0; i--)
            {
                var fsm = stateMachineList[i];
                if (fsm.isPaused) continue;
                fsm.SetLastActiveTime(Time.time);
                //暫停不跑
                
                if (!fsm.IsInTransition && fsm.Component.enabled)
                {
                    fsm.CurrentStateMap.Update();

#if UNITY_EDITOR
                    // if (showCurrentState)
                    //     currentState = fsm.CurrentStateMap.state.ToString();
#endif
                }


            }

            Profiler.EndSample();
        }

        public void LateUpdateFromManager()
        {
            foreach (var fsm in stateMachineList)
            {
                if (fsm.isPaused) continue;
                if (!fsm.IsInTransition && fsm.Component.enabled)
                {
                    fsm.CurrentStateMap.SpriteUpdate();
                    // fsm.CurrentStateMap.LateUpdate();
                }
            }

            if (owner == null || owner.VariableFolder == null)
            {
                Debug.LogError("No owner found",this);
            }
            //late update 之後才能更新
            owner.VariableFolder.CommitVariableValues();
        }
        
        [PreviewInInspector]
        [AutoParent] StateMachineOwner owner;

        //void OnCollisionEnter(Collision collision)
        //{
        //	if(currentState != null && !IsInTransition)
        //	{
        //		currentState.OnCollisionEnter(collision);
        //	}
        //}

        public static void DoNothing()
        {
        }

        public static void DoNothingCollider(Collider other)
        {
        }

        public static void DoNothingCollision(Collision other)
        {
        }

        public static IEnumerator DoNothingCoroutine()
        {
            yield break;
        }
    }


    public class StateMapping
    {
        public object state;

        public bool hasEnterRoutine;
        public Action EnterCall = StateMachineRunner.DoNothing;
        public Func<IEnumerator> EnterRoutine = StateMachineRunner.DoNothingCoroutine;

        public bool hasExitRoutine;
        public Action ExitCall = StateMachineRunner.DoNothing;
        public Func<IEnumerator> ExitRoutine = StateMachineRunner.DoNothingCoroutine;
        public Action Finally = StateMachineRunner.DoNothing;
        public Action Update = StateMachineRunner.DoNothing;
        public Action SpriteUpdate = StateMachineRunner.DoNothing;
        public Action LateUpdate = StateMachineRunner.DoNothing;
        public Action FixedUpdate = StateMachineRunner.DoNothing;
        public Action<Collision> OnCollisionEnter = StateMachineRunner.DoNothingCollision;

        public StateMapping(object state)
        {
            this.state = state;
        }

        public void ClearReferences()
        {
            state = null;
            EnterCall = null;
            EnterRoutine = null;
            ExitCall = null;
            ExitRoutine = null;
            Finally = null;
            Update = null;
            SpriteUpdate = null;
            LateUpdate = null;
            FixedUpdate = null;
            OnCollisionEnter = null;
        }

    }
}


