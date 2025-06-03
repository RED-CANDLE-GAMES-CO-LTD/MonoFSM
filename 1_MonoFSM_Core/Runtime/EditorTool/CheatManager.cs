using UnityEditor;
using UnityEngine;

namespace RCGMaker.Core
{
    public class CheatManager : MonoBehaviour, IEditorOnly
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) EditorApplication.isPaused = !EditorApplication.isPaused;
        }
    }
}