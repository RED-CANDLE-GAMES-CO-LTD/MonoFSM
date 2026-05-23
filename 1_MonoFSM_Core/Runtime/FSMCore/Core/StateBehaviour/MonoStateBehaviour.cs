using Fusion.Addons.FSM;
using MonoFSM.Core;
using MonoFSM.EditorExtension;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    //不需要另外拆分network相關的行為, 由上層MonoStateMachineController處理
    public class MonoStateBehaviour : AbstractStateBehaviour<MonoStateBehaviour>, IDrawDetail,
        IValueOfKey<string>
    {
        public override Color BackgroundColor => Application.isPlaying
            ? HierarchyResource.CurrentStateColor
            : base.BackgroundColor;
        public bool IsFullRect => false;

        public override bool IsDrawGUIHierarchyBackground =>
            Application.isPlaying
                ? (Owner && Owner.IsCurrentState(this))
                : base.IsDrawGUIHierarchyBackground;

        public string Key => Name;

        public bool TryActivateState() //這會很越權嗎？
        {
            return Machine.TryActivateState(this);
        }
    }
}
