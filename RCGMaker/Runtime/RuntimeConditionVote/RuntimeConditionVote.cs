using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
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

//只有bool
//[]: 如果想要放在Scriptable上，需要FlagInit時把資料清乾淨，如果沒有reload domain會殘留
[Serializable]
public class RuntimeConditionVote : IRuntimeConditionImplementation
{
    [ShowInPlayMode] private Object[] keys => votes.Keys.ToArray();
    [ShowInPlayMode] private VoteRecord[] values => votes.Values.ToArray();

    public Dictionary<Object, VoteRecord> votes = new();
    
    [System.Serializable]
    public struct VoteRecord
    {
  
        private string _voterName;
        private bool _vote;
        
        [ShowInPlayMode]
        public string Voter => _voterName;
        
        [ShowInPlayMode]
        public bool Vote => _vote;

        public VoteRecord(Object voter,bool vote)
        {
#if UNITY_EDITOR
            _voterName = voter.name; //會有GC，UNITYEDITOR ONLY?
#else
            _voterName = string.Empty;
#endif
            _vote = vote;
        }
    }

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

    public enum ChangeResultTiming
    {
        OnVote, //一觸發就更新, 線性call
        OnManualUpdate, //類似ECS, 應該就叫做OnUpdate
        //LazySolve, 取用的時候才solve, 這個就不是event driven了
    }

    private ChangeResultTiming _changeChangeResultTiming = ChangeResultTiming.OnVote;

    public void ManualUpdate() //投票的時候還沒solve, 在update (或是真的要手動Lazy Solve)
    {
        CheckResult();
    }


    public void Vote(Object m, bool vote)
    {
        if (m is IVoteChild voteChild)
            m = voteChild.VoteOwner;

        //不需樣Add?
        votes[m] = new VoteRecord(m,vote) ;
        // Debug.Log($"Vote {m} bool:{vote}");
        
        if(_changeChangeResultTiming == ChangeResultTiming.OnVote)
            CheckResult();
        //ManualUpdate
        //投票的時候還沒solve
    }

    public void Revoke(Object m)
    {
        if (m is IVoteChild voteChild)
            m = voteChild.VoteOwner;
        if (votes.ContainsKey(m))
            votes.Remove(m);
        
        if(_changeChangeResultTiming == ChangeResultTiming.OnVote)
            CheckResult();
    }

    public async UniTask AddForSeconds(MonoBehaviour m, float seconds)
    {
        Vote(m, true);
        await UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.DeltaTime);
        Vote(m, false);
    }

    private List<Object> _toRemove = new();
    private void CheckResult()
    {
        var newResult = GetDefaultValue();

        //clear null key

        _toRemove.Clear();
        var iterator = votes.GFIterator();
        while (iterator.MoveNext())
        {
            if (iterator.Current.Key == null)
            {
                _toRemove.Add(iterator.Current.Key);
                
                Debug.LogError("null key !!????: 後面有被destroy的東西嗎？" + iterator.Current.Value.Voter);
            }
        }

        foreach (var key in _toRemove)
        {
            votes.Remove(key);
        }


        if (GetConditionType() == ConditionType.AND)
        {
            if (votes.Count != 0)
                newResult = true;
            iterator = votes.GFIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Current.Value.Vote == false)
                {
                    newResult = false;
                    break;
                }
            }

            // foreach (var vote in votes.Values)
            // {
            //     if (vote == false)
            //     {
            //         newResult = false;
            //     }
            // }  
        }
        else if (GetConditionType() == ConditionType.OR)
        {
            iterator = votes.GFIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Current.Value.Vote != true) continue;
                newResult = true;
                break;
            }


            // foreach (var vote in votes.Values)
            // {
            //     if (vote != true) continue;
            //     newResult = true;
            //     break;
            // }  
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
        OnValueChangeDelegate onValueChangeDelegate = null,ChangeResultTiming updateTiming = ChangeResultTiming.OnVote)
    {
        _getConditionTypeDelegate = ()=>type;
        _getDefaultValueDelegate = ()=>defaultValue;
        _onValueChangeDelegate = onValueChangeDelegate;
        _currentResult = GetDefaultValue();
        _changeChangeResultTiming = updateTiming;
        OnValueChange(_currentResult);

        
    }

    public RuntimeConditionVote(GetConditionTypeDelegate getConditionTypeDelegate,
        GetDefaultValueDelegate getDefaultValueDelegate, OnValueChangeDelegate onValueChangeDelegate = null,ChangeResultTiming updateTiming = ChangeResultTiming.OnVote)
    {
         _getConditionTypeDelegate = getConditionTypeDelegate;
         _getDefaultValueDelegate = getDefaultValueDelegate;
         _onValueChangeDelegate = onValueChangeDelegate;
         _changeChangeResultTiming = updateTiming;
         _currentResult = GetDefaultValue();
         OnValueChange(_currentResult);
    }



}
