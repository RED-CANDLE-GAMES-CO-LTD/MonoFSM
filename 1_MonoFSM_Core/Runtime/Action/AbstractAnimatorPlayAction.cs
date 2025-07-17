using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif


namespace MonoFSM.Core
{
    //[]: 先弄成abstract不會和原專案的class衝突
    [Obsolete]
    public abstract class AbstractAnimatorPlayAction : AbstractStateAction, IAnimatorPlayAction,
        ISceneSavingCallbackReceiver, ITransitionCheckInvoker
    {
        private bool IsStateNameProvider()
        {
            return GetComponent<AbstractStringProvider>() != null;
        }

        [ReadOnly] [TabGroup("Animator", false, 1)] [Required]
        public Animator animator;

        [TabGroup("Animator")]
        [InfoBox("Not Valid State name", InfoMessageType.Error, nameof(IsStateNameNotInAnimator))]
        [ValueDropdown("GetAnimatorStateNames", IsUniqueList = true)]
        [HideIf("IsStateNameProvider")]
        //有provider就藏起來
        public string stateName;

        [InfoBox("Not Valid State name", InfoMessageType.Error, nameof(IsStateNameNotInAnimator))]
        [Title("遇到這個state不要播")]
        [ValueDropdown("GetAnimatorStateNames", IsUniqueList = true)]
        public string ignoreStateName;

        [Auto(false)] private AbstractStringProvider stateNameProvider; //拿旁邊的，蓋掉要怎麼做...藏起來
        private string StateName => stateNameProvider ? stateNameProvider.StringValue : stateName;
        [TabGroup("Animator")] public int stateLayer; //FIXME: 做什麼用的?還要再講清楚? playerLayer

        [TabGroup("Animator")] public float startNormalizedTimeOffset = 0;

        [TabGroup("Animator")] public float animatorEnterCrossFade = 0;


        [Header("Done")] [TabGroup("Animator")] [ValueDropdown("GetLayerNames", IsUniqueList = true)]
        public string doneEventLayerName;

        [TabGroup("Animator")] [ShowInInspector] [ReadOnly] [SerializeField]
        private int doneEventLayer;

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (animator == null)
            {
                var owner = GetComponentInParent<StateMachineOwner>();
                if (owner)
                    animator = owner.GetComponentInChildren<Animator>();
            }


            var layer = GetDoneEventLayerIndex();

            if (doneEventLayer == layer)
                return;

            doneEventLayer = layer == -1 ? 0 : layer;
#endif
        }

        private int animDefaultNameHash;
        // protected override void Start()
        // {
        //     base.Start();
        //     if (animator)
        //     {
        //         animator.keepAnimatorControllerStateOnDisable = true;
        //         animDefaultNameHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        //     }
        // }


        private bool IsStateNameNotInAnimator(string name)
        {
#if UNITY_EDITOR
            if (isActiveAndEnabled == false) //NOTE: 沒開的話不管
                return false;


            var names = GetAnimatorStateNames();
            if (names == null)
                return true;
            foreach (var _name in names)
                if (_name == name)
                    return false;
#endif
            return true;
        }
#if UNITY_EDITOR
        //拿動畫上的所有state name
        private IEnumerable<string> GetAnimatorStateNames()
        {
            return animator.GetAnimatorStateNames(stateLayer);

            // var ac = GetAnimatorController(animator);
            // if (ac == null)
            //     return null;
            //
            // var names = new List<string>();
            // foreach (var state in ac.layers[stateLayer].stateMachine.states)
            // {
            //     names.Add(state.state.name);
            // }
            // return names;
        }

        private IEnumerable<string> GetLayerNames()
        {
            return animator.GetLayerNames();
            // var ac = GetAnimatorController(animator);
            //
            // if (ac == null)
            //     return null;
            //
            //
            // var names = new List<string>();
            // foreach (var layer in ac.layers)
            // {
            //     names.Add(layer.name);
            // }
            // return names;
        }

#endif


        protected override void OnActionExecuteImplement()
        {
            // Debug.Log("Play Animation State");
            if (animator == null || animator.runtimeAnimatorController == null) return;

            this.Log("[AnimatorPlayAction]", gameObject.name, ":[", stateLayer, "]:", StateName);

            // animator.keepAnimatorStateOnDisable = true;

            // var startNormalizedTimeResult = startNormalizedTimeOffset;

            //這個看起來是為了火焰跳transition? 
            // if (CheckInitAndSkipAnimationToLastFrame()) startNormalizedTimeResult = 1;

            //想跳過
            if (animator.GetCurrentAnimatorStateInfo(stateLayer).IsName(ignoreStateName) ||
                animator.GetCurrentAnimatorStateInfo(stateLayer).IsName(StateName))
            {
                this.Log("[AnimatorPlayAction] Skip Animation:", StateName, "layer:", stateLayer);
                return;
            }

            if (animatorEnterCrossFade == 0)
            {
                this.Log("[AnimatorPlayAction] Play Animation:", StateName, "layer:", stateLayer);

                animator.enabled = true;

                animator.Play(StateName, stateLayer); //startNormalizedTimeOffset
            }
            else
            {
                animator.CrossFade(StateName, animatorEnterCrossFade, stateLayer); //startNormalizedTimeOffset
            }

            animator.Update(0);
            // animator.Update(Time.deltaTime);
            // Debug.Break();
        }
#if UNITY_EDITOR
        private int GetDoneEventLayerIndex()
        {
            var names = GetLayerNames();

            if (names == null) return 0;

            var index = 0;
            foreach (var name in names)
            {
                if (name == doneEventLayerName) return index;

                index++;
            }

            return 0;
        }
#endif
        public bool IsPlayingCurrentClip()
        {
            var layer = doneEventLayer;
            if (animator.runtimeAnimatorController == null)
                return false;

            if (animator.isActiveAndEnabled == false)
                return false;

            if (animator.GetCurrentAnimatorStateInfo(layer).IsName(StateName) == false &&
                animator.GetCurrentAnimatorStateInfo(layer).normalizedTime > 0)
                Debug.LogError("AnimatorPlayAction 不該提早切走喔！！！ animator 裡面髒髒。", gameObject);

            if (animator.GetCurrentAnimatorStateInfo(layer).normalizedTime <= 0) return false;

            return animator.GetCurrentAnimatorStateInfo(layer).IsName(StateName);
        }


        //TODO:
        protected override void OnSpriteUpdateImplement()
        {
            // Debug.Log("time:" + animator.GetCurrentAnimatorStateInfo(0).normalizedTime);

            if (doneEventTransition == null)
                return;

            if (IsPlayingCurrentClip() && animator.GetCurrentAnimatorStateInfo(doneEventLayer).normalizedTime >= 1)
                //TODO: AnimationDone
                //Done;
                // GetComponentInParent<GeneralState>().TransitionCheck();
                if (doneEventTransition)
                    // Debug.Log("AnimatorPlayAction > 1" + animator.GetCurrentAnimatorStateInfo(0).normalizedTime + "state:", gameObject);
                    AnimationDone();
            // if (TryGetComponent<EventReceiveTransition>(out var transition))
            // {
            //     Debug.Log("AnimatorPlayAction > 1" + animator.GetCurrentAnimatorStateInfo(0).normalizedTime + "state:", gameObject);
            //     transition.EventReceived("AnimationDone");
            // }
        }

        private void AnimationDone()
        {
            // TransitionTarget.OnTransitionCheck();
            doneEventTransition.IsTransitionCheckNeeded = true;
            // doneEventTransition.EventReceived("AnimationDone");
        }

        [TabGroup("Animator")] [ShowInInspector] [ReadOnly] [Auto(false)]
        private global::StateTransition doneEventTransition;

#if UNITY_EDITOR
        //不一定有，optional...
        [TabGroup("Animator")]
        [HideIf("doneEventTransition")]
        [Button("Add Done Event Transition")]
        private void CreateEventReceiver()
        {
            // doneEventTransition = gameObject.AddChildrenComponent<AbstractStateTransition>("[Transition] Anim Done");
            doneEventTransition = this.TryGetCompOrAdd<global::StateTransition>();
            // doneEventTransition = gameObject.AddComponent<AbstractStateTransition>();
        }


        //TODO: animation clip  ...生成？
        //GenerateAnimationClipInPrefabFolder
        private AnimationClip previewClip;

        private AnimationClip FetchClip()
        {
            var controller = animator.runtimeAnimatorController as AnimatorController;
            //find the clip of the state 
            if (controller == null)
            {
                Debug.LogError("找不到AnimatorController");
                return null;
            }

            //FIXME: 沒有處理override controller?
            var clip = controller.layers[stateLayer].stateMachine.states.First(s => s.state.name == StateName).state
                .motion as AnimationClip;
            previewClip = clip;
            return clip;
        }

        [Button("編輯動畫")]
        public void EditClip()
        {
            Debug.Log("Edit State Clip" + gameObject, this);
            EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
            Selection.activeObject = animator.gameObject;
            var animationWindow = EditorWindow.GetWindow<AnimationWindow>(false);

            //TODO:選不到.. state和clip不會對上？

            // var clip = animator.GetCurrentAnimatorClipInfo(stateLayer)[0].clip;
            // var clip = animator.runtimeAnimatorController.animationClips[""];
            FetchClip();
            animationWindow.Focus();
            animationWindow.animationClip = previewClip;
            animationWindow.previewing = true;
            // animationWindow.recording = true;

            Debug.Log("animationWindow current clip:" + animationWindow.animationClip + "," + previewClip);

            // Debug.Log("Focus Window:" + EditorWindow.focusedWindow.ToString());
            // EditorWindow.GetWindow<ProjectWindowUtil>();
            // ActiveEditorTracker.sharedTracker.isLocked = true;
        }

        public AnimationClip Clip => previewClip ??= FetchClip();
        public Animator BindAnimator => animator;
#endif
        // public void EventReceived<T>(RCGEventReceiver receiver, T arg)
        // {
        //     OnStateEnterImplement();
        // }
        // public void EventReceived<T>(RCGEventReceiver receiver, T arg)
        // {
        //     OnStateEnterImplement();
        // }

        public void OnBeforeSceneSave()
        {
            OnValidate();
        }


        #region InitAndAutoSkipToLastFrame

        [AutoParent(false)] private StateMachineOwner _fsmowner;

        // private bool CheckInitAndSkipAnimationToLastFrame()
        // {
        //     if (_fsmowner == null)
        //     {
        //         Debug.LogError("No _fsmowner?", this);
        //         return false;
        //     }
        //
        //     //state一樣
        //     if (_fsmowner.FsmContext.fsm.LastState == _fsmowner.FsmContext.startState)
        //         return true;
        //
        //     return false;
        // }

        #endregion
    }
}