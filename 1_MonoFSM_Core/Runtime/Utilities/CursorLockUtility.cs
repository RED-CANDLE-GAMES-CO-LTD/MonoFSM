using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>統一管理 Cursor.lockState / Cursor.visible，避免各處各自寫一份判斷。</summary>
    public static class CursorLockUtility
    {
        public static void Lock()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;
            if (Cursor.visible)
                Cursor.visible = false;
        }

        public static void Unlock()
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;
        }

        public static void Toggle()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                Unlock();
            else
                Lock();
        }
    }
}
