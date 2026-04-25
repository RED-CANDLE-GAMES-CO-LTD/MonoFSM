using System.Collections.Generic;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonoFSM.ParticleSystemActions
{
    /// <summary>
    /// 直接 reference Material 設定屬性（不透過 MaterialPropertyBlock）
    /// 注意：會修改到 Material 資產本身（若為 shared material）
    /// </summary>
    public class SetMaterialPropertyAction : AbstractStateAction
    {
        public override string Description =>
            $"Set [{_propertyName}] ({_propertyType}) on Material [{(_material != null ? _material.name : "null")}]";

        public enum PropertyType
        {
            Float,
            Color,
            Int,
            Bool
        }

        [SerializeField] [Required] private Material _material;

#if UNITY_EDITOR
        [ValueDropdown(nameof(GetPropertyNames))]
#endif
        [SerializeField]
        private string _propertyName;

        [SerializeField] private PropertyType _propertyType;

        // [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Float)]
        // private VarFloatWrapper _floatValue;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Float)]
        private float _floatLiteral;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Color)]
        private Color _colorValue = Color.white;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Int)]
        private VarIntWrapper _intValue;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Bool)]
        private VarBoolWrapper _boolValue;

        private int _propertyId = -1;
        private string _cachedPropertyName;

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

        [Button("Preview")]
        protected override void OnActionExecuteImplement()
        {
            if (_material == null)
            {
                Debug.LogWarning("SetMaterialPropertyAction: No Material assigned", this);
                return;
            }

            if (_propertyId == -1 || _cachedPropertyName != _propertyName)
            {
                _propertyId = Shader.PropertyToID(_propertyName);
                _cachedPropertyName = _propertyName;
            }

            switch (_propertyType)
            {
                case PropertyType.Float:
                    _material.SetFloat(_propertyId, _floatLiteral);
                    break;
                case PropertyType.Color:
                    _material.SetColor(_propertyId, _colorValue);
                    break;
                case PropertyType.Int:
                    _material.SetInt(_propertyId, _intValue.Value);
                    break;
                case PropertyType.Bool:
                    _material.SetFloat(_propertyId, _boolValue.Value ? 1f : 0f);
                    break;
            }
        }
    }
}
