using UnityEngine;

namespace MonoDebugSetting
{
    //強迫改condition的值
    public class DebugConditionResultOverrider : MonoBehaviour
    {
        [Header("Debug Mode時，會強迫改condition的值")]
        public bool OverrideResultValue = true;
    }
}