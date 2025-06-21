#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MonoFSM.Core
{
    public class CheatManager : MonoBehaviour, IEditorOnly
    {
        // [SerializeField] private AbstractStateAction _action9WasPressed;
        // [SerializeField] private AbstractStateAction _action9WasReleased;
        //FIXME: 用action + condition的方式來做?
        private void Update()
        {
            //內建就有了
            // if (Input.GetKeyDown(KeyCode.P)) EditorApplication.isPaused = !EditorApplication.isPaused;
        }
    }
}