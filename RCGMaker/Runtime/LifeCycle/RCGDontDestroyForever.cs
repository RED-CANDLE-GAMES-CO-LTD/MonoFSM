using System;
using UnityEngine;

namespace RCGMaker.Runtime.LifeCycle
{
    public class RCGDontDestroyForever : MonoBehaviour
    {
        private void Awake()
        {
            RCGLifeCycle.DontDestroyForever(gameObject);
        }
    }
}