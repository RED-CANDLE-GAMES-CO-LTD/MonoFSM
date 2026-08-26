using MonoFSM.FSM;
using MonoFSM.Core;
using MonoFSM.Core.Simulate;
using MonoFSM.EditorExtension;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    //只需要這個就好了嗎？但是
    public class TransitionBehaviour
        : TransitionBehaviour<MonoStateBehaviour>,
            IOverrideHierarchyIcon,
            IDrawHierarchyBackGround
    {
        protected override string DescriptionTag => "Transition";

        public override string Description =>
            _target != null && _target.Name != null
                ? "=>" + _target.Name.Replace("[State]", "")
                : "";

        protected override void Awake()
        {
            _transitionData = new TransitionData<MonoStateBehaviour>(
                _target,
                (from, to) =>
                {
                    if (isActiveAndEnabled == false)
                        return false;

                    // Check all conditions
                    if (AreConditionsValid() == false)
                        return false;
#if UNITY_EDITOR
                    RecordDebugTick(from);
#endif
                    return true;
                }
            );
        }

        [OnValueChanged(nameof(Rename))]
        [Required]
        [DropDownRef]
        public MonoStateBehaviour _target;

        // [RequiredListLength(1, null)]
        [SerializeField]
        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private AbstractConditionBehaviour[] _conditions;

        /// <summary>
        /// 只檢查這個 transition 的 condition 群，不含自身 isActiveAndEnabled 與 target 判定。
        /// 給 TransitionRef 共用同一組條件用。
        /// </summary>
        public bool AreConditionsValid()
        {
            return _conditions.IsAllValid();
        }

#if UNITY_EDITOR
        //transition 通過條件的 tick 歷史，用來對照 resimulation 時同一個 tick 是否被重複判定
        private struct DebugTickRecord
        {
            public int _tick;
            public bool _isStage; //false = resimulate（重跑過去的 tick）
            public bool _hasValue;
        }

        private const int DebugTickHistoryCapacity = 16;

        private readonly DebugTickRecord[] _debugTickHistory = new DebugTickRecord[
            DebugTickHistoryCapacity
        ];

        private int _debugTickWriteIndex;
        private static readonly System.Text.StringBuilder DebugTickStringBuilder = new(128);

        private void RecordDebugTick(MonoStateBehaviour fromState)
        {
            var tickProvider = fromState?.Machine?.TickProvider;
            _debugTickHistory[_debugTickWriteIndex] = new DebugTickRecord
            {
                _tick = tickProvider?.Tick ?? WorldUpdateSimulator.CurrentTick,
                _isStage = tickProvider?.IsStage ?? true,
                _hasValue = true,
            };
            _debugTickWriteIndex = (_debugTickWriteIndex + 1) % DebugTickHistoryCapacity;
        }

        [ShowInInspector]
        [PropertyOrder(100)]
        [DisplayAsString(false)]
        [LabelText("Transition Ticks (新→舊)")]
        private string DebugTickHistoryText
        {
            get
            {
                DebugTickStringBuilder.Clear();
                for (var i = 0; i < DebugTickHistoryCapacity; i++)
                {
                    //從最後寫入的位置往回讀，最新的排前面
                    var index =
                        (_debugTickWriteIndex - 1 - i + DebugTickHistoryCapacity)
                        % DebugTickHistoryCapacity;
                    var record = _debugTickHistory[index];
                    if (record._hasValue == false)
                        continue;
                    if (DebugTickStringBuilder.Length > 0)
                        DebugTickStringBuilder.Append(", ");
                    DebugTickStringBuilder.Append(record._tick);
                    DebugTickStringBuilder.Append(record._isStage ? "" : "(resim)");
                }

                return DebugTickStringBuilder.Length == 0
                    ? "(尚未觸發)"
                    : DebugTickStringBuilder.ToString();
            }
        }

        // public Color BackgroundColor => new(1.0f, 0f, 0f, 0.3f);
        public string IconName => "CollabMoved Icon";
        public bool IsDrawingIcon => true;

        public Texture2D CustomIcon => null;
        // UnityEditor.EditorGUIUtility.ObjectContent(null, typeof(StateTransition)).image as Texture2D;

#endif
        // public bool IsDrawGUIHierarchyBackground => HasError(); //還是用icon?


        // private bool HasError()
        // {
        //     if (_target == null)
        //     {
        //         _errorMessage = "No Target State";
        //         return true;
        //     }
        //
        //     //FIXME: cache判定？貴一點要GetComponent...什麼時候refresh? auto找不到的有點麻煩...non serialized...
        //     // if (NoChecker)
        //     // {
        //     //     _errorMessage = "No Checker Invoker in Parent or Children";
        //     //     return true;
        //     // }
        //
        //     _errorMessage = "Pass!";
        //     return false;
        // }
    }

    //這層才算是換掉的實作？上面是介面 serialized field就是一種介面的參數，如果放在最外層，名字一樣就可以直接抽換了
    public abstract class TransitionBehaviour<TState> : AbstractDescriptionBehaviour
        where TState : AbstractStateBehaviour<TState>
    {
        public TransitionData<TState> _transitionData;

        public TState TargetState => _transitionData.TargetState;
        // public TransitionData<GeneralStateBehaviour> TransitionData => _transitionData;

        // set => _transitionData = value;
    }
}
