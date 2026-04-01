using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    public class SetMaterialPropertyBlockAction : AbstractStateAction
    {
        public override string Description =>
            $"Set [{_propertyName}] ({_propertyType}) on [{(_renderer != null ? _renderer.name : "null")}]";

        public enum PropertyType
        {
            Float,
            Color,
            Int
        }

        [SerializeField] [DropDownRef] [Required]
        private Renderer _renderer;

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

        protected override void OnActionExecuteImplement()
        {
            if (_renderer == null)
            {
                Debug.LogWarning("SetMaterialPropertyBlockAction: Renderer is null", this);
                return;
            }

            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
                _propertyId = Shader.PropertyToID(_propertyName);
            }

            _renderer.GetPropertyBlock(_mpb, _materialIndex);

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

            _renderer.SetPropertyBlock(_mpb, _materialIndex);
        }
    }
}