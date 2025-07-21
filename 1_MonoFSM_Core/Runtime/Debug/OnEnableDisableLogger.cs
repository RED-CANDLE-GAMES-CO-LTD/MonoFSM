using System;
using MonoFSM.Core.Attributes;
using UnityEngine;

namespace MonoDebugSetting
{
    //這個看不出來的一定是animation控的？不能開關就是了
    public class OnEnableDisableLogger : MonoBehaviour, IEditorOnly
    {
        private void OnEnable()
        {
            // Debug.Log("OnEnable", this);
        }

        private void OnDisable()
        {
            // Debug.Log("OnDisable", this);
        }
    }
}