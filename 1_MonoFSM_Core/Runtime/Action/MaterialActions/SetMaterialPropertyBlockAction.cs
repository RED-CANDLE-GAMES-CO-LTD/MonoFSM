using MonoFSM.Core.Runtime.Action;
using MonoFSM.Render;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    public class SetMaterialPropertyBlockAction : AbstractStateAction
    {
        public override string Description =>
            $"Set [{_propertyName}] ({_propertyType}) on [{(_rendererCollection != null ? _rendererCollection.name : _renderer != null ? _renderer.name : "null")}]";

        public enum PropertyType
        {
            Float,
            Color,
            Int
        }

        [SerializeField] [DropDownRef]
        private Renderer _renderer;

        [SerializeField] [DropDownRef]
        private RendererCollection _rendererCollection;

        [SerializeField] private string _propertyName;

        [SerializeField] private int _materialIndex;

        [SerializeField] private PropertyType _propertyType;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Float)]
        private VarFloatWrapper _floatValue;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Color)]
        private Color _colorValue = Color.white;

        [SerializeField] [ShowIf(nameof(_propertyType), PropertyType.Int)]
        private VarIntWrapper _intValue;

        private MaterialPropertyBlock _mpb;
        private int _propertyId;
        

        [Button("Preview")]
        protected override void OnActionExecuteImplement()
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
            else if (_renderer != null)
            {
                ApplyPropertyBlock(_renderer);
            }
            else
            {
                Debug.LogWarning("SetMaterialPropertyBlockAction: No Renderer or RendererCollection assigned", this);
            }
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
            }

            renderer.SetPropertyBlock(_mpb, _materialIndex);
        }
    }
}
