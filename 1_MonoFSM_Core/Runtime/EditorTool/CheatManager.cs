using MonoFSM_Core.Runtime.Action;
using UnityEditor;
using UnityEngine;

namespace RCGMaker.Core
{
    public class CheatManager : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private AbstractStateAction _action9WasPressed;
        [SerializeField] private AbstractStateAction _action9WasReleased;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) EditorApplication.isPaused = !EditorApplication.isPaused;
            if (Input.GetKeyDown(KeyCode.Alpha0))
                RCGTime.SetTimeScaleUnsafe(5);
            else
                RCGTime.SetTimeScaleUnsafe(1);
        }
    }
}