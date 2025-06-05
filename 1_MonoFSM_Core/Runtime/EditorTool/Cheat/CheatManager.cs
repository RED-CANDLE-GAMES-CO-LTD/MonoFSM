using MonoFSM_Core.Runtime.Action;
using UnityEditor;
using UnityEngine;

namespace RCGMaker.Core
{
    public class CheatManager : MonoBehaviour, IEditorOnly
    {
        // [SerializeField] private AbstractStateAction _action9WasPressed;
        // [SerializeField] private AbstractStateAction _action9WasReleased;
        //FIXME: 用action + condition的方式來做?
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) EditorApplication.isPaused = !EditorApplication.isPaused;
        }
    }
}