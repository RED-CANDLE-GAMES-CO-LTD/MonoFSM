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

        [Button("Preview")]
        public override void OnEnterRenderImplement()
        {
            if (_rendererCollection != null)
            {
                var renderers = _rendererCollection.Renderers;
                if (renderers != null)
                {
                    foreach (var r in renderers)
                    {
                        if (r == null) continue;
                        ApplyKeyword(r);
                    }
                }
            }

            if (_renderer != null)
            {
                ApplyKeyword(_renderer);
            }
            else if (_rendererCollection == null)
            {
                Debug.LogWarning("EnableKeywordAction: No Renderer or RendererCollection assigned",
                    this);
            }
        }

        public override void OnRenderImplement()
        {
            OnEnterRenderImplement();
        }

        private void ApplyKeyword(Renderer renderer)
        {
            var materials = renderer.materials;
            if (_materialIndex < 0 || _materialIndex >= materials.Length)
            {
                Debug.LogWarning(
                    $"EnableKeywordAction: materialIndex {_materialIndex} out of range on [{renderer.name}]",
                    this);
                return;
            }

            var mat = materials[_materialIndex];
            if (_enable.Value)
                mat.EnableKeyword(_keyword);
            else
                mat.DisableKeyword(_keyword);
        }
    }
}
