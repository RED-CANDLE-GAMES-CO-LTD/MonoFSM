using System;
using UnityEngine;

namespace RCGSetting
{
    public class OnEnableDisableLogger : MonoBehaviour
    {
        private void OnEnable()
        {
            Debug.Log("OnEnable", this);
        }

        private void OnDisable()
        {
            Debug.Log("OnDisable", this);
        }
    }
}