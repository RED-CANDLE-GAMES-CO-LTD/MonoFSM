using UnityEngine;

namespace InputExtension
{
    public static class MonoInput
    {
        private static bool isCursorVisible
        {
            get => Cursor.visible; //可能不同平台的實作不同唷
            set => Cursor.visible = value;
        }
        public static void SetCursorVisible(bool visible)
        {
#if RCG_DEV && UNITY_STANDALONE
             return;
#endif
            
            //Editor模式下不要改變Cursor.visible

            if (visible == isCursorVisible) return;
#if UNITY_EDITOR
            Debug.Log("SetCursorVisible" + visible);
#endif
            isCursorVisible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Confined;

        }

        public static bool IsCursorVisible => isCursorVisible;
        
    }
}