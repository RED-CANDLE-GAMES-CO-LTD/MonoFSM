using UnityEngine;
using Sirenix.OdinInspector;

namespace MonoFSM.Core
{
    /// <summary>
    /// 不綁定任何觸發來源，純粹透過外部引用呼叫 EventHandle() 的 EventHandler。
    /// </summary>
    public class ManualEventHandler : AbstractEventHandler
    {
#if UNITY_EDITOR
        [Button("Find References"), PropertyOrder(-100)]
        private void FindReferences()
        {
            var windowType = System.Type.GetType(
                "MonoFSM.Editor.ReferenceSystem.ComponentReferenceWindow, MonoFSM.Core.Editor");
            if (windowType != null)
            {
                var method = windowType.GetMethod("ShowWindowWithTarget",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                method?.Invoke(null, new object[] { this });
            }
            else
            {
                Debug.LogWarning("ComponentReferenceWindow not found. Please ensure MonoFSM.Core.Editor assembly is loaded.");
            }
        }
#endif
    }
}
