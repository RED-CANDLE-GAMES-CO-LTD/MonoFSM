using System;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Deprecated
{
    [Obsolete]
    public abstract class AbstractState<T> : MonoBehaviour
    {
        [Required] public T stateType;
        public float statusTimer = 0;
        [PreviewInInspector] private int _currentFrameCount = 0;
        public int CurrentFrameCount => _currentFrameCount;
        [Header("State CoolDown")] public float StateCoolDown = 0.0f;
        // public float CurrentCoolDown = 0.0f;

        // private void Update()
        // {
        //     if (CurrentCoolDown > 0)
        //     {
        //         CurrentCoolDown -= Time.deltaTime;
        //     }
        // }

        protected MonoBehaviour _context;

        public virtual AbstractState<T> ResolveProxy()
        {
            return this;
        }

        public virtual void OnCreateMapping(MonoBehaviour context)
        {
            if (_context != null)
                Debug.LogError("State Binding Twice?", this);

            _context = context;
        }

        private void OnEnable()
        {
            statusTimer = 0;
        }

        //如果要mapping 3個 節點 OnStateEnter, OnStateExit, OnStateUpdate 都要寫特殊class?

        public virtual void OnStateEnter()
        {
            //        Debug.Log("OnStateEnter" + name, gameObject);
            statusTimer = 0;
            _currentFrameCount = 0;
        }

        public virtual void OnEnterStateRender()
        {
            //fixme: render timer?
        }

        public virtual void OnStateExit()
        {
            // CurrentCoolDown = StateCoolDown;
            statusTimer = -statusTimer;
        }


        public virtual void OnStateFinally()
        {
        }


        public virtual void OnStateUpdate()
        {
            statusTimer += Time.deltaTime;
            _currentFrameCount++;
        }

        /// <summary>
        /// FIXME: 怎麼處理這個？network用
        /// </summary>
        /// <param name="deltaTime"></param>
        public virtual void OnStateSimulate(float deltaTime)
        {
            statusTimer += deltaTime;
            // _currentFrameCount++;
        }

        // public virtual void OnStateLateUpdate()
        // {
        //
        // }


        public virtual void OnRenderUpdate()
        {
        }


        public virtual void OnStateFixedUpdate()
        {
        }


        public virtual void OnStateCollisionEnter(Collision c)
        {
        }
    }


    public class StateMapping<T>
    {
        private Dictionary<T, AbstractState<T>> mapping = new();
        private List<MappingEntry> mappingList = new();
        public List<MappingEntry> getAllStates => mappingList;

        public bool HasState(T state)
        {
            return mapping.ContainsKey(state);
        }

        public struct MappingEntry
        {
            public T state;

            // public MonoBehaviour context;
            public AbstractState<T> stateBehavior;
        }

        public void AddStateBehaviorMapping(T state, AbstractState<T> stateBehavior, MonoBehaviour context)
        {
            stateBehavior.OnCreateMapping(context);

            var entry = new MappingEntry
            {
                state = state,
                // entry.context = context;
                stateBehavior = stateBehavior
            };

            mappingList.Add(entry);
            mapping.Add(state, stateBehavior);
        }

        public AbstractState<T> FindStateBehavior(T t, bool ResolveProxy = true)
        {
            if (mapping.ContainsKey(t))
                return ResolveProxy ? mapping[t].ResolveProxy() : mapping[t];
            else
                return null;
        }
    }
}