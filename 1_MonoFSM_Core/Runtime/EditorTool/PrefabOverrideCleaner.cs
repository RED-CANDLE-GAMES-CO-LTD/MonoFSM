using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Core
{
    public class PrefabOverrideCleaner : MonoBehaviour, ISceneSavingCallbackReceiver,
        IBeforePrefabSaveCallbackReceiver
    {
        [OnCollectionChanged(nameof(UpdateComponentLockState))]
        public Component[] _targetComponent;

        public bool _lockComponents = true; // 是否鎖定 components，讓它們在 Inspector 上不可編輯

        private void OnValidate()
        {
#if UNITY_EDITOR
            UpdateComponentLockState();
#endif
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            //進 Play Mode 時鎖定 hideFlags 沒有意義，場景上有近千個實例，省掉 enter play mode 的無謂成本
            if (Application.isPlaying)
                return;
            UpdateComponentLockState();
#endif
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;
            // 清理時解鎖所有 components
            UnlockAllComponents();
#endif
        }

        public void OnBeforeSceneSave()
        {
            RevertAllOverrides();
        }

        public void OnBeforePrefabSave()
        {
            RevertAllOverrides();
        }

        private void UpdateComponentLockState()
        {
#if UNITY_EDITOR
            if (_targetComponent == null || _targetComponent.Length == 0)
                return;

            foreach (var component in _targetComponent)
            {
                if (component == null)
                    continue;

                if (_lockComponents)
                {
                    component.hideFlags = HideFlags.NotEditable;
                }
                else
                {
                    component.hideFlags = HideFlags.None;
                }
            }
#endif
        }

        private void UnlockAllComponents()
        {
#if UNITY_EDITOR
            if (_targetComponent == null || _targetComponent.Length == 0)
                return;

            foreach (var component in _targetComponent)
            {
                if (component == null)
                    continue;

                component.hideFlags = HideFlags.None;
            }
#endif
        }

        [Button]
        private void RevertAllOverrides()
        {
#if UNITY_EDITOR
            if (_targetComponent == null || _targetComponent.Length == 0)
                return;

            foreach (var component in _targetComponent)
            {
                if (component == null)
                    continue;

                // Check if the target component is part of a prefab instance
                if (!PrefabUtility.IsPartOfPrefabInstance(component))
                {
                    Debug.Log(
                        $"Component {component.GetType().Name} is not part of a prefab instance. Skipping override revert.");
                    continue;
                }


                // Get the serialized object and iterate through all properties
                var serObj = new SerializedObject(component);
                var prop = serObj.GetIterator();
                while (prop.NextVisible(true))
                {
                    // Revert each property override
                    // Debug.Log(
                    //     $"Reverting override for property {prop.name} in component {component.GetType().Name}");
                    PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
                }
            }
#endif
        }
    }
}
