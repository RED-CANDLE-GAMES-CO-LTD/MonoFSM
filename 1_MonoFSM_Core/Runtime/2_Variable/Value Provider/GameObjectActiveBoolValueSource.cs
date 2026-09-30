using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ValueSource.ValueSource
{
    /// <summary>
    ///     回傳 _target 這個 GameObject 現在是不是 activeInHierarchy（掛在 [Getter] VarBool 底下）。
    ///     用途是把「場景擺放狀態」當初始值：企劃在關卡裡把某個物件整顆設 inactive 代表「這組開場沒有」，
    ///     再由 OnResetStart 之類的事件用 SetVarBool 把這個值寫進一顆 networked VarBool。
    ///     _target 沒指就回 false。
    /// </summary>
    public class GameObjectActiveBoolValueSource : AbstractValueSource<bool>
    {
        [Required]
        [SerializeField]
        private GameObject _target;

        public override bool Value => _target != null && _target.activeInHierarchy;

        public override string Description =>
            _target != null ? $"{_target.name} active?" : "null target active?";
    }
}
