using UnityEngine;

namespace RCGMaker.Core
{
    public static class MonoLifeTimeExtension
    {
        public static void ReParent(this MonoBehaviour mono, Transform parent)
        {
            mono.Log("ReParent to" + parent);
            mono.transform.SetParent(parent);
        }

        public static void SetActive(this MonoBehaviour mono, bool active)
        {
            mono.Log("SetActive" + active);
            mono.gameObject.SetActive(active);
        }
    }
}