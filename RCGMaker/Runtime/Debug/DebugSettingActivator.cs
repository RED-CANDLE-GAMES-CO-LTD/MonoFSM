using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGSetting
{
    public class DebugSettingActivator : MonoBehaviour
    {
        public GameObject ChildNode;

        private IEnumerable<string> GetAllDebugSettingNames()
        {
            foreach (var property in typeof(DebugSetting).GetProperties())
            {
                if (property.PropertyType != typeof(bool)) continue;
                yield return property.Name;
            }
        }

        [ValueDropdown(nameof(GetAllDebugSettingNames))]
        public string activatePropertyName;

        private PropertyInfo GetActivatePropertyInfo()
        {
            return typeof(DebugSetting).GetProperty(activatePropertyName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private void ActivateCheck()
        {
            var fieldInfo = GetActivatePropertyInfo();
            if (fieldInfo == null)
            {
                Debug.LogError($"DebugSettingActivator: {activatePropertyName} not found");
                return;
            }

            var value = (bool)fieldInfo.GetValue(null);
            if (value != ChildNode.activeSelf)
                ChildNode.SetActive(value);
        }
#if RCG_DEV
        private void Update()
        {
            ActivateCheck();
        }
#endif
    }
}