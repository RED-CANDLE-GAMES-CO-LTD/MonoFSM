using UnityEngine;

namespace RCGExtension
{
    public static class HierarchyDebug
    {
        private static bool _isDebugMode = false;

        public static bool IsDebugMode
        {
            get => _isDebugMode;
            set
            {
                _isDebugMode = value;
                if (_isDebugMode)
                    Debug.Log("Hierarchy Debug Mode Enabled");
                else
                    Debug.Log("Hierarchy Debug Mode Disabled");
            }
        }
    }
}