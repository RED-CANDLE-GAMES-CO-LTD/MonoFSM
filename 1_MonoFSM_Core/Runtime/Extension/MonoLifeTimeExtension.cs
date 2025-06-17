using UnityEngine;

namespace MonoFSM.Core
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

        public static void SetDirty(this MonoBehaviour mono)
        {
#if UNITY_EDITOR
            // mono.Log("SetDirty");
            UnityEditor.EditorUtility.SetDirty(mono);
#endif
        }

        //for scriptable
        public static void SafeSetDirty(this ScriptableObject obj)
        {
#if UNITY_EDITOR
            // mono.Log("SetDirty");
            UnityEditor.EditorUtility.SetDirty(obj);
#endif
        }
    }
}