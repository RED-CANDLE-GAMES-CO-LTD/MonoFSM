using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class AbstractStateAction : AbstractBehaviour, IVoteChild
{
    //怎麼知道誰用Enter, 誰用Update
    private bool IsValid //AND
    {
        get
        {
            if (_delay) return false;
            if (conditions.Length == 0)
                return true;
            return conditions.IsAllValid();
        }
    }

    [AutoParent()] protected GeneralState bindingState; // => this.GetComponentInParent<GeneralState>(true);

    // #if UNITY_EDITOR
    [PropertyOrder(1)]
    [TabGroup("Condition", false, 1)]
    [Component(typeof(AbstractConditionComp), AddComponentAt.Children, "[Condition]")]
    [PreviewInInspector]
    // #endif
    [AutoChildren(false, DepthOneOnly = true)] public AbstractConditionComp[] conditions;//condition 成立，才能做事

    // private bool conditionFeteched = false;

    // private void CheckFetchCondition()
    // {
    //     if (conditionFeteched)
    //         return;

    //     conditionFeteched = true;

    //     if (conditions == null || conditions.Count == 0)
    //     {
    //         conditions.AddRange(this.GetComponents<AbstractCondition>());
    //     }
    // }

    [AutoParent]
    DelayActionModifier delayActionModifier;

    private bool _delay = false;

    //一定是AND的啦
    public async void OnActionEnter()
    {
        if (_delay)
            UnityEngine.Debug.LogError("Delay 還沒結束又DELAY 死罪", this);

        // _delay = false;
        //TODO: conditions
        if (!IsValid) return; //not valid也要用字串？

        _delay = true;
        if (delayActionModifier != null)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delayActionModifier.delayTime), DelayType.DeltaTime,
                    PlayerLoopTiming.Update, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                _delay = false;
                // Debug.LogError("Delay Cancelled" + e, this);
                return;
            }
        }

        _delay = false;
        // this.AddTask(OnStateEnterImplement, delayActionModifier.delayTime);
        OnStateEnterImplement();
    }
    protected abstract void OnStateEnterImplement();
    public void OnActionUpdate()
    {
        if (IsValid)
            OnStateUpdateImplement();
    }
    protected virtual void OnStateUpdateImplement() { }
    public void OnActionSpriteUpdate()
    {
        if (IsValid)
            OnSpriteUpdateImplement();
    }
    protected virtual void OnSpriteUpdateImplement() { }

    public async void OnActionExit()
    {
        if (!IsValid) return;
        if (delayActionModifier != null) await UniTask.Delay(TimeSpan.FromSeconds(delayActionModifier.delayTime));
        OnStateExitImplement();
    }
    protected virtual void OnStateExitImplement() { }
    public MonoBehaviour VoteOwner => bindingState.Context.fsmOwner;

    protected CancellationTokenSource cancellationTokenSource => bindingState.GetStateExitCancellationTokenSource();
}