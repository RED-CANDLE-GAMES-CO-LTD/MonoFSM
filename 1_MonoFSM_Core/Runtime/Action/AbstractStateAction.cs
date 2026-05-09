using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Vote;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;

namespace MonoFSM.Core.Runtime.Action
{
    /// <summary>
    ///     Represents an abstract base class for defining actions that are executed within a state
    ///     in the finite state machine (FSM) framework. Inherit from this class to implement
    ///     custom state actions.
    /// </summary>
    [Searchable]
    public abstract class AbstractStateAction
        : AbstractDescriptionBehaviour,
            IVoteChild,
            IGuidEntity,
            IDefaultSerializable,
            IEventReceiver,
            IResetStateRestore
    // IArgEventReceiver<GeneralEffectHitData>
    {
        public float DeltaTime => WorldUpdateSimulator.DeltaTime;

        protected override bool HasError() //FIXME: 會很貴嗎？
        {
            //only one
            if (transform.parent.GetComponent<IActionParent>() == null)
            {
                _errorMessage = "No direct action parent found";
                return true;
            }

            return base.HasError();
            // return GetComponentInParent<IActionParent>(true) == null || base.HasError();
        }

        protected override string DescriptionTag => "Action";

        //怎麼知道誰用Enter, 誰用Update
        public bool IsValid //AND
        {
            get
            {
                // if (_delay) //FIXME: 蛤？
                //     return false;
                return gameObject.activeSelf && _conditions.IsAllValid();
                //用activeSelf到底可以嗎？有可能強制都要isActiveAndEnabled？
            }
        }

        // [PreviewInInspector]
        //FIXME: 不一定會有bindingState? 還是乾脆拿logic的就好了？
        [AutoParent]
        protected GeneralState bindingState; // => this.GetComponentInParent<GeneralState>(true)// ;

        //為什麼沒有紅紅？
        [Required]
        [PreviewInInspector]
        [AutoParent]
        // [SerializeField]
        //FIXME: 應該要做 DepthOnly1嗎？
        protected IActionParent _actionParent;

        [HideInInlineEditors]
        // #if UNITY_EDITOR
        [PropertyOrder(1)]
        [TabGroup("Condition", false, 1)]
        [Component(AddComponentAt.Children, "[Condition]")]
        [PreviewInInspector]
        // #endif
        [AutoChildren(DepthOneOnly = true)]
        protected AbstractConditionBehaviour[] _conditions; //condition 成立，才能做事
#if UNITY_EDITOR
        [PreviewInInspector]
        private bool IsAllValid => _conditions.IsAllValid();
#endif

        protected virtual string renamePostfix => "";

        //FIXME: 還是不要用這個好了
        // [AutoParent]
        // private DelayActionModifier delayActionModifier;

        private bool _delay; //FIXME:

        protected virtual bool ForceExecuteInValid => false;

        //FIXME: 不會走這了？
        // public async void OnActionExecute()
        // {
        //     if (!gameObject.activeSelf)
        //         return;
        //     if (_delay)
        //         Debug.LogError("Delay 還沒結束又DELAY 死罪", this);
        //
        //     // _delay = false;
        //     //TODO: conditions
        //     if (!IsValid && !ForceExecuteInValid)
        //         return; //not valid也要用字串？
        //
        //     _delay = true;
        //     if (delayActionModifier != null)
        //         try
        //         {
        //             //FIXME: 這個delay用unitask不好，時間軸和fsm錯開了
        //             //有點像sequence? 如果另外包好像還行？
        //             await UniTask.Delay(
        //                 TimeSpan.FromSeconds(delayActionModifier.delayTime),
        //                 DelayType.DeltaTime,
        //                 PlayerLoopTiming.Update,
        //                 cancellationTokenSource.Token
        //             );
        //         }
        //         catch (OperationCanceledException)
        //         {
        //             _delay = false;
        //             // Debug.LogError("Delay Cancelled" + e, this);
        //             return;
        //         }
        //
        //     _delay = false;
        //     // this.AddTask(OnStateEnterImplement, delayActionModifier.delayTime);
        //     AddEventRecord();
        //     OnActionExecuteImplement();
        //     Debug.Log($"Action Executed: {name} {renamePostfix} at {lastEventReceivedTime}", this);
        // }

        protected abstract void OnActionExecuteImplement();

        public void OnActionRender()
        {
            if (!IsValid) return;
            Profiler.BeginSample("AbstractStateAction OnActionRender", this);
            OnRenderImplement();
            AddEventRecord();
            Profiler.EndSample();
        }

        protected virtual void OnRenderImplement()
        {
            OnActionExecuteImplement(); //做一樣的事？
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

        [AutoParent]
        private IBinder nearestBinder;

        protected CancellationTokenSource cancellationTokenSource =>
            bindingState.GetStateExitCancellationTokenSource();

#if UNITY_EDITOR
        [Serializable]
        public struct EventReceivedRecord
        {
            public int _tick;
            public bool _isForward;

            public override string ToString() => $"tick={_tick} forward={_isForward}";
        }

        [PreviewInDebugMode]
        protected Queue<EventReceivedRecord> _lastEventReceivedRecords = new();

        [PreviewInDebugMode]
        protected EventReceivedRecord lastEventReceivedRecord =>
            _lastEventReceivedRecords.Count > 0
                ? _lastEventReceivedRecords.Last()
                : default;

        private const int MaxEventTimeRecords = 10;
#endif

        //可以用delay modifier?
        [SerializeField]
        [CompRef]
        [Auto]
        private DelayActionModifier _delayActionModifier;

#if UNITY_EDITOR
        [Button]
        void ForceExecute()
        {
            EventReceived();
        }
#endif
        public void EventReceived()
        {
            if (_delayActionModifier == null)
            {
                AddEventRecord(); //FIXME: hmm這個會騙人耶, 該吃arg的結果跑一般的以為有正確執行
                OnActionExecuteImplement();
                return;
            }

            var delayTime = _delayActionModifier.delayTime;
            //primeTween delay?
            PrimeTween.Tween.Delay(
                this,
                delayTime,
                t =>
                {
                    t.AddEventRecord();
                    if (t.gameObject.activeSelf)
                        OnActionExecuteImplement();
                }
            );
        }

        public virtual void SimulationUpdate(float passedDuration) { }

        public virtual void SetPlaybackTime(float time) { }

        public virtual void Pause() { }

        public virtual void Resume() { }

        public virtual void ResetStateRestore(bool isHardReset)
        {
#if UNITY_EDITOR
            _lastEventReceivedRecords.Clear();
#endif
            _delay = false;
        }

#if UNITY_EDITOR
        protected void AddEventRecord()
        {
            var snap = AbstractMonoVariable._networkTickSnapshot;
            _lastEventReceivedRecords.Enqueue(new EventReceivedRecord
            {
                _tick = snap?._tick ?? WorldUpdateSimulator.CurrentTick,
                _isForward = snap?._isForward ?? true,
            });

            // 保持最多10個記錄
            while (_lastEventReceivedRecords.Count > MaxEventTimeRecords)
                _lastEventReceivedRecords.Dequeue();
        }
#else
        protected void AddEventRecord()
        {
            // Release模式下不記錄
        }
#endif
    }
}
