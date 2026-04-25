using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonoFSM.ParticleSystemActions
{
    /// <summary>
    /// 掛在 GameObject 上，讓 Animation 可以 key Material 屬性值
    /// 欄位使用原生型別（float / Color）以便被 AnimationClip 錄製
    /// 在 LateUpdate 套用至 Material（動畫更新後）
    /// 注意：會修改到 Material 資產本身（若為 shared material）
    /// </summary>
    [ExecuteAlways]
    public class MaterialPropertyDriver : MonoBehaviour
    {
        public enum PropertyType
        {
            Float,
            Color,
            Int,
            Bool
        }

        [SerializeField] [Required]
        private Material _material;

#if UNITY_EDITOR
        [ValueDropdown(nameof(GetPropertyNames))]
#endif
        [SerializeField]
        private string _propertyName;

        [SerializeField] private PropertyType _propertyType;

        [ShowIf(nameof(_propertyType), PropertyType.Float)]
        public float _floatValue;

        [ShowIf(nameof(_propertyType), PropertyType.Color)]
        public Color _colorValue = Color.white;

        [ShowIf(nameof(_propertyType), PropertyType.Int)]
        public float _intValue;

        [ShowIf(nameof(_propertyType), PropertyType.Bool)]
        public float _boolValue;

        private int _propertyId = -1;
        private string _cachedPropertyName;

        private void LateUpdate()
        {
            Apply();
        }

        [Button("Apply Now")]
        private void Apply()
        {
            if (_material == null || string.IsNullOrEmpty(_propertyName)) return;

            if (_propertyId == -1 || _cachedPropertyName != _propertyName)
            {
                _propertyId = Shader.PropertyToID(_propertyName);
                _cachedPropertyName = _propertyName;
            }

            switch (_propertyType)
            {
                case PropertyType.Float:
                    _material.SetFloat(_propertyId, _floatValue);
                    break;
                case PropertyType.Color:
                    _material.SetColor(_propertyId, _colorValue);
                    break;
                case PropertyType.Int:
                    _material.SetInt(_propertyId, Mathf.RoundToInt(_intValue));
                    break;
                case PropertyType.Bool:
                    _material.SetFloat(_propertyId, _boolValue >= 0.5f ? 1f : 0f);
                    break;
            }
        }

#if UNITY_EDITOR
        private IEnumerable<ValueDropdownItem<string>> GetPropertyNames()
        {
            if (_material == null || _material.shader == null) yield break;
            var shader = _material.shader;

            var targetType = _propertyType switch
            {
                PropertyType.Float => ShaderPropertyType.Float,
                PropertyType.Color => ShaderPropertyType.Color,
                PropertyType.Int => ShaderPropertyType.Int,
                PropertyType.Bool => ShaderPropertyType.Float,
                _ => ShaderPropertyType.Float
            };

            var count = shader.GetPropertyCount();
            for (var i = 0; i < count; i++)
            {
                var type = shader.GetPropertyType(i);
                if (type != targetType && !(targetType == ShaderPropertyType.Float &&
                                            type == ShaderPropertyType.Range))
                    continue;

                var propName = shader.GetPropertyName(i);
                var desc = shader.GetPropertyDescription(i);
                var label = string.IsNullOrEmpty(desc) ? propName : $"{desc} ({propName})";
                yield return new ValueDropdownItem<string>(label, propName);
            }
        }
#endif
    }
}
