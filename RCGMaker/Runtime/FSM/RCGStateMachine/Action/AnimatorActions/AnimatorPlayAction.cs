using System.Collections.Generic;
using System.Linq;
using RCGFSM.AnimatorControl;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace RCGFSM.Animation
{
    //小心從init routing來，會直接播結束的frame，要從transition上知道這件事
    [Searchable]
    public class AnimatorPlayAction : AbstractStateAction, IRCGArgEventReceiver, IAnimatorPlayAction,
        ISceneSavingCallbackReceiver, ISelfValidator, ISerializableComponent
    {
        protected override void Awake()
        {
            base.Awake();
            _stateNameHash = Animator.StringToHash(StateName);
        }

      

        bool IsStateNameProvider()
        {
            return GetComponent<AbstractStringProvider>() != null;
        }

        //FIXME: 不能直接往下找？要從IFSMOwner下面往下找之類的？
        IEnumerable<Animator> GetAnimatorsInChildren()
        {
            var provider = GetComponentInParent<IAnimatorProvider>();
            return provider.ChildAnimators;
        }
        
        [TabGroup("Animator", false, 1)]
        [Required]
        // [InlineEditor]
        [ValueDropdown(nameof(GetAnimatorsInChildren), IsUniqueList = true, NumberOfItemsBeforeEnablingSearch = 3)]
        public Animator animator;

        [InlineEditor] [PreviewInInspector] private Animator animatorComp => animator;
        
        [TabGroup("Animator")]
#if UNITY_EDITOR
        [InfoBox("Not Valid State name", InfoMessageType.Error, nameof(IsStateNameNotInAnimator))]

        [ValueDropdown(nameof(GetAnimatorStateNames), IsUniqueList = true, NumberOfItemsBeforeEnablingSearch = 3)]
#endif
        [HideIf("IsStateNameProvider")] //有provider就藏起來
        public string stateName;
        [Auto(false)] AbstractStringProvider stateNameProvider; //拿旁邊的，蓋掉要怎麼做...藏起來
        public string StateName => stateNameProvider ? stateNameProvider.StringValue : stateName;

        private int StateHash
        {
            get
            {
                if (stateNameProvider && stateNameProvider is AnimatorStateStringListProvider listProvider)
                    return listProvider.StateHashValue;
                else
                {
                    return _stateNameHash;
                }
            }
        }
        
        #if UNITY_EDITOR

        private Dictionary<int, string> _stateHashToName = new();

        private void BuildStateHashToName()
        {
            _stateHashToName.Clear();
            var names = GetAnimatorStateNames();
            if (names == null)
                return;
            foreach (var name in names)
            {
                _stateHashToName.Add(Animator.StringToHash(name), name);
            }
        }
        #endif
      
        //
        [TabGroup("Animator")]
        [DisableIf("@true")]
        public int stateLayer;//FIXME: 做什麼用的?還要再講清楚? playerLayer

        // [ValueDropdown()]
#if UNITY_EDITOR
        void BindStateLayer()
        {
            stateLayer = AnimatorHelpler.GetLayerIndex(animator, _stateLayerName);
        }


        [TabGroup("Animator")]
        [OnValueChanged(nameof(BindStateLayer))]
        [ShowInInspector]
        [ValueDropdown(nameof(GetLayerNames))]
#endif
        string _stateLayerName;
        
        private int stateRange => animator.layerCount;
        
        [TabGroup("Animator")]
        [Range(0, 1)]
        public float startNormalizedTimeOffset = 0;

        [TabGroup("Animator")] [Title("StateEnter 空降Normalized Time")] [ShowInPlayMode]
        float runtimeStartNormalizedTimeOffset = 0;

        [TabGroup("Animator")]
        public float animatorEnterCrossFade = 0;

       
        private void OnValidate()
        {
#if UNITY_EDITOR
            try
            {
                if (animator == null)
                {
                    var owner = GetComponentInParent<StateMachineOwner>();
                    if (owner)
                        animator = owner.GetComponentInChildren<Animator>();
                    if (animator == null)
                        return;
                }

                if (animator.runtimeAnimatorController == null)
                    return;

                var ac = animator.GetAnimatorController();
                if (ac == null)
                    return;
                _stateLayerName = ac.layers[stateLayer].name;

                var layer = GetDoneEventLayerIndex();
                if (doneEventLayer == layer)
                    return;

                doneEventLayer = layer == -1 ? 0 : layer;
            }
            catch (Exception e)
            {
                Debug.LogError(e,this);
            }



#endif

        }
        int animDefaultNameHash;
        // protected override void Start()
        // {
        //     base.Start();
        //     if (animator)
        //     {
        //         animator.keepAnimatorControllerStateOnDisable = true;
        //         animDefaultNameHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        //     }
        // }
        
#if UNITY_EDITOR
        bool IsStateNameNotInAnimator(string name)
        {
            if (isActiveAndEnabled == false)//NOTE: 沒開的話不管
                return false;

            var names = GetAnimatorStateNames();
            if (names == null)
                return true;
            foreach (var _name in names)
            {
                if (_name == name)
                    return false;
            }
            return true;
        }
        //拿動畫上的所有state name
        #if UNITY_EDITOR
        public IEnumerable<string> GetAnimatorStateNames()
        {
            return AnimatorHelpler.GetAnimatorStateNames(animator, stateLayer);
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
        #endif

        #if UNITY_EDITOR
        void OverrideClip()
        {
            var runtimeAnimatorController = animator.runtimeAnimatorController;
            AnimatorOverrideController animatorOverrideController = runtimeAnimatorController as AnimatorOverrideController;
            
            if (animatorOverrideController == null)
            {
                Debug.LogError("animatorOverrideController == null");
                return;
            }
            
            var originAnimatorController = animatorOverrideController.runtimeAnimatorController as AnimatorController;
            if(originAnimatorController == null)
            {
                Debug.LogError("originAnimatorController == null");
                return;
            }
          
      
            Undo.SetCurrentGroupName("Override Clip");
            var groupIndex = Undo.GetCurrentGroup();
            Undo.RecordObject(animatorOverrideController, "Override Clip");
            // Undo.RecordObject(this, "Override Clip");
            
            var mappingState = originAnimatorController.layers[stateLayer].stateMachine.states.First(s => s.state.name == StateName);
            var baseClip = mappingState.state.motion as AnimationClip;
            var originalClip = animatorOverrideController[baseClip];
            
            var newClip = AssetDatabaseUtility.CopyAssetOrCreateToPrefabFolder(originalClip,".clip" ,(prefabPath) =>
            {
                var clip = new AnimationClip();
                // AssetDatabase.CreateAsset(clip, path);
                return clip;
            });
            //copy asset to new clip
            //override clip
        
            animatorOverrideController[originalClip] = newClip;
            animatorOverrideController.SetDirty();
        
            // PrefabUtility.RecordPrefabInstancePropertyModifications(animatorPlayAction);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(groupIndex);
        }
        [TabGroup("Animator")]
        [PropertyOrder(-1)]
        [ShowInInspector]
        private AnimationClip BaseClip
        {
            get
            {
                if (animator == null)
                    return null;
                
                if(animator.runtimeAnimatorController == null)
                    return null;
                //沒有OverrideController
                var animatorController = animator.runtimeAnimatorController as AnimatorController;
                if (animatorController == null)
                {
                    animatorController = ((AnimatorOverrideController)animator.runtimeAnimatorController)
                        .runtimeAnimatorController as AnimatorController;
                }
                if (animatorController == null)
                    return null;
                try
                {
                    var state1 = animatorController.layers[stateLayer].stateMachine.states
                        .First(s => s.state.name == StateName).state;
                    return state1.motion as AnimationClip;
                }
                catch
                {
                    return null;
                }
            }
        }

        #endif
        
        // [CustomContextMenu("Override Clip", nameof(OverrideClip))]
     

        private IEnumerable<string> GetLayerNames()
        {
            return AnimatorHelpler.GetLayerNames(animator);
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
        
        [CustomContextMenu("Override Clip", nameof(OverrideClip))]
        [TabGroup("Animator")]
        [PropertyOrder(-1)]
        [ShowInInspector]
        private AnimationClip OverridingClip
        {
            get
            { 
                if(animator == null)
                    return null;
                if(animator.runtimeAnimatorController == null)
                    return null;
                
                var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
                if (overrideController == null)
                {
                    return null;
                }
                
                var ac = overrideController.runtimeAnimatorController as AnimatorController;
                if (ac == null)
                    return null;
                try
                {
                    var state = ac.layers[stateLayer].stateMachine.states.First(s => s.state.name == StateName).state;

                    var originalClip = state.motion as AnimationClip;
                    //有override controller但是沒有override clip
                    if (originalClip == overrideController[originalClip])
                        return null;
                    return overrideController[originalClip];
                }

                catch
                {
                    return null;
                }
            }
        }

#endif
        

   

        protected override void OnStateEnterImplement()
        {
            // Debug.Log("Play Animation State");

            if (animator == null)
            {
                Debug.LogError("animator is null" + _fsmOwner.name, this);
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                // Debug.Log(animator);
                // Debug.Log(animator.runtimeAnimatorController);
                // Debug.LogError("animator.runtimeAnimatorController == null? "+this._fsmOwner.name,this);
                return;
            }
            animator.keepAnimatorStateOnDisable = true;
            animator.enabled = true;

            if (animator.isActiveAndEnabled == false)
            {
                // Debug.LogError("animator.isActiveAndEnabled == false "+this._fsmOwner.name,this);
                return;
            }

            this.Log("[AnimatorPlayAction]", gameObject, ":[", stateLayer, "]:", StateName);


            runtimeStartNormalizedTimeOffset = startNormalizedTimeOffset;
            //FIXME: init skip to last frame是不是不好...該拆兩個狀態就拆兩個狀態吧？
            if (CheckInitAndSkipAnimationToLastFrame()) runtimeStartNormalizedTimeOffset = 1;

            if (animatorEnterCrossFade == 0)
            {
                this.Log("Play Animation:", StateName, "layer:", stateLayer);
                // Debug.Log("Play Animation:" + StateName + "layer:" + stateLayer, this);
                animator.enabled = true;
#if UNITY_EDITOR
                if (!animator.HasState(stateLayer, StateHash))
                {
                    Debug.LogError("AnimatorPlayAction: 沒有這個state:" + StateName + ",hash:" + StateHash, gameObject);
                }

                OnClipPlay?.Invoke(CurrentClip);
#endif
                //如果是init state過來的，就直接跳到最後一幀
                animator.Play(StateHash, stateLayer, runtimeStartNormalizedTimeOffset);

             
                _onStateNameChange?.Invoke(StateName);
            }
            else
            {
                animator.CrossFade(StateHash, animatorEnterCrossFade, stateLayer, runtimeStartNormalizedTimeOffset);
            }

            animator.Update(0);
            // animator.Update(RCGTime.deltaTime);
            // Debug.Break();
        }

        public Action<AnimationClip> OnClipPlay;
        Action<string> _onStateNameChange;
        
        [TabGroup("Animator")]
        [ShowInPlayMode]
        private int _stateNameHash;
        #if UNITY_EDITOR
        [HideIf(nameof(NoDoneEventTransition))]
        [Header("Done")]
        [TabGroup("Animator")]
        [ValueDropdown("GetLayerNames", IsUniqueList = true)]
        #endif
        public string doneEventLayerName; //getter? onvalidate的時候，選的時候選string，存int？
       
        [HideIf(nameof(NoDoneEventTransition))]
        [TabGroup("Animator")]
        [ShowInInspector]
        [ReadOnly]
        [SerializeField]
        int doneEventLayer;

  
   


#if UNITY_EDITOR
        [TabGroup("Animator")]
        // [HideIf(nameof(NoDoneEventTransition))]
        [PreviewInInspector]
        private float ClipLength
        {
            get
            {
                var currentClip = CurrentClip;
                if (currentClip == null)
                    return -1;
                return currentClip.length;
            }
        }

        [TabGroup("Animator")]
        // [HideIf(nameof(NoDoneEventTransition))]
        [PreviewInInspector]
        private bool IsClipLoop
        {
            get
            {
                var currentClip = CurrentClip;
                if (currentClip == null)
                    return false;
                return currentClip.isLooping;
            }
        }

        private AnimationClip CurrentClip
        {
            get
            {
                var overridingClip = OverridingClip;
                if (overridingClip != null)
                    return overridingClip;
                var baseClip = BaseClip;
                if (baseClip != null)
                    return baseClip;
                return null;
            }
        }
        int GetDoneEventLayerIndex()
        {
            var names = GetLayerNames();

            if (names == null)
            {
                return 0;
            }

            int index = 0;
            foreach (string name in names)
            {
                if (name == doneEventLayerName)
                {
                    return index;
                }
                index++;
            }
            return 0;
        }


        public override void SetPlaybackTime(float time)
        {
            var normalizedTime = time / ClipLength;
            animator.Play(StateHash, stateLayer, normalizedTime);
            animator.Update(0);
        }

      
#endif
        public override void Pause()
        {
            animator.speed = 0;
        }

        public override void Resume()
        {
            animator.speed = 1;
        }

        [ShowInPlayMode]
        private float CurrentPlayingNormalizedTime =>
            animator.GetCurrentAnimatorStateInfo(doneEventLayer).normalizedTime;

        public bool IsDone => CurrentPlayingNormalizedTime >= 1;
        private bool IsStatePlaying(int layer)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == StateHash;
        }
        public bool IsPlayingCurrentClip()
        {
            var layer = doneEventLayer;
            if (animator.runtimeAnimatorController == null)
                return false;

            if (animator.isActiveAndEnabled == false)
                return false;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            
            
            //Cross fade 這邊一定會叫
            if (animatorEnterCrossFade <= 0)
                if (IsStatePlaying(layer) == false && stateInfo.normalizedTime > 0)
                {
#if UNITY_EDITOR
                    if (_stateHashToName.Count == 0)
                        BuildStateHashToName();
                    var shouldPlayStateName = _stateHashToName[StateHash];
                    var playingStateName = _stateHashToName[stateInfo.shortNameHash];
                    if (ClipLength == -1)
                    {
                        Debug.LogError("Null Clip of State: ClipLength == -1", this);
                    }
                    else
                        Debug.LogError(
                        "AnimatorPlayAction 不該提早切走喔！(應該是animator controller裡面有transition) should play: " +
                        shouldPlayStateName +
                        ",playingStateName: " + playingStateName, gameObject);
#else
                        Debug.LogError("AnimatorPlayAction 不該提早切走喔！(應該是animator controller裡面有transition) should play: "+this._fsmOwner.name, gameObject);
#endif
                    
          
                }
            

            if (stateInfo.normalizedTime <= 0)
            {
                return false;
            }

            return IsStatePlaying(layer);
        }

        

        //TODO:
        protected override void OnSpriteUpdateImplement()
        {
            // Debug.Log("time:" + animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            
            if (doneEventTransition == null)
                return;


#if UNITY_EDITOR
            if (animator == null)
            {
                Debug.LogError("animator == null",this);
            }
#endif
            if (animator.runtimeAnimatorController == null)
            {
                enabled = false;
                return;
            }

            //FIXME: 完全知道動畫多久，可以預判播完的時間然後去下一個state，就可以functional?
            //包子 Cross Fade 不能一直跑 （議會小電梯） 
            if (animator.isActiveAndEnabled && animatorEnterCrossFade <= 0)
                animator.Play(StateHash, stateLayer);

            var info = animator.GetCurrentAnimatorStateInfo(doneEventLayer);
            // UnityEngine.Debug.Log("Current Animator State length:" + info.length + ",normalizedTime:" +
            //                       info.normalizedTime + "," +
            //                       info.shortNameHash);
            if (IsPlayingCurrentClip() && CurrentPlayingNormalizedTime >= 1)
            {
             
                //TODO: AnimationDone
                //Done;
                // GetComponentInParent<GeneralState>().TransitionCheck();
                if (doneEventTransition)
                {
                    Debug.Log(
                        "AnimatorPlayAction > 1:" + CurrentPlayingNormalizedTime + "state:" +
                        StateName,
                        gameObject);
                    // Debug.Break();
                    AnimationDone();
                }
                // if (TryGetComponent<EventReceiveTransition>(out var transition))
                // {
                //     Debug.Log("AnimatorPlayAction > 1" + animator.GetCurrentAnimatorStateInfo(0).normalizedTime + "state:", gameObject);
                //     transition.EventReceived("AnimationDone");
                // }
            }
        }

        public Action OnAnimationDone;
        void AnimationDone()
        {
            doneEventTransition.TransitionCheck();
            OnAnimationDone?.Invoke();
            // doneEventTransition.EventReceived("AnimationDone");
        }
        

        bool NoDoneEventTransition()
        {
            return GetComponent<AbstractStateTransition>() == null;
        }
        
        [HideIf(nameof(NoDoneEventTransition))]
        [TabGroup("Animator")]
        [PreviewInInspector]
        [Auto(false)] AbstractStateTransition doneEventTransition;

        private IRCGArgEventReceiver _ircgArgEventReceiverImplementation;

#if UNITY_EDITOR
        //不一定有，optional...
        [TabGroup("Animator")]
        [Button("Add Done Event Transition")]
        [ShowIf(nameof(NoDoneEventTransition))]
        void CreateEventReceiver()
        {
            // doneEventTransition = gameObject.AddChildrenComponent<AbstractStateTransition>("[Transition] Anim Done");
            doneEventTransition = this.TryGetCompOrAdd<AbstractStateTransition>();
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
            animationWindow.animationClip = CurrentClip; // previewClip;
            animationWindow.previewing = true;
            // animationWindow.recording = true;

            Debug.Log("animationWindow current clip:" + animationWindow.animationClip + "," + previewClip);

            // Debug.Log("Focus Window:" + EditorWindow.focusedWindow.ToString());
            // EditorWindow.GetWindow<ProjectWindowUtil>();
            // ActiveEditorTracker.sharedTracker.isLocked = true;
        }

        public AnimationClip Clip => CurrentClip;
        public Animator BindAnimator => animator;
#endif
        // public void EventReceived<T>(RCGEventReceiver receiver, T arg)
        // {
        //     OnStateEnterImplement();
        // }
        public void EventReceived<T>(T arg)
        {
            OnStateEnterImplement();
        }

        public void OnBeforeSceneSave()
        {
            OnValidate();
        }


        #region InitAndAutoSkipToLastFrame

        [AutoParent(false)] private StateMachineOwner _fsmOwner; //monster也可以，應該抽成interface
        private bool CheckInitAndSkipAnimationToLastFrame()
        {
            if (_fsmOwner == null)
            {
                // Debug.LogError("No _fsmowner?", this);
                return false;
            }
            
            //只有在init的時候才會跳過
            var context = _fsmOwner.FsmContext;
            if (context.LastState != context.startState)
            {
                this.Log("Not InitAndAutoSkipToLastFrame", context.LastState, ",",
                    context.startState);
                return false;
            }

            if (context.LastTransition && context.LastTransition.IsTransitionSkippable == false)
            {
                return false;
            }


            this.Log("InitAndAutoSkipToLastFrame", context.LastState, ",",
                context.LastTransition);
            // this.Break();
            return true;


        }

        #endregion

        public void Validate(SelfValidationResult result)
        {
#if UNITY_EDITOR
            if (IsStateNameNotInAnimator(stateName))
            {
                // Debug.LogError("AnimatorPlayAction: 沒有這個state:" + StateName + ",hash:" + StateHash, gameObject);
                result.AddError("AnimatorPlayAction: 沒有這個state:" + StateName + ",hash:" + StateHash);
            }
#endif
        }

        [TabGroup("擴充模組")] [AutoChildren] [Component(addAt = AddComponentAt.Same)] [PreviewInInspector]
        private AnimatorPlayActionModule[] _animatorPlayActionModule;

        public string Serialize()
        {
            //get field which is not default value?
            return GetType().Name + " " + animator.name + " " + stateName;
        }

        public void Deserialize(string data)
        {
            throw new NotImplementedException();
        }
    }
}

