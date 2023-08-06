using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;


public interface IRuntimeConditionImplementation //這個interface的目的是？
{
    ConditionType GetConditionType();
    void OnValueChange(bool value);
    void Vote(Object m, bool vote);
    bool GetDefaultValue();
}


public enum ConditionType
{
    AND,
    OR
}

public interface IVoteChild
{
    public MonoBehaviour VoteOwner { get; }
}

//[]: 如果想要放在Scriptable上，需要FlagInit時把資料清乾淨，如果沒有reload domain會殘留
[Serializable]
public class RuntimeConditionVote : IRuntimeConditionImplementation
{
    [ShowInPlayMode] private Object[] keys => votes.Keys.ToArray();
    [ShowInPlayMode] private bool[] values => votes.Values.ToArray();

    public Dictionary<Object, bool> votes = new();

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
        _onValueChangeDelegate?.Invoke(value);
    }

    private GetDefaultValueDelegate _getDefaultValueDelegate;
    private OnValueChangeDelegate _onValueChangeDelegate;
    private GetConditionTypeDelegate _getConditionTypeDelegate;
    
    public delegate bool GetDefaultValueDelegate();
    public delegate void OnValueChangeDelegate(bool value);
    public delegate ConditionType GetConditionTypeDelegate ();



    public void Vote(Object m, bool vote)
    {
        if (m is IVoteChild voteChild)
            m = voteChild.VoteOwner;

        //不需樣Add?
        votes[m] = vote;
        // Debug.Log($"Vote {m} bool:{vote}");
        CheckResult();
    }

    public void Revoke(Object m)
    {
        if (m is IVoteChild voteChild)
            m = voteChild.VoteOwner;
        if (votes.ContainsKey(m))
            votes.Remove(m);
        CheckResult();
    }

    public async UniTask AddForSeconds(MonoBehaviour m, float seconds)
    {
        Vote(m, true);
        await UniTask.Delay(TimeSpan.FromSeconds(seconds));
        Vote(m, false);
    }

    private void CheckResult()
    {
        var newResult = GetDefaultValue();

        //clear null key
        foreach (var key in votes.Keys.ToArray())
            if (key == null)
            {
                votes.Remove(key);
                Debug.LogError("null key !!????: 後面有被destroy的東西嗎？" + key);
            }


        if (GetConditionType() == ConditionType.AND)
        {
            if (votes.Values.Count != 0)
                newResult = true;
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
                if (vote != true) continue;
                newResult = true;
                break;
            }  
        }

        if (_currentResult != newResult)
        {
            _currentResult = newResult;
            OnValueChange(newResult);
        }

    }

    private bool _currentResult = false;

    // public bool VoteResult => _currentResult;
    public bool Result => _currentResult;

    public RuntimeConditionVote(ConditionType type = ConditionType.OR, bool defaultValue = false,
        OnValueChangeDelegate onValueChangeDelegate = null)
    {
        _getConditionTypeDelegate = ()=>type;
        _getDefaultValueDelegate = ()=>defaultValue;
        _onValueChangeDelegate = onValueChangeDelegate;
        _currentResult = GetDefaultValue();
        OnValueChange(_currentResult);

      
    }

    public RuntimeConditionVote(GetConditionTypeDelegate getConditionTypeDelegate,
        GetDefaultValueDelegate getDefaultValueDelegate, OnValueChangeDelegate onValueChangeDelegate = null)
    {
         _getConditionTypeDelegate = getConditionTypeDelegate;
         _getDefaultValueDelegate = getDefaultValueDelegate;
         _onValueChangeDelegate = onValueChangeDelegate;
         _currentResult = GetDefaultValue();
         OnValueChange(_currentResult);
    }



}
