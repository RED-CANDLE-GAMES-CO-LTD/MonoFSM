using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Sirenix.OdinInspector;

public interface IState<in TState>
{
    // GeneralFSMContext Context { get; }

    bool TransitionCheck(TState toState);
    // bool TransitionCheck(TState toState, float timeOffset, StateTransition fromTransition = null);
}

//FIXME:如果所有的condition都可以自行註冊，這個就不需要了，全部都用condition處理
public interface ITransitionCheckInvoker
{
    //FIXME: bool IsReadyToTransition?
} //interface沒有意義？

//還是用IRCGEventReceiver?
[Searchable]
public class StateTransition : TransitionBehaviour
{
    //FIXME: 檢查沒有任何的condition應該是錯的
    public bool IsTransitionCheckNeeded = false;
}


// [Searchable]
// public class StateTransition : AbstractDescriptionBehaviour, IGuidEntity, IDefaultSerializable, IResetStateRestore,
//     IConditionChangeListener, IOverrideHierarchyIcon, IDrawHierarchyBackGround
// {
// #if UNITY_EDITOR
//     public string IconName => "CollabMoved Icon";
//     public bool IsDrawingIcon => true;
//
//     public Texture2D CustomIcon => null;
//     // UnityEditor.EditorGUIUtility.ObjectContent(null, typeof(StateTransition)).image as Texture2D;
//
// #endif
//     //現在event driven直接set也work, 要做成只有condition改變才會觸發transition?
//     public bool IsTransitionCheckNeeded = false;
//
//     //TODO: 我需要保證附近有checker, 我應該和_checker註冊？
//     //FIXME: 空transition就可以自動過去？
//     [InfoBox("No Checker, 可能需要加StateUpdateAction", InfoMessageType.Error, nameof(NoChecker))]
//     [PreviewInInspector]
//     [AutoParent]
//     [Component(AddComponentAt.Same)]
//     //FIXME: 會有需要parent的情況嗎？ children也包括自己
//     private ITransitionCheckInvoker _checkInvoker; //AnimatorPlayAction動畫...有點鳥
//
//
//     //conditionProvider? 網上問？
//     //要分同層級的嗎？
//     [CompRef] [AutoChildren]
//     private ITransitionCheckInvoker[] _childrenCheckers = Array.Empty<ITransitionCheckInvoker>();
//
//
//     private bool HasChecker()
//     {
//         //refetch? 什麼時候做？每次做超浪費效能耶
//         return _checkInvoker != null ||
//                _childrenCheckers is { Length: > 0 };
//     }
//
//     private bool NoChecker
//         => !HasChecker();
//
//     private bool TransitionValidationResult()
//     {
//         
//         return _target == _parentState as GeneralState;
//     }
//
//     [PreviewInInspector] private string _errorMessage;
//
//     private bool HasError()
//     {
//         if (_target == null)
//         {
//             _errorMessage = "No Target State";
//             return true;
//         }
//
//         //FIXME: cache判定？貴一點要GetComponent...什麼時候refresh? auto找不到的有點麻煩...non serialized...
//         // if (NoChecker)
//         // {
//         //     _errorMessage = "No Checker Invoker in Parent or Children";
//         //     return true;
//         // }
//
//         _errorMessage = "Pass!";
//         return false;
//     }
//
//     //這個其實光是用名字就可以了耶？
//     [FormerlySerializedAs("target")]
//     [MCPExtractable]
//     [InfoBox("Target is self", InfoMessageType.Error, nameof(TransitionValidationResult))]
//     [ValueDropdown(nameof(FindStates), NumberOfItemsBeforeEnablingSearch = 5)]
//     [Required]
//     [Header("Go To")]
//     [GUIColor(0.8f, 0.8f, 1)]
//     [SerializeField]
//     protected GeneralState _target;
//
//     [ReadOnly] [ShowInInspector] public GeneralState Target => _target;
//
//     private IEnumerable<GeneralState> FindStates()
//     {
//         return GetComponentInParent<GeneralFSMContext>(true).GetAllGeneralStates();
//     }
//
//     [PreviewInInspector] [AutoChildren(false)] [Component]
//     private AbstractConditionComp[] conditions = Array.Empty<AbstractConditionComp>();
//
//     [Title("從init來會播動畫的Transition")]
//     [ShowInInspector]
//     public bool IsDefaultTransition => conditions == null || conditions.Length == 0;
//     //試圖封裝 resolving和resolved，不想要把clip和transition分開，有隱含邏輯在裡面
//
//     // protected override void Awake()
//     // {
//     //     bindingState = GetComponentInParent<GeneralState>();
//     // }
//     [Button("測試transition")]
//     private void TransitionTest()
//     {
//         TransitionCheck();
//     }
//
//     // [AutoParent()] private GeneralState bindingState;
//
//     [PreviewInInspector] [AutoParent] private IState<GeneralState> _parentState; //FIXME: 錯了！
//     public IState<GeneralState> ParentState => _parentState;
//     [ShowInInspector] private bool IsSelfTransition => _parentState as GeneralState == _target;
//
//
//     [AutoChildren] private ISkippableAnimationTransition[] _skippableAnimationTransitions;
//
//     [ShowInInspector]
//     public bool IsTransitionSkippable
//     {
//         get
//         {
//             if (_skippableAnimationTransitions == null) return true;
//
//             foreach (var s in _skippableAnimationTransitions)
//                 if (s.CanSkip() == false)
//                     return false;
//
//             return true;
//         }
//     }
//
//
//     [InfoBox("SelfTransition要勾才會過", InfoMessageType.Error, "IsSelfTransitionNotValid")]
//     [ShowInInspector]
//     private bool IsSelfTransitionNotValid
//         => _target != null &&
//            IsSelfTransition &&
//            !_target.CanSelfTransition;
//
//     [PreviewInInspector]
//     public bool TransitionConditionValid
//         => conditions == null ||
//            conditions.IsAllValid();
//
//     // [AutoParent] private RCGCullingGroup _cullingGroup;
//
//     //FIXME: 不該空降call, 只能在系統特定時間點
//     public bool TransitionCheck(float timeOffset = 0)
//     {
//         //FIXME: 有需要嗎？
//         // if (IsTransitionCheckNeeded == false)
//         //     return false;
//         // IsTransitionCheckNeeded = false;
//         if (gameObject.activeSelf == false) //關著也想change state
//             return false;
//
//         //整顆單位關著，表示config沒有想要打開
//         //FIXME:只是為了擋掉關著的FSM?
//         // if (_cullingGroup && _cullingGroup.HasActivated == false)
//         // {
//         //     return false;
//         // }
//
//         if (conditions != null && conditions.IsAllValid() == false)
//             return false;
//
//         //TODO: 這個runtime拿蠻不好的, 改成通通拿IState? 合併anyState和State
//         // var anyState = GetComponentInParent<IState<GeneralState>>();
//         //任何東西都是iState吧？不用分了
//         if (_parentState != null) //走any，直接過
//         {
//             if (_target == null)
//             {
//                 Debug.LogError("No Target! 選一個", gameObject);
//                 return false;
//             }
//
//
//             if (_target.stateType.gameObject.activeSelf == false)
//             {
//                 this.Log("[Transition] Fail ChangeState target inactive" + _target.stateType, gameObject);
//                 return false;
//             }
//
//             if (_parentState.TransitionCheck(_target.stateType, timeOffset, this))
//             {
//                 //FIXME: 這個時間點會太晚嗎？ 會，這個回來就已經切到另一個state了
//                 //會...
//             }
//
//             return true;
//         }
//
//         if (_parentState == null) Debug.LogError("Why no parent State" + _parentState, gameObject);
//
//         return false;
//     }
//
//     public bool IsLastTransition
//         => _parentState != null &&
//            _parentState.Context.LastTransition == this;
//
//     public void ResetStateRestore()
//     {
//         if (!HasChecker()) Debug.LogError("No Checker", gameObject);
//     }
//
//     public override string Description
//         => _target!=null&&_target.stateType!=null?"=>" + _target.stateType.name.Replace("[State]", ""):"";
//
//     protected override string DescriptionTag
//         => "Transition";
//
//     public void OnConditionChanged()
//     {
//         IsTransitionCheckNeeded = true;
//     }
//
//     public Color BackgroundColor => new(1.0f, 0f, 0f, 0.3f);
//
//     public bool IsDrawGUIHierarchyBackground => HasError(); //還是用icon? 
//     //FIXME: highlight related component ex: target state, 偷改他狀態？ 怎麼做標記？ 我被選到的話
// }