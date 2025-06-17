using UnityEngine;

using Sirenix.OdinInspector;

namespace MonoFSM.Core
{
    //拿來寫註解，MonoNodeWindow會顯示出來
    public class ComponentNote : MonoBehaviour
    {
        //displayed in hierarchy view window
        [TextArea] public string Note;
        [ColorPalette] public Color color;
    }
}