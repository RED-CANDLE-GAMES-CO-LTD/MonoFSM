using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Render;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    public class EnableKeywordAction : AbstractRenderBehaviour
    {
        public override string Description =>
            $"{(_enable.Value ? "Enable" : "Disable")} keyword [{_keyword}] on [{(_rendererCollection != null ? _rendererCollection.name : _renderer != null ? _renderer.name : "null")}]";

        [SerializeField] [DropDownRef] private Renderer _renderer;

        [HideIf(nameof(_renderer))] [SerializeField] [DropDownRef]
        private RendererCollection _rendererCollection;

#if UNITY_EDITOR
        [ValueDropdown(nameof(GetKeywords))]
#endif
        [SerializeField]
        private string _keyword;

        [SerializeField] private int _materialIndex;

        [Tooltip("true = EnableKeyword, false = DisableKeyword")] [SerializeField]
        private VarBoolWrapper _enable = new(true);

#if UNITY_EDITOR
        private IEnumerable<ValueDropdownItem<string>> GetKeywords()
        {
            var shader = GetShaderFromRenderer();
            if (shader == null) yield break;

            var count = shader.keywordSpace.keywordCount;
            var keywords = shader.keywordSpace.keywords;
            for (var i = 0; i < count; i++)
            {
                var name = keywords[i].name;
                yield return new ValueDropdownItem<string>(name, name);
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

        //單一 _renderer 的 material instance 陣列 cache，避免每幀 renderer.materials 的 alloc 與重複 instancing
        private Material[] _rendererMaterials;

        //上次實際套用到 material 的 keyword 狀態；null = 尚未套用（進入狀態時重置以強制重套）
        private bool? _lastEnabled;

        [Button("Preview")]
        public override void OnEnterRenderImplement()
        {
            //進入狀態時強制重套一次（material instance 的 keyword 可能已被其他 state 改動）
            _lastEnabled = null;
            ApplyIfChanged();
        }

        public override void OnRenderImplement()
        {
            ApplyIfChanged();
        }

        private void ApplyIfChanged()
        {
            var enable = _enable.Value;
            if (_lastEnabled == enable)
                return;
            _lastEnabled = enable;

            if (_rendererCollection != null)
            {
                var cached = _rendererCollection.CachedMaterials;
                if (cached != null)
                {
                    foreach (var materials in cached)
                        ApplyKeyword(materials, enable);
                }
            }

            if (_renderer != null)
            {
                _rendererMaterials ??= _renderer.materials;
                ApplyKeyword(_rendererMaterials, enable);
            }
            else if (_rendererCollection == null)
            {
                Debug.LogWarning("EnableKeywordAction: No Renderer or RendererCollection assigned",
                    this);
            }
        }

        private void ApplyKeyword(Material[] materials, bool enable)
        {
            if (materials == null)
                return;
            if (_materialIndex < 0 || _materialIndex >= materials.Length)
            {
                Debug.LogWarning(
                    $"EnableKeywordAction: materialIndex {_materialIndex} out of range",
                    this);
                return;
            }

            var mat = materials[_materialIndex];
            if (enable)
                mat.EnableKeyword(_keyword);
            else
                mat.DisableKeyword(_keyword);
        }
    }
}
