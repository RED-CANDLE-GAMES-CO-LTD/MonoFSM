using MonoFSM.Core.Runtime.Action;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fusion.Addons.KCC._0_MonoFSM_Network.Action
{
    public class PauseGameAction : AbstractStateAction,IEditorOnly
    {
        protected override void OnActionExecuteImplement()
        {
#if UNITY_EDITOR
            EditorApplication.isPaused = !EditorApplication.isPaused;
#endif
        }
    }
}