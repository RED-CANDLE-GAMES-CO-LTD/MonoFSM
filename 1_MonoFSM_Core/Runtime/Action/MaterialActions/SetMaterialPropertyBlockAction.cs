using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Render;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonoFSM.ParticleSystemActions
{
    public class SetMaterialPropertyBlockAction : AbstractRenderBehaviour
    {
        public override string Description =>
            $"Set [{_propertyName}] ({_propertyType}) on [{(_rendererCollection != null ? _rendererCollection.name : _renderer != null ? _renderer.name : "null")}]";

        public enum PropertyType
        {
            Float,
            Color,
            Int,
            Bool
        }

        [SerializeField] [DropDownRef]
        private Renderer _renderer;

        [HideIf(nameof(_renderer))]
        [SerializeField] [DropDownRef]
        private RendererCollection _rendererCollection;

#if UNITY_EDITOR
        [ValueDropdown(nameof(GetPropertyNames))]
#endif
        [SerializeField]
        private string _propertyName;

        [SerializeField] private int _materialIndex;

        [SerializeField] private PropertyType _propertyType;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Float)]
        private VarFloatWrapper _floatValue;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Color)]
        private Color _colorValue = Color.white;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Int)]
        private VarIntWrapper _intValue;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Bool)]
        private VarBoolWrapper _boolValue;

        private MaterialPropertyBlock _mpb;
        private int _propertyId;

#if UNITY_EDITOR
        private IEnumerable<ValueDropdownItem<string>> GetPropertyNames()
        {
            var shader = GetShaderFromRenderer();
            if (shader == null) yield break;

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

        private Shader GetShaderFromRenderer()
        {
            Renderer r = _renderer;
            if (r == null && _rendererCollection != null)
                r = _rendererCollection.GetComponentInChildren<Renderer>();
            if (r == null) return null;

            var mat = _materialIndex < r.sharedMaterials.Length
                ? r.sharedMaterials[_materialIndex]
                : null;
            return mat != null ? mat.shader : null;
        }
#endif

        [Button("Preview")]
        public override void OnEnterRenderImplement()
        {
            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
                _propertyId = Shader.PropertyToID(_propertyName);
            }

            if (_rendererCollection != null)
            {
                var renderers = _rendererCollection.Renderers;
                if (renderers == null) return;

                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    ApplyPropertyBlock(r);
                }
            }

            if (_renderer != null)
            {
                ApplyPropertyBlock(_renderer);
            }
            else
            {
                Debug.LogWarning("SetMaterialPropertyBlockAction: No Renderer or RendererCollection assigned", this);
            }
        }

        public override void OnRenderImplement()
        {
            OnEnterRenderImplement();
        }

        private void ApplyPropertyBlock(Renderer renderer)
        {
            renderer.GetPropertyBlock(_mpb, _materialIndex);

            switch (_propertyType)
            {
                case PropertyType.Float:
                    _mpb.SetFloat(_propertyId, _floatValue.Value);
                    break;
                case PropertyType.Color:
                    _mpb.SetColor(_propertyId, _colorValue);
                    break;
                case PropertyType.Int:
                    _mpb.SetInt(_propertyId, _intValue.Value);
                    break;
                case PropertyType.Bool:
                    _mpb.SetFloat(_propertyId, _boolValue.Value ? 1f : 0f);
                    break;
            }

            renderer.SetPropertyBlock(_mpb, _materialIndex);
        }
    }
}
