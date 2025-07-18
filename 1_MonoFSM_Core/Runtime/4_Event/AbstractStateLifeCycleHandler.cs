using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Vote;
using MonoFSM.Variable.Attributes;
using RCGExtension;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// Abstract base class for handling complete state lifecycle events (Enter, Update, Exit).
    /// Can be used in two ways:
    /// 1. As a handler that manages child IEventReceiver components (like traditional AbstractEventHandler)
    /// 2. As a base class for Actions that need complete lifecycle control
    /// </summary>
    /// <remarks>
    /// This class serves as the foundation for state lifecycle management, offering:
    /// - Complete state lifecycle support (Enter, Update, Exit)
    /// - Action management through IEventReceiver pattern
    /// - Integration with AbstractStateAction functionality (conditions, delays, etc.)
    /// - Consistent interface for state behavior handling
    /// 
    /// Unlike AbstractEventHandler which handles generic events, this class is specifically
    /// designed for state machine lifecycle management.
    /// </remarks>
    /// <seealso cref="IEventReceiver"/>
    /// <seealso cref="IActionParent"/>
    /// <seealso cref="AbstractEventHandler"/>
    /// <seealso cref="AbstractStateAction"/>
    [Searchable]
    public abstract class AbstractStateLifeCycleHandler : AbstractDescriptionBehaviour, IActionParent, IVoteChild,
        IGuidEntity,
        IDefaultSerializable, IArgEventReceiver<IEffectHitData>
    {
        #region AbstractStateAction Integration

        protected override bool HasError()
        {
            return GetComponentInParent<IActionParent>(true) == null;
        }

        protected override string DescriptionTag => "LifeCycleHandler";

        public bool IsValid
        {
            get
            {
                if (_delay) return false;
                return isActiveAndEnabled && _conditions.IsAllValid();
            }
        }

        [AutoParent] protected GeneralState _bindingState;

        [Required] [PreviewInInspector] [AutoParent]
        protected IActionParent _actionParent;

        [HideInInlineEditors]
        [HideFromFSMExport]
        [PropertyOrder(1)]
        [TabGroup("Condition", false, 1)]
        [Component(AddComponentAt.Children, "[Condition]")]
        [PreviewInInspector]
        [AutoChildren(DepthOneOnly = true)]
        protected AbstractConditionBehaviour[] _conditions;

#if UNITY_EDITOR
        [PreviewInInspector] private bool IsAllValid => _conditions.IsAllValid();
#endif

        [AutoParent] private DelayActionModifier delayActionModifier;
        private bool _delay = false;

        public virtual MonoBehaviour VoteOwner => nearestBinder as MonoBehaviour;
        [AutoParent] private IBinder nearestBinder;

        protected CancellationTokenSource cancellationTokenSource =>
            _bindingState?.GetStateExitCancellationTokenSource();

        #endregion

        #region Event Receiver Management

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        protected IEventReceiver[] _eventReceivers;

        /// <summary>
        /// Execute all registered event receivers.
        /// </summary>
        /// FIXME: 還要這個嗎？
        [Obsolete]
        protected virtual void ExecuteEventReceivers()
        {
            if (!isActiveAndEnabled)
                return;

            if (_eventReceivers != null)
                foreach (var eventReceiver in _eventReceivers)
                    if (eventReceiver.IsValid)
                        eventReceiver.EventReceived();
        }
        #endregion

        #region State Lifecycle Methods

        /// <summary>
        /// Called when the state is entered. Override to implement custom enter behavior.
        /// Base implementation executes child event receivers.
        /// </summary>
        protected virtual void OnStateEnter()
        {
            ExecuteEventReceivers();
        }

        /// <summary>
        /// Called during state update. Override to implement custom update behavior.
        /// Base implementation executes child event receivers.
        /// </summary>
        protected virtual void OnStateUpdate()
        {
            ExecuteEventReceivers();
        }

        /// <summary>
        /// Called when the state is exited. Override to implement custom exit behavior.
        /// Base implementation executes child event receivers.
        /// </summary>
        protected virtual void OnStateExit()
        {
            ExecuteEventReceivers();
        }

        #endregion

        #region Public Interface Methods

        /// <summary>
        /// Public interface for triggering state enter from external sources.
        /// Includes validation and delay handling like AbstractStateAction.
        /// </summary>
        public async void TriggerStateEnter()
        {
            await ExecuteWithValidationAndDelay(() => OnStateEnter());
        }

        /// <summary>
        /// Public interface for triggering state update from external sources.
        /// Includes validation and delay handling like AbstractStateAction.
        /// </summary>
        public async void TriggerStateUpdate()
        {
            await ExecuteWithValidationAndDelay(() => OnStateUpdate());
        }

        /// <summary>
        /// Public interface for triggering state exit from external sources.
        /// Includes validation and delay handling like AbstractStateAction.
        /// </summary>
        public async void TriggerStateExit()
        {
            await ExecuteWithValidationAndDelay(() => OnStateExit());
        }

        #endregion

        #region AbstractStateAction Compatibility Methods

        /// <summary>
        /// Execute with validation and delay, similar to AbstractStateAction.OnActionExecute()
        /// </summary>
        private async UniTask ExecuteWithValidationAndDelay(Action action)
        {
            if (!isActiveAndEnabled) return;
            if (_delay)
            {
                Debug.LogError("Delay 還沒結束又DELAY 死罪", this);
                return;
            }

            if (!IsValid) return;

            _delay = true;
            if (delayActionModifier != null)
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(delayActionModifier.delayTime), DelayType.DeltaTime,
                        PlayerLoopTiming.Update, cancellationTokenSource?.Token ?? CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    _delay = false;
                    return;
                }

            _delay = false;
            action?.Invoke();
        }

        /// <summary>
        /// Compatibility method for IEventReceiver interface
        /// </summary>
        public void EventReceived()
        {
            TriggerStateEnter(); // Default to Enter for basic event handling
        }

        /// <summary>
        /// Compatibility method for IEventReceiver interface with typed argument
        /// </summary>
        public virtual void EventReceived<T>(T arg)
        {
            TriggerStateEnter(); // Default to Enter for basic event handling
        }

        /// <summary>
        /// Compatibility method for IArgEventReceiver interface
        /// </summary>
        public virtual void ArgEventReceived(IEffectHitData arg)
        {
            EventReceived(arg);
        }

        /// <summary>
        /// Compatibility method for simulation update
        /// </summary>
        public virtual void SimulationUpdate(float passedDuration)
        {
        }

        /// <summary>
        /// Compatibility method for setting playback time
        /// </summary>
        public virtual void SetPlaybackTime(float time)
        {
        }

        /// <summary>
        /// Compatibility method for pause
        /// </summary>
        public virtual void Pause()
        {
        }

        /// <summary>
        /// Compatibility method for resume
        /// </summary>
        public virtual void Resume()
        {
        }

        #endregion
    }
}