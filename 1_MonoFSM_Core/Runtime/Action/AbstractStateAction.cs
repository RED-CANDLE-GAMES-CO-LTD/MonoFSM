using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MonoFSM.Foundation;
using RCGExtension;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.Vote;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM_Core.Runtime.Action
{
    //IEventInvoker?
    public interface IActionParent //給GameObject結構Validate用的
    {
    }

    /// <summary>
    /// Represents an abstract base class for defining actions that are executed within a state
    /// in the finite state machine (FSM) framework. Inherit from this class to implement
    /// custom state actions.
    /// </summary>
    ///FIXME: 不該叫StateAction了，Action? SideEffect?
    //多餘？IStateEnter, IStateUpdate?
    [Searchable]
    public abstract class AbstractStateAction : AbstractDescriptionBehaviour, IVoteChild, IGuidEntity,
        IDefaultSerializable,
        IEventReceiver
    {
        protected override bool HasError()
        {
            return GetComponentInParent<IActionParent>() == null;
        }

        protected override string DescriptionTag => "Action";

        //怎麼知道誰用Enter, 誰用Update
        private bool IsValid //AND
        {
            get
            {
                if (_delay) return false;
                return isActiveAndEnabled && _conditions.IsAllValid();
            }
        }


        // [PreviewInInspector]
        [AutoParent] protected GeneralState bindingState; // => this.GetComponentInParent<GeneralState>(true)// ;

        [Required]
        [PreviewInInspector]
        [AutoParent]
        protected IActionParent _actionParent;


        [HideInInlineEditors]
        // #if UNITY_EDITOR
        [HideFromFSMExport]
        [PropertyOrder(1)]
        [TabGroup("Condition", false, 1)]
        [Component(AddComponentAt.Children, "[Condition]")]
        [PreviewInInspector]
        // #endif
        [AutoChildren(false, DepthOneOnly = true)]
        protected AbstractConditionComp[] _conditions; //condition 成立，才能做事

#if UNITY_EDITOR
        [PreviewInInspector] private bool IsAllValid => _conditions.IsAllValid();
#endif

        protected virtual string renamePostfix => "";

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

        [AutoParent] private DelayActionModifier delayActionModifier;

        private bool _delay = false;

        //一定是AND的啦
        public async void OnActionEnter()
        {
            if (!isActiveAndEnabled) return;
            if (_delay)
                Debug.LogError("Delay 還沒結束又DELAY 死罪", this);

            // _delay = false;
            //TODO: conditions
            if (!IsValid) return; //not valid也要用字串？

            _delay = true;
            if (delayActionModifier != null)
                try
                {
                    //FIXME: 這個delay用unitask不好，時間軸和fsm錯開了
                    await UniTask.Delay(TimeSpan.FromSeconds(delayActionModifier.delayTime), DelayType.DeltaTime,
                        PlayerLoopTiming.Update, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    _delay = false;
                    // Debug.LogError("Delay Cancelled" + e, this);
                    return;
                }

            _delay = false;
            // this.AddTask(OnStateEnterImplement, delayActionModifier.delayTime);
            OnStateEnterImplement();
        }

        protected abstract void OnStateEnterImplement(); //FIXME: 沒參數的?

        public void OnActionUpdate()
        {
            if (IsValid)
                OnStateUpdateImplement();
        }

        protected virtual void OnStateUpdateImplement()
        {
        }

        public void OnActionSpriteUpdate()
        {
            if (IsValid)
                OnSpriteUpdateImplement();
        }

        protected virtual void OnSpriteUpdateImplement()
        {
        }

        public async void OnActionExit()
        {
            if (!IsValid) return;
            if (delayActionModifier != null) await UniTask.Delay(TimeSpan.FromSeconds(delayActionModifier.delayTime));
            OnStateExitImplement();
        }

        protected virtual void OnStateExitImplement()
        {
        }

        public virtual MonoBehaviour VoteOwner => nearestBinder as MonoBehaviour;
        [AutoParent] private IBinder nearestBinder;

        protected CancellationTokenSource cancellationTokenSource => bindingState.GetStateExitCancellationTokenSource();

        public virtual void SetPlaybackTime(float time)
        {
        }

        public virtual void Pause()
        {
        }

        public virtual void Resume()
        {
        }

        public virtual void EventReceived<T>(T arg)
        {
            //FIXME: 這個會無窮迴圈..
            // if (this is IRCGArgEventReceiver<T> receiver)
            // {
            //     Debug.Log("AbstractStateAction.EventReceived"+receiver, this);
            //     Debug.Log("AbstractStateAction.EventReceived arg"+arg, this);
            //     receiver.EventReceived(arg);
            // }
            // else
            OnStateEnterImplement();
        }

        

        public void EventReceived()
        {
            OnStateEnterImplement();
        }

        public virtual void SimulationUpdate(float passedDuration)
        {
        }

        public virtual void EventReceived(IEffectHitData arg)
        {
            EventReceived<IEffectHitData>(arg);
        }
    }
}