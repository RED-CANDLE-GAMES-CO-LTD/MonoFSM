using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MonoFSM.Foundation;
using RCGExtension;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Runtime.Vote;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    //IEventInvoker?
    public interface IActionParent //給GameObject結構Validate用的
    {
    }

    public abstract class AbstractStateAction<T> : AbstractStateAction, IArgEventReceiver<T>
    // where T : IEffectHitData
    {
        void IArgEventReceiver<T>.ArgEventReceived(T arg)
        {
            _lastEventReceivedTime = Time.time;
            OnArgEventReceived(arg);
        }

        protected abstract void OnArgEventReceived(T arg);
    }

    /// <summary>
    /// Represents an abstract base class for defining actions that are executed within a state
    /// in the finite state machine (FSM) framework. Inherit from this class to implement
    /// custom state actions.
    /// </summary>
    ///FIXME:  好像可以架一層有吃參數的比較好？
    [Searchable]
    public abstract class AbstractStateAction : AbstractDescriptionBehaviour, IVoteChild, IGuidEntity,
        IDefaultSerializable, IEventReceiver
    // IArgEventReceiver<GeneralEffectHitData>
    {
        protected override bool HasError()
        {
            return GetComponentInParent<IActionParent>(true) == null;
        }

        protected override string DescriptionTag => "Action";

        //怎麼知道誰用Enter, 誰用Update
        public bool IsValid //AND
        {
            get
            {
                if (_delay) return false;
                return isActiveAndEnabled && _conditions.IsAllValid();
            }
        }


        // [PreviewInInspector]
        //FIXME: 不一定會有bindingState? 還是乾脆拿logic的就好了？
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
        [AutoChildren(DepthOneOnly = true)]
        protected AbstractConditionBehaviour[] _conditions; //condition 成立，才能做事

#if UNITY_EDITOR
        [PreviewInInspector] private bool IsAllValid => _conditions.IsAllValid();
#endif

        protected virtual string renamePostfix => "";

        [AutoParent] private DelayActionModifier delayActionModifier;

        private bool _delay = false; //FIXME: 
        public async void OnActionExecute()
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
                    //有點像sequence? 如果另外包好像還行？
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
            _lastEventReceivedTime = Time.time;
            OnActionExecuteImplement();
        }
        
        protected abstract void OnActionExecuteImplement(); //FIXME: 沒參數的?
        // public void OnActionSpriteUpdate() 
        // {
        //     if (IsValid)
        //         OnSpriteUpdateImplement();
        // }

        [Obsolete]
        protected virtual void OnSpriteUpdateImplement()
        {
        }

        // public async void OnActionExit()
        // {
        //     if (!IsValid) return;
        //     if (delayActionModifier != null) await UniTask.Delay(TimeSpan.FromSeconds(delayActionModifier.delayTime));
        //     OnStateExitImplement();
        // }
        //
        // protected virtual void OnStateExitImplement()
        // {
        // }

        public virtual MonoBehaviour VoteOwner => nearestBinder as MonoBehaviour;
        [AutoParent] private IBinder nearestBinder;

        protected CancellationTokenSource cancellationTokenSource => bindingState.GetStateExitCancellationTokenSource();

  

        //FIXME: 不該全部都virtual
        // public virtual void ArgEventReceived(IEffectHitData arg)
        // {
        //     EventReceived(arg);
        // }
        //
        // public virtual void ArgEventReceived(GeneralEffectHitData arg)
        // {
        //     EventReceived(arg);
        // }

        // public virtual void EventReceived<T>(T arg)
        // {
        //     OnActionExecuteImplement();
        // }
#if UNITY_EDITOR
        [PreviewInInspector] protected float _lastEventReceivedTime = -1f;
#endif

        public void EventReceived()
        {
            _lastEventReceivedTime = Time.time;
            OnActionExecuteImplement();
        }

        public virtual void SimulationUpdate(float passedDuration)
        {
        }

        public virtual void SetPlaybackTime(float time)
        {
        }

        public virtual void Pause()
        {
        }

        public virtual void Resume()
        {
        }
    }
}