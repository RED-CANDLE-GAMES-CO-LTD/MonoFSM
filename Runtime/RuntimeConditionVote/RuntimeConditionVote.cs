using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public interface IRuntimeConditionImplementation
{
    ConditionType GetConditionType();
    void OnValueChange(bool value);
    void Vote(MonoBehaviour m, bool vote);
    bool GetDefaultValue();
}


public enum ConditionType
{
    AND,
    OR
}

public  class RuntimeConditionVote :IRuntimeConditionImplementation
{
    public Dictionary<MonoBehaviour, bool> votes = new Dictionary<MonoBehaviour, bool>();

    public ConditionType GetConditionType()
    {
        return _getConditionTypeDelegate();
    }

    public bool GetDefaultValue()
    {
        return _getDefaultValueDelegate.Invoke();
    }

    public void OnValueChange(bool value)
    {
        _onValueChangeDelegate(value);
    }

    private GetDefaultValueDelegate _getDefaultValueDelegate;
    private OnValueChangeDelegate _onValueChangeDelegate;
    private GetConditionTypeDelegate _getConditionTypeDelegate;
    
    public delegate bool GetDefaultValueDelegate();
    public delegate void OnValueChangeDelegate(bool value);
    public delegate ConditionType GetConditionTypeDelegate ();
    
    public void Vote(MonoBehaviour m, bool vote)
    {
        if (votes.ContainsKey(m))
            votes[m] = vote;
        else
        {
            votes.Add(m,vote);
        }

        CheckResult();
    }

    private void CheckResult()
    {
        bool newResult = GetDefaultValue();

        if (GetConditionType() == ConditionType.AND)
        {
            foreach (var vote in votes.Values)
            {
                if (vote == false)
                {
                    newResult = false;
                }
            }  
        }
        else if (GetConditionType() == ConditionType.OR)
        {
            foreach (var vote in votes.Values)
            {
                if (vote == true)
                {
                    newResult = true;
                }
            }  
        }

        if (_lastResult != newResult)
        {
            _lastResult = newResult;
            OnValueChange(newResult);
        }

    }

    private bool _lastResult = false;


    public bool VoteResult => _lastResult;
    public RuntimeConditionVote(ConditionType type ,bool defaultValue,OnValueChangeDelegate onValueChangeDelegate)
    {
        _getConditionTypeDelegate = ()=>type;
        _getDefaultValueDelegate = ()=>defaultValue;
        _onValueChangeDelegate = onValueChangeDelegate;
        _lastResult = GetDefaultValue();
        OnValueChange(_lastResult);
    }

    public RuntimeConditionVote(GetConditionTypeDelegate getConditionTypeDelegate ,GetDefaultValueDelegate getDefaultValueDelegate,OnValueChangeDelegate onValueChangeDelegate)
    {
         _getConditionTypeDelegate = getConditionTypeDelegate;
         _getDefaultValueDelegate = getDefaultValueDelegate;
         _onValueChangeDelegate = onValueChangeDelegate;
         _lastResult = GetDefaultValue();
        OnValueChange(_lastResult);
    }

    public bool Result => _lastResult;

}
