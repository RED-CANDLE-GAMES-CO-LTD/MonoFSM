using MonoFSM.Core;
using MonoFSM.Core.Runtime.Action;
using UnityEngine;


namespace Fusion.Addons.KCC._0_MonoFSM_Network.Action
{
    public class SetCursorLockStateAction : AbstractStateAction
    {
        public bool _isLocked;

        protected override void OnActionExecuteImplement()
        {
            CursorLockUtility.Toggle();
            Debug.Log($"SetCursor {Cursor.lockState}");
        }
    }
}
