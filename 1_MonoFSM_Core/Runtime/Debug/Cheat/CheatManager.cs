#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;

namespace MonoFSM.Core
{
    [Obsolete("CheatAction就好？")]
    public class CheatManager : MonoBehaviour, IEditorOnly
    {
        // [SerializeField] private AbstractStateAction _action9WasPressed;
        // [SerializeField] private AbstractStateAction _action9WasReleased;
        //FIXME: 用action + condition的方式來做?
    }
}