using UnityEngine;

namespace MonoFSM.Core.Module
{
    //打開的時候才算有效，現在是直接從上面往下綁，給code判用的...可能只是暫時之計
    public class EnableModule : MonoBehaviour
    {
        public bool IsValid => gameObject.activeSelf;
    }
}