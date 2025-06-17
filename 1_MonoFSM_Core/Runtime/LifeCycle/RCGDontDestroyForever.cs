using System;
using UnityEngine;

namespace MonoFSM.Runtime.LifeCycle
{
    public class RCGDontDestroyForever : MonoBehaviour
    {
        private void Awake()
        {
            //重複的時候要把另一個殺掉？
            RCGLifeCycle.DontDestroyForever(gameObject);
        }
    }
}