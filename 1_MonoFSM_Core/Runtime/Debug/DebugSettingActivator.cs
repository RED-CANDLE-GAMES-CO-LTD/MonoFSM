using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoDebugSetting
{
    /// <summary>
    /// 應該用ConditionActivator配合IsDebugModeCondition?
    /// </summary>
    [Obsolete]
    public class DebugSettingActivator : MonoBehaviour, IResetter
    {
        public GameObject ChildNode;

        private void Start()
        {
            ChildNode.SetActive(false);
        }

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

        private Func<bool> GetActivatePropertyInfo()
        {
            if (_getActivateProperty == null)
            {
                var propertyInfo = typeof(DebugSetting).GetProperty(activatePropertyName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo == null)
                {
                    Debug.LogError($"DebugSettingActivator: {activatePropertyName} not found");
                    return null;
                }

                _getActivateProperty =
                    (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), propertyInfo.GetGetMethod());
            }

            return _getActivateProperty;
            // if (_cachedInfo == null)
            // {
            //     _cachedInfo=  typeof(DebugSetting).GetProperty(activatePropertyName,
            //         BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            // }
            //
            // return _cachedInfo;
        }

        private Func<bool> _getActivateProperty;
        private PropertyInfo _cachedInfo = null;

        private void ActivateCheck()
        {
            var getActivateValue = GetActivatePropertyInfo();
            if (getActivateValue == null)
            {
                Debug.LogError($"DebugSettingActivator: {activatePropertyName} not found");
                return;
            }


            var value = getActivateValue();
            if (value != ChildNode.activeSelf)
                ChildNode.SetActive(value);
        }
#if RCG_DEV
        private void Update()
        {
            ActivateCheck();
        }
#endif
        public void EnterLevelReset()
        {
            ChildNode.SetActive(false);
        }

        public void ExitLevelAndDestroy()
        {
            ChildNode.SetActive(false);
        }
    }
}