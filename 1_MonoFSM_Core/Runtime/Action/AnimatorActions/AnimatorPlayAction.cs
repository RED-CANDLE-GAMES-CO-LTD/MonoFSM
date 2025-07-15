using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.AnimatorControl;
using MonoFSM.AnimatorUtility;
using MonoFSM.Foundation;
using MonoFSM.Core.Editor;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace MonoFSM.Animation
{
    //小心從init routing來，會直接播結束的frame，要從transition上知道這件事
    //documentation要放哪？


//FIXME: 把StateAction拔掉？
    [HelpURL("https://www.notion.so/AnimatorPlayA-061be2a2d4e5414e88e84f1ed80d8ea2")]
    [Searchable]
    public class AnimatorPlayAction : AbstractDescriptionBehaviour, IAnimatorPlayAction,
        ISceneSavingCallbackReceiver, ISelfValidator, ISerializableComponent, ITransitionCheckInvoker, IRenderAction
    {
        public override string Description => " " + animator.gameObject.name + ": " + StateName;
        protected override string DescriptionTag => "Anim";

        protected override void Awake()
        {
            base.Awake();
            _stateNameHash = Animator.StringToHash(StateName);
        }

        private bool IsStateNameProvider() 
            => GetComponent<AbstractStringProvider>() != null;

        public void SimulationUpdate(float passedDuration) 
            => animator.playbackTime = passedDuration;

        // FIXME: 不能直接往下找？要從IFSMOwner下面往下找之類的？
        private IEnumerable<Animator> GetAnimatorsInChildren()
        {
            var provider = GetComponentInParent<IAnimatorProvider>();
            return provider?.ChildAnimators;
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
        [HideIf("IsStateNameProvider")]
        //有provider就藏起來
        public string stateName;

        [Auto(false)] private AbstractStringProvider stateNameProvider; //拿旁邊的，蓋掉要怎麼做...藏起來
        public string StateName 
            => stateNameProvider 
                ? stateNameProvider.StringValue 
                : stateName;

        private int StateHash 
            => stateNameProvider && stateNameProvider is AnimatorStateStringListProvider listProvider
                ? listProvider.StateHashValue
                : _stateNameHash;

#if UNITY_EDITOR
        private readonly Dictionary<int, string> _stateHashToName = new();

        private void BuildStateHashToName()
        {
            _stateHashToName.Clear();
            var names = GetAnimatorStateNames();
            if (names == null)
                return;
            foreach (var name in names) _stateHashToName.Add(Animator.StringToHash(name), name);
        }
#endif

        //
        [TabGroup("Animator")] [DisableIf("@true")]
        public int stateLayer; //FIXME: 做什麼用的?還要再講清楚? playerLayer

        // [ValueDropdown()]
#if UNITY_EDITOR
        private void BindStateLayer()
        {
            stateLayer = AnimatorHelpler.GetLayerIndex(animator, _stateLayerName);
        }


        [TabGroup("Animator")]
        [OnValueChanged(nameof(BindStateLayer))]
        [ShowInInspector]
        [ValueDropdown(nameof(GetLayerNames))]
#endif
        private string _stateLayerName;

        private int stateRange => animator.layerCount;

        [TabGroup("Animator")] [Range(0, 1)] public float startNormalizedTimeOffset = 0;

        [TabGroup("Animator")] [Title("StateEnter 空降Normalized Time")] [ShowInPlayMode]
        private float runtimeStartNormalizedTimeOffset = 0;

        [TabGroup("Animator")] public float animatorEnterCrossFade = 0;

#if UNITY_EDITOR
        [TabGroup("Animator")]
        [Button]
        void CreateAnimatorControllerAndClipForState()
        {
            // var controller = animator.runtimeAnimatorController as AnimatorOverrideController;
            var controller = animator.GetAnimatorController();
            if (controller == null)
            {
                // Debug.LogError("animator.runtimeAnimatorController is not AnimatorOverrideController");
                controller = AnimatorControllerGenerator.CreateAnimatorControllerForAnimatorOfCurrentPrefab(animator);
                Debug.Log("CreateAnimatorController"+controller, controller);
            }

            var bindingState = GetComponentInParent<GeneralState>();
            //哭了...怎麼reference?
            var newStateName = bindingState.name.Replace("[State]","").Replace(" ", "");
            AnimatorAssetUtility.AddStateAndCreateClipToLayerIndex(controller, stateLayer,newStateName);
            stateName = newStateName;
        }
#endif
        // private GeneralState bindingState;
        bool IsAnimatorNoControl => animator == null || animator.runtimeAnimatorController == null;
        
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
                Debug.LogError(e, this);
            }


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

#if UNITY_EDITOR
        private bool IsStateNameNotInAnimator(string name)
        {
            if (isActiveAndEnabled == false) //NOTE: 沒開的話不管
                return false;

            var names = GetAnimatorStateNames();
            if (names == null)
                return true;
            foreach (var _name in names)
                if (_name == name)
                    return false;

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
        private void OverrideClip()
        {
            var runtimeAnimatorController = animator.runtimeAnimatorController;
            var animatorOverrideController =
                runtimeAnimatorController as AnimatorOverrideController;

            if (animatorOverrideController == null)
            {
                Debug.LogError("animatorOverrideController == null");
                return;
            }

            var originAnimatorController = animatorOverrideController.runtimeAnimatorController as AnimatorController;
            if (originAnimatorController == null)
            {
                Debug.LogError("originAnimatorController == null");
                return;
            }


            Undo.SetCurrentGroupName("Override Clip");
            var groupIndex = Undo.GetCurrentGroup();
            Undo.RecordObject(animatorOverrideController, "Override Clip");
            // Undo.RecordObject(this, "Override Clip");

            var mappingState = originAnimatorController.layers[stateLayer].stateMachine.states
                .First(s => s.state.name == StateName);
            var baseClip = mappingState.state.motion as AnimationClip;
            var originalClip = animatorOverrideController[baseClip];

            var newClip = AssetDatabaseUtility.CopyAssetOrCreateToPrefabFolder(originalClip, ".clip", (prefabPath) =>
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

                if (animator.runtimeAnimatorController == null)
                    return null;
                //沒有OverrideController
                var animatorController = animator.runtimeAnimatorController as AnimatorController;
                if (animatorController == null)
                    animatorController = ((AnimatorOverrideController)animator.runtimeAnimatorController)
                        .runtimeAnimatorController as AnimatorController;

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
                if (animator == null)
                    return null;
                if (animator.runtimeAnimatorController == null)
                    return null;

                var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
                if (overrideController == null) return null;

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

//如果animator沒開，就不要強迫開啟
        public bool IsDontPlayWhenAnimatorDisabled = false;

        // protected override void OnStateEnterImplement()
        // {
        //     OnEnterRender();
        // }

        public Action<AnimationClip> OnClipPlay;
        private Action<string> _onStateNameChange;

        [TabGroup("Animator")] [ShowInPlayMode]
        private int _stateNameHash;
#if UNITY_EDITOR
        [HideIf(nameof(NoDoneEventTransition))]
        [Header("Done")]
        [TabGroup("Animator")]
        [ValueDropdown("GetLayerNames", IsUniqueList = true)]
#endif
        public string doneEventLayerName; //getter? onvalidate的時候，選的時候選string，存int？

        [HideIf(nameof(NoDoneEventTransition))] [TabGroup("Animator")] [ShowInInspector] [ReadOnly] [SerializeField]
        private int doneEventLayer;



        [TabGroup("Animator")]
        // [HideIf(nameof(NoDoneEventTransition))]
        [PreviewInInspector]
        private float ClipLength
        {
            get
            {
#if UNITY_EDITOR
                if (Mathf.Approximately(_cachedClipLength, -1))
                {
                    var currentClip = CurrentClip;
                    if (currentClip == null)
                        return -1;
                    _cachedClipLength = currentClip.length;
                }
#endif
                return _cachedClipLength; 
            }
        }


        [SerializeField] private float _cachedClipLength = -1;

#if UNITY_EDITOR
        [Button]
        private void CalculateClipLength()
        {
            if (CurrentClip == null)
            {
                Debug.LogError("CurrentClip is null, cannot calculate length", this);
                return;
            }

            _cachedClipLength = CurrentClip.length;
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


        public void SetPlaybackTime(float time)
        {
            var normalizedTime = time / ClipLength;
            animator.Play(StateHash, stateLayer, normalizedTime);
            animator.Update(0);
        }


#endif
        public void Pause()
        {
            animator.speed = 0;
        }

        public void Resume()
        {
            animator.speed = 1;
        }

        [ShowInPlayMode]
        private float CurrentPlayingNormalizedTime =>
            animator.GetCurrentAnimatorStateInfo(doneEventLayer).normalizedTime;

        [AutoParent] private MonoStateBehaviour _stateBehaviour; //這個是State的行為，還是要有個StateAction來做事情
        //FIXME: 錯了！抓到BUG 要cache? 切State後，StateTime就會重置了
        public bool IsDone => _stateBehaviour.StateTime >= ClipLength; // && IsPlayingCurrentClip();
        

        // [SerializeField] private float clipDuration;

        //FIXME: 用邏輯時間
        // public bool IsDone => CurrentPlayingNormalizedTime >= 1; // && IsPlayingCurrentClip();

        private bool IsStatePlaying(int layer)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == StateHash;
        }

        private void OnEnable() //State雖然過來了，但是關著就沒有進ActionStateEnter
        {
            HasAnimationPlaySuccess = false;
        }

        private bool HasAnimationPlaySuccess
        {
            get => _hasAnimationPlaySuccess;
            set => _hasAnimationPlaySuccess = value;
            // this.Log("HasAnimationPlaySuccess:", value);
        }

        private bool _hasAnimationPlaySuccess;

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
                if (IsStatePlaying(layer) == false && stateInfo.normalizedTime > 0) //正在播別的state
                {
#if UNITY_EDITOR
                    if (_stateHashToName.Count == 0)
                        BuildStateHashToName();
                    if (_stateHashToName.ContainsKey(StateHash) == false)
                    {
                        Debug.LogError("AnimatorPlayAction: 沒有這個state:" + StateName + ",hash:" + StateHash, gameObject);
                        return false;
                    }

                    var shouldPlayStateName = _stateHashToName[StateHash];
                    var playingStateName = _stateHashToName[stateInfo.shortNameHash];
                    if (ClipLength == -1)
                    {
                        Debug.LogError("Null Clip of State: ClipLength == -1", this);
                    }

                    else if (HasAnimationPlaySuccess)
                    {
// #if UNITY_EDITOR
//                         EditorUtility.DisplayDialog("AnimatorPlayAction",
//                             "AnimatorPlayAction 不該提早切走喔！(應該是animator controller裡面有transition) should play: " +
//                             shouldPlayStateName +
//                             ", playing: " + playingStateName + ", time:" + stateInfo.normalizedTime, "OK");
// #endif
                        Debug.LogError(
                            "AnimatorPlayAction 不該提早切走喔！(應該是animator controller裡面有transition) should play: " +
                            shouldPlayStateName +
                            ", playing: " + playingStateName + ", time:" + stateInfo.normalizedTime, gameObject);
                        // Debug.Break();
                    }

#else
                        // Debug.LogError("AnimatorPlayAction 不該提早切走喔！(應該是animator controller裡面有transition) should play: "+this._fsmOwner.name, gameObject);
#endif
                }


            if (stateInfo.normalizedTime <= 0) return false;

            var result = IsStatePlaying(layer);

            //這裡有小髒髒狀態
            if (result && HasAnimationPlaySuccess == false) //有沒有播到這個play action要的state過
                HasAnimationPlaySuccess = true;
            return result;
        }

        public bool IsPlayDone => IsPlayingCurrentClip() && CurrentPlayingNormalizedTime >= 1;

        //TODO:
        // protected override void OnSpriteUpdateImplement()
        // {
        //     OnRender();
        // }

        public Action OnAnimationDone;

        private void AnimationDone()
        {
            // TransitionTarget.OnTransitionCheck();
            //FIXME: 還是應該用condition...hmm
            doneEventTransition.IsTransitionCheckNeeded = true;
            OnAnimationDone?.Invoke(); //事件驅動，不太好
            // doneEventTransition.EventReceived("AnimationDone");
        }

        private void OnDestroy()
        {
            OnAnimationDone = null;
        }

        private bool NoDoneEventTransition()
        {
            return GetComponentsInChildren<StateTransition>() == null;
        }

        [HideIf(nameof(NoDoneEventTransition))] [TabGroup("Animator")] [PreviewInInspector] [Component] [AutoChildren]
        private StateTransition doneEventTransition; //寫成condition更好？

        private IEventReceiver _ircgArgEventReceiverImplementation;

#if UNITY_EDITOR
        //不一定有，optional...
        [TabGroup("Animator")]
        [Button("Add Done Event Transition")]
        [ShowIf(nameof(NoDoneEventTransition))]
        private void CreateEventReceiver()
        {
            // doneEventTransition = gameObject.AddChildrenComponent<AbstractStateTransition>("[Transition] Anim Done");
            doneEventTransition = this.AddChildrenComponent<StateTransition>("[Transition] Anim Done");
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

        public void OnBeforeSceneSave()
        {
            OnValidate();
        }


        #region InitAndAutoSkipToLastFrame

        [AutoParent(false)] private StateMachineOwner _fsmOwner; //monster也可以，應該抽成interface

        // private bool CheckInitAndSkipAnimationToLastFrame()
        // {
        //     if (_fsmOwner == null)
        //         // Debug.LogError("No _fsmowner?", this);
        //         return false;
        //
        //     //只有在init的時候才會跳過
        //     var context = _fsmOwner.FsmContext;
        //     if (context.LastState != context.startState)
        //         // this.Log("Not InitAndAutoSkipToLastFrame", context.LastState, ",",
        //         //     context.startState);
        //         return false;
        //
        //     if (context.LastTransition && context.LastTransition.IsTransitionSkippable == false) return false;
        //
        //
        //     this.Log("InitAndAutoSkipToLastFrame", context.LastState, ",",
        //         context.LastTransition);
        //     // this.Break();
        //     return true;
        // }

        #endregion

        public void Validate(SelfValidationResult result)
        {
#if UNITY_EDITOR
            if (IsStateNameNotInAnimator(stateName))
                // Debug.LogError("AnimatorPlayAction: 沒有這個state:" + StateName + ",hash:" + StateHash, gameObject);
                result.AddError("AnimatorPlayAction: 沒有這個state:" + StateName + ",hash:" + StateHash);
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

        // public void EnterLevelAwake()
        // {
        //     //想要留著動畫的狀態，這個是不是也來不及？
        //     animator.keepAnimatorStateOnDisable = true;
        //     
        // }
        // public ITransitionCheckingTarget ValueChangedTarget => doneEventTransition;
        public void OnEnterRender() //transition更早就判定？導致done錯了？
        {
            // Debug.Log("Play Animation State");
            HasAnimationPlaySuccess = false;
            if (animator == null)
            {
                Debug.LogError("animator is null" + _fsmOwner.name, this);
                return;
            }

            if (animator.runtimeAnimatorController == null)
                // Debug.Log(animator);
                // Debug.Log(animator.runtimeAnimatorController);
                // Debug.LogError("animator.runtimeAnimatorController == null? "+this._fsmOwner.name,this);
                return;

            //FIXME: 這個感覺有點危險
            // animator.keepAnimatorStateOnDisable = true;
            if (IsDontPlayWhenAnimatorDisabled == false)
                animator.enabled = true;

            if (animator.isActiveAndEnabled == false)
                // Debug.LogError("animator.isActiveAndEnabled == false "+this._fsmOwner.name,this);
                return;

            this.Log("[AnimatorPlayAction]", gameObject, ":[", stateLayer, "]:", StateName);


            runtimeStartNormalizedTimeOffset = startNormalizedTimeOffset;
            //FIXME: init skip to last frame是不是不好...該拆兩個狀態就拆兩個狀態吧？
            // if (CheckInitAndSkipAnimationToLastFrame())
            //     runtimeStartNormalizedTimeOffset = 1;

            if (animatorEnterCrossFade == 0)
            {
                this.Log("Play Animation:", StateName, "layer:", stateLayer);
                // Debug.Log("Play Animation:" + StateName + "layer:" + stateLayer, this);
                animator.enabled = true;
#if UNITY_EDITOR
                if (!animator.HasState(stateLayer, StateHash))
                    Debug.LogError("AnimatorPlayAction: 沒有這個state:" + StateName + ",hash:" + StateHash, gameObject);

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

            // FIXME: 不要update 0就不會造成這個onenable了？
            // 是什麼情境一定要OnEnable?
            animator.Update(0);
            // animator.Update(RCGTime.deltaTime);
            // Debug.Break();
        }

        public void OnRender()
        {
            // Debug.Log("time:" + animator.GetCurrentAnimatorStateInfo(0).normalizedTime);

            if (doneEventTransition == null)
                return;


#if UNITY_EDITOR
            if (animator == null) Debug.LogError("animator == null", this);
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

            // if (!IsPlayingCurrentClip())
            // {
            //     animator.Play(StateHash, stateLayer, runtimeStartNormalizedTimeOffset);
            //     animator.Update(0);
            // }

            //FIXME: 要animator.Update(0)?
            // UnityEngine.Debug.Log("Current Animator State length:" + info.length + ",normalizedTime:" +
            //                       info.normalizedTime + "," +
            //                       info.shortNameHash);
            if (IsPlayingCurrentClip() && CurrentPlayingNormalizedTime >= 1)
                //TODO: AnimationDone
                //Done;
                // GetComponentInParent<GeneralState>().TransitionCheck();
                if (doneEventTransition)
                {
                    this.Log(
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

        public override void OnBeforePrefabSave()
        {
#if UNITY_EDITOR
            CalculateClipLength();
            base.OnBeforePrefabSave();
#endif
        }
    }
}