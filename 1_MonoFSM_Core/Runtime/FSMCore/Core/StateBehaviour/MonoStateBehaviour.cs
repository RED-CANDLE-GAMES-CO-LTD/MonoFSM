using Fusion.Addons.FSM;
using MonoFSM.Core;
using RCGExtension;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    //不需要另外拆分network相關的行為, 由上層MonoStateMachineController處理
    public class MonoStateBehaviour : AbstractStateBehaviour<MonoStateBehaviour>, IDrawHierarchyBackGround, IDrawDetail
    {
        public Color BackgroundColor => HierarchyResource.CurrentStateColor;
        public bool IsFullRect => false;

        public bool IsDrawGUIHierarchyBackground =>
            Application.isPlaying && _context && _context.IsCurrentState(this);
    }
}