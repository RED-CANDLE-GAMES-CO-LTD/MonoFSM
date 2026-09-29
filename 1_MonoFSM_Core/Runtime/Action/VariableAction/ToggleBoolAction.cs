using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.VariableAction
{
    /// <summary>
    /// 把 _target 這顆 VarBool 反轉（true↔false）：按一下開、再按一下關的兩段式機關用，
    /// 常掛在 Interact Device Trigger 的 [Event] ManualEvent 或 receiver 的 EffectEnterNode 底下。
    /// </summary>
    public class ToggleBoolAction : AbstractStateAction
    {
        [SerializeField] [DropDownRef] public VarBool _target; //var?

        protected override void OnActionExecuteImplement()
        {
            // Debug.Log($"ToggleBoolAction: Toggling value of {_target}", this);
            _target.SetValue(!_target.Value, this);
        }

        public override string Description =>
            _target != null ? $"Toggle Bool: {_target.name}" : "Toggle Bool: No target set";
    }
}