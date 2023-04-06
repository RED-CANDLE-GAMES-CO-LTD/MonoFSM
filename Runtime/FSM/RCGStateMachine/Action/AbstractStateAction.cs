using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

public abstract class AbstractStateAction : AbstractBehaviour
{
    //怎麼知道誰用Enter, 誰用Update
    protected bool IsValid //AND
    {
        get
        {
            return conditions.IsAllValid();
        }
    }

    public GeneralState bindingState => this.GetComponentInParent<GeneralState>(true);

    // #if UNITY_EDITOR
    [PropertyOrder(1)]
    [TabGroup("Condition", false, 1)]
    [Component(typeof(AbstractConditionComp), AddComponentAt.Children, "[Condition]")]
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

    //一定是AND的啦
    public async void OnActionEnter()
    {
        //TODO: conditions
        if (!IsValid) return; //not valid也要用字串？

        if (delayActionModifier != null)
            await UniTask.Delay(TimeSpan.FromSeconds(delayActionModifier.delayTime));

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
}