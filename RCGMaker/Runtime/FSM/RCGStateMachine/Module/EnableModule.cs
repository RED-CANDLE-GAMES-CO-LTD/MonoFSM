using UnityEngine;

namespace RCGMaker.Core.Module
{
    public class EnableModule : MonoBehaviour
    {
        public bool IsValid => gameObject.activeSelf;
    }
}