using MonoFSM.Core.Runtime.Action;
using UnityEngine;


namespace Fusion.Addons.KCC._0_MonoFSM_Network.Action
{
    public class SetCursorLockStateAction : AbstractStateAction
    {
        public bool _isLocked;

        protected override void OnActionExecuteImplement()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log($"SetCursor None");
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Debug.Log($"SetCursor Lock");
            }

            // if (!_isLocked)
            // {
            //     Cursor.lockState = CursorLockMode.None;
            //     Cursor.visible = true;
            // }
            // else
            // {
            //     Cursor.lockState = CursorLockMode.Locked;
            //     Cursor.visible = false;
            // }
        }
    }
}
