using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public abstract class AbstractState<T> : MonoBehaviour
{
    [HideInInspector]
    [Required]
    public T stateType;
    public float statusTimer = 0;
    [Header("State CoolDown")]
    public float StateCoolDown = 0.0f;
    public float CurrentCoolDown = 0.0f;
    private void Update()
    {
        if (CurrentCoolDown > 0)
        {
            CurrentCoolDown -= Time.deltaTime;
        }
    }
    protected MonoBehaviour _context;

    public virtual AbstractState<T> ResolveProxy()
    {
        return this;
    }

    public virtual void OnCreateMapping(MonoBehaviour context)
    {
        if (_context != null)
            Debug.LogError("State Binding Twice?");

        _context = context;
    }

    // [System.Serializable]
    // public class StateEvents
    // {
    //     public UnityEvent StateEnterEvent;
    //     public UnityEvent StateExitEvent;
    //     public UnityEvent StateFinallyEvent;
    //     public UnityEvent StateUpdateEvent;
    //     public UnityEvent StateLateUpdateEvent;
    //     public UnityEvent StateSpriteUpdateEvent;
    //     public UnityEvent StateFixedUpdateEvent;
    //     public UnityEvent<Collision> StateCollisionEnterEvent;
    // }

    // public StateEvents stateEvents;

    public virtual void OnStateEnter()
    {
        //        Debug.Log("OnStateEnter" + name, gameObject);
        statusTimer = 0;
    }



    public virtual void OnStateExit()
    {
        CurrentCoolDown = StateCoolDown;
    }



    public virtual void OnStateFinally()
    {

    }



    public virtual void OnStateUpdate()
    {
        statusTimer += Time.deltaTime;
    }



    public virtual void OnStateLateUpdate()
    {

    }



    public virtual void OnSpriteUpdate()
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
    private List<MappingEntry> mappingList = new List<MappingEntry>();
    private Dictionary<T, AbstractState<T>> mapping = new Dictionary<T, AbstractState<T>>();
    public List<MappingEntry> getAllStates => mappingList;
    public bool HasState(T state)
    {
        return mapping.ContainsKey(state);
    }

    public struct MappingEntry
    {
        public T state;
        public MonoBehaviour context;
        public AbstractState<T> stateBehavior;
    }

    public void AddStateBehaviorMapping(T state, AbstractState<T> stateBehavior, MonoBehaviour context)
    {
        stateBehavior.OnCreateMapping(context);

        MappingEntry entry = new MappingEntry();
        entry.state = state;
        entry.context = context;
        entry.stateBehavior = stateBehavior;

        mappingList.Add(entry);
        mapping.Add(state, stateBehavior);
    }

    public AbstractState<T> FindStateBehavior(T t, bool ResolveProxy = true)
    {
        if (mapping.ContainsKey(t))
        {
            return ResolveProxy ? mapping[t].ResolveProxy() : mapping[t];
        }
        else
            return null;
    }


}



