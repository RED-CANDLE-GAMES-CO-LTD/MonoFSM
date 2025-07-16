using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Formula
{
    public class FilterByComponentProvider : MonoBehaviour, IMonoDescriptableListProvider
    {
        [SerializeField] [Required]
        private Component _inputProviderComponent;
        private IMonoDescriptableListProvider _inputProvider;

        [SerializeField] [Tooltip("The full name of the component to filter by, e.g., 'UnityEngine.BoxCollider' or 'MyGame.Enemy'")]
        private string _requiredComponentTypeName;

        private void Awake()
        {
            _inputProvider = _inputProviderComponent as IMonoDescriptableListProvider;
            if (_inputProvider == null)
            {
                Debug.LogError($"The provided component on {gameObject.name} does not implement IMonoDescriptableListProvider.", this);
            }
        }

        public IEnumerable<MonoEntity> GetDescriptables()
        {
            if (_inputProvider == null || string.IsNullOrEmpty(_requiredComponentTypeName))
            {
                return Enumerable.Empty<MonoEntity>();
            }

            Type componentType = Type.GetType(_requiredComponentTypeName);
            if (componentType == null)
            {
                Debug.LogError($"Component type '{_requiredComponentTypeName}' not found.", this);
                return Enumerable.Empty<MonoEntity>();
            }

            return _inputProvider.GetDescriptables().Where(md => md != null && md.GetComponent(componentType) != null);
        }
    }
}
