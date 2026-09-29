using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Render;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonoFSM.ParticleSystemActions
{
    /// <summary>
    /// 用 MaterialPropertyBlock 覆寫 renderer 某個 material slot 的一顆 shader property，不會把 material instance 化。
    /// 掛在 RenderLoop 下；`_restoreSharedMaterial` 為 true 時改成清掉該 slot 的 block，讓 renderer 讀回 shared material
    /// （Play 中直接調 material asset 就看得到）。例：燈泡關燈 = `_EmissionColor` 覆寫成黑、亮著時 restore。
    /// `_multiplier` 綁 VarFloat 時（只對 Float / Color 有效），輸出 = 基準值 × 倍率，基準值是覆寫值或 shared material 的值；
    /// 例：LightStyleFlicker 把閃爍倍率寫進 VarFloat，這顆 action 仍是最後寫 property 的人，關燈照樣全黑。
    /// </summary>
    public class SetMaterialPropertyBlockAction : AbstractRenderBehaviour
    {
        public override string Description =>
            $"MPB [{_propertyName}] = {ValueText}{(HasMultiplier ? $" × {_multiplier.Description}" : "")} on [{TargetName}]{(_restoreSharedMaterial._var == null && _restoreSharedMaterial.Value == false ? "" : $" (restore when {_restoreSharedMaterial.Description})")}";

        private string TargetName => _rendererCollection != null ? _rendererCollection.name :
            _renderer != null ? _renderer.name : "null";

        private string ValueText => _propertyType switch
        {
            PropertyType.Float => _floatValue.Description,
            PropertyType.Color => $"#{ColorUtility.ToHtmlStringRGBA(_colorValue)}",
            PropertyType.Int => _intValue.Description,
            PropertyType.Bool => _boolValue.Description,
            _ => "?"
        };

        public enum PropertyType
        {
            Float,
            Color,
            Int,
            Bool
        }

        [HideIf(nameof(_rendererCollection))]
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

        [Tooltip("true = 清掉此 material slot 的 property block，renderer 讀回 shared material（Play 中調 asset 即時可見）；" +
                 "false = 照常用 block 覆寫。注意 restore 會把同一 slot 上其他 Action 設的 block 值一起清掉")]
        [SerializeField]
        private VarBoolWrapper _restoreSharedMaterial = new(false);

        [Tooltip("Float / Color 才有效。綁 VarFloat 或填 1 以外的值時，輸出 = 基準值 × 倍率（restore 狀態的基準值 = shared material 的值），" +
                 "restore 時改成每幀寫 block、不再清掉")]
        [SerializeField] [ShowIf(nameof(IsMultipliable))]
        private VarFloatWrapper _multiplier = new(1f);

        private bool IsMultipliable => _propertyType is PropertyType.Float or PropertyType.Color;

        private bool HasMultiplier => IsMultipliable && _multiplier != null &&
                                      (_multiplier._var != null || !Mathf.Approximately(_multiplier.Value, 1f));

        private static readonly List<Material> s_sharedMaterials = new();

        private MaterialPropertyBlock _mpb;
        private int _propertyId;

        //上一幀是否處於 restore 狀態；null = 剛進狀態，強制套用一次
        private bool? _lastRestored;

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
            _lastRestored = null;
            Apply();
        }

        public override void OnRenderImplement()
        {
            Apply();
        }

        private void Apply()
        {
            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
                _propertyId = Shader.PropertyToID(_propertyName);
            }

            var restore = _restoreSharedMaterial.Value;
            var hasMultiplier = HasMultiplier;
            //restore 是一次性的清除，維持在 restore 狀態時不用每幀重清；覆寫值可能綁 Var，每幀重套
            //有倍率時 restore 也要每幀寫（shared 值 × 倍率），所以不算進「已清過」
            var clearOnly = restore && !hasMultiplier;
            if (clearOnly && _lastRestored == true)
                return;
            _lastRestored = clearOnly;
            var multiplier = hasMultiplier ? _multiplier.Value : 1f;

            if (_rendererCollection != null)
            {
                var renderers = _rendererCollection.Renderers;
                if (renderers == null) return;

                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    ApplyTo(r, restore, clearOnly, multiplier);
                }
            }

            if (_renderer != null)
                ApplyTo(_renderer, restore, clearOnly, multiplier);
        }

        private void ApplyTo(Renderer renderer, bool restore, bool clearOnly, float multiplier)
        {
            if (clearOnly)
                renderer.SetPropertyBlock(null, _materialIndex);
            else
                ApplyPropertyBlock(renderer, restore, multiplier);
        }

        private void ApplyPropertyBlock(Renderer renderer, bool fromShared, float multiplier)
        {
            //fromShared：restore 狀態 + 有倍率，基準值讀 shared material（Play 中調 asset 一樣看得到）
            Material shared = null;
            if (fromShared)
            {
                renderer.GetSharedMaterials(s_sharedMaterials);
                if (_materialIndex < s_sharedMaterials.Count)
                    shared = s_sharedMaterials[_materialIndex];
                s_sharedMaterials.Clear();
                if (shared == null || !shared.HasProperty(_propertyId))
                {
                    renderer.SetPropertyBlock(null, _materialIndex);
                    return;
                }
            }

            renderer.GetPropertyBlock(_mpb, _materialIndex);

            switch (_propertyType)
            {
                case PropertyType.Float:
                    _mpb.SetFloat(_propertyId,
                        (fromShared ? shared.GetFloat(_propertyId) : _floatValue.Value) * multiplier);
                    break;
                case PropertyType.Color:
                    //alpha 不乘，只調亮度
                    var c = fromShared ? shared.GetColor(_propertyId) : _colorValue;
                    _mpb.SetColor(_propertyId, new Color(c.r * multiplier, c.g * multiplier, c.b * multiplier, c.a));
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
