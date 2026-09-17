using System.Text;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.FSM;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM.Core
{
    /// <summary>
    ///  Condition that checks if a specific key is pressed.
    ///  「這一幀按下」走 <see cref="CheatKeyLatch"/>：input 在 dynamic update 收，但判定多半跑在
    ///  Simulate（FixedUpdate/FixedUpdateNetwork），直接查 wasPressedThisFrame 會漏按。
    ///  自己會登錄進 <see cref="CheatRegistry"/>，讓 Command Palette 列得出「這顆鍵在哪個 state 做什麼」；
    ///  Palette 觸發時走 <see cref="CheatKeyLatch.Inject"/>，跟真的按鍵共用同一條消費路徑。
    ///  FIXME: 應該要 Debug mode才？
    /// </summary>
    public class WasKeyPressCheatCondition : AbstractConditionBehaviour, ISceneAwake //FIXME: parent的模組需要拔掉的話怎麼辦？
    {
        public override string Description =>
            _isPress ? $"Is Key Pressed: {_key}" : $"Was Key Pressed: {_key}";

        [SerializeField]
        private Key _key;

        [SerializeField]
        [Tooltip("勾選：持續按住 (isPressed)；不勾：這一幀按下 (wasPressedThisFrame)")]
        private bool _isPress;

        [SerializeField]
        [Tooltip("勾選：若有 Ctrl/Alt/Shift/Cmd 任一 modifier 鍵被按住，就不觸發（避免組合鍵誤觸）")]
        private bool _ignoreIfModifierHeld;

        private bool IsModifierHeld =>
            Keyboard.current.ctrlKey.isPressed
            || Keyboard.current.altKey.isPressed
            || Keyboard.current.shiftKey.isPressed
            || Keyboard.current.leftMetaKey.isPressed
            || Keyboard.current.rightMetaKey.isPressed;

        protected override bool IsValid =>
            _key > 0
            && Keyboard.current != null
            && !(_ignoreIfModifierHeld && IsModifierHeld)
            && (_isPress ? Keyboard.current[_key].isPressed : CheatKeyLatch.WasPressed(_key));

        private CheatEntry _cheatEntry;

        //關著的 state 子樹也要列得出來，所以走 ISceneAwake（MonoObj 會對整棵子樹分派，含 inactive）；
        //沒有 MonoObj 的場合退回 OnEnable，兩邊都是 idempotent
        public void EnterSceneAwake()
        {
            EnsureRegistered();
        }

        private void OnEnable()
        {
            EnsureRegistered();
        }

        private void OnDestroy()
        {
            CheatRegistry.Unregister(_cheatEntry);
            _cheatEntry = null;
        }

        private void EnsureRegistered()
        {
            if (_cheatEntry != null || _key <= 0)
                return;

            CacheCheatDescription();

            //按住型只顯示不觸發；一次性的走 latch inject，跟真的按鍵同一條路
            _cheatEntry = CheatRegistry.Register(
                _key,
                CheatModifier.None,
                _cheatFallbackDescription,
                BuildHierarchyPath(),
                _isPress ? null : InjectKey,
                _isPress,
                _ignoreIfModifierHeld
                    ? CheatModifier.Ctrl | CheatModifier.Shift | CheatModifier.Alt
                    : CheatModifier.None,
                descriptionGetter: GetCheatDescription,
                isSharedKey: true,
                owner: gameObject);
        }

        private void InjectKey()
        {
            CheatKeyLatch.Inject(_key);
        }

        //condition 本身只說「按了哪顆鍵」，看不出用途；描述優先用人寫的 _note，
        //沒寫才退回「[State 名] 同一個 state 底下第一個 action 的描述」
        private string _cheatStateName;
        private string _cheatFallbackDescription;

        private void CacheCheatDescription()
        {
            var state = GetComponentInParent<IMonoState>(true);
            _cheatStateName = state != null
                ? state.Name
                : transform.parent != null
                    ? transform.parent.name
                    : name;

            var searchRoot = state as Component;
            var action = searchRoot != null
                ? searchRoot.GetComponentInChildren<AbstractStateAction>(true)
                : GetComponentInChildren<AbstractStateAction>(true);
            var detail = action != null ? action.Description : Description;
            _cheatFallbackDescription = $"[{_cheatStateName}] {detail}";
        }

        //note 在 Inspector 上隨時可改，所以走 descriptionGetter 動態取（只有 Inspector / Palette 會呼叫）
        private string GetCheatDescription()
        {
            var note = Note;
            return string.IsNullOrEmpty(note) ? _cheatFallbackDescription : $"[{_cheatStateName}] {note}";
        }

        //只在登錄時算一次
        private string BuildHierarchyPath()
        {
            var builder = new StringBuilder(64);
            var current = transform;
            while (current != null)
            {
                if (builder.Length > 0)
                    builder.Insert(0, '/');
                builder.Insert(0, current.name);
                current = current.parent;
            }

            return builder.ToString();
        }
    }
}
