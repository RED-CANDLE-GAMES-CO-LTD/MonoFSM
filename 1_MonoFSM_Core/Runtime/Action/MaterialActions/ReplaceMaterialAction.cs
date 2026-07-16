using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Render
{
    /// <summary>
    /// FSM 觸發時，把指定 Renderer（或 RendererCollection）第 _materialIndex 個 material
    /// 替換成 _material。用於角色換 skin 等情境。
    /// 用 sharedMaterials 寫回，直接掛上現成的 material asset，不產生多餘的 material instance。
    /// </summary>
    public class ReplaceMaterialAction : AbstractStateAction
    {
        public override string Description =>
            $"Replace material[{_materialIndex}] on [{(_rendererCollection != null ? _rendererCollection.name : _renderer != null ? _renderer.name : "null")}] to [{(_material != null ? _material.name : "?")}]";

        [SerializeField] [DropDownRef] private Renderer _renderer;

        [HideIf(nameof(_renderer))] [SerializeField] [DropDownRef]
        private RendererCollection _rendererCollection;

        [SerializeField] private int _materialIndex;

        [Required] [SerializeField] private Material _material;

        [Button("Preview")]
        protected override void OnActionExecuteImplement()
        {
            if (_material == null)
            {
                Debug.LogWarning("ReplaceMaterialAction: _material is null", this);
                return;
            }

            if (_renderer != null)
                ApplyMaterial(_renderer);

            if (_rendererCollection != null)
            {
                var renderers = _rendererCollection.Renderers;
                if (renderers != null)
                    foreach (var r in renderers)
                    {
                        if (r == null) continue;
                        ApplyMaterial(r);
                    }
            }

            if (_renderer == null && _rendererCollection == null)
                Debug.LogWarning("ReplaceMaterialAction: No Renderer or RendererCollection assigned", this);
        }

        private void ApplyMaterial(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            if (_materialIndex < 0 || _materialIndex >= materials.Length)
            {
                Debug.LogWarning(
                    $"ReplaceMaterialAction: materialIndex {_materialIndex} out of range on [{renderer.name}]",
                    this);
                return;
            }

            materials[_materialIndex] = _material;
            renderer.sharedMaterials = materials;
        }
    }
}
