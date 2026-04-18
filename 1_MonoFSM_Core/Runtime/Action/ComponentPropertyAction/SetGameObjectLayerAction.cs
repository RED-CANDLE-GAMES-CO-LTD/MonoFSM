using System.Collections.Generic;
using System.Linq;
using MonoFSM.Render;
using Sirenix.OdinInspector;
using UnityEngine;

// 未來升級方向（目前用方案 A: Odin ValueDropdown）：
// B. 專案級 [Layer] attribute + drawer：任何 int 欄位都能套。當 layer 下拉在多個 Action 重複出現時再做。
// C. VarLayer / VarLayerWrapper：一整套 Variable type，可放進 VarFolder、跨 Action 共用。
//    成本最大，僅在「layer 變數需要被多個系統讀寫、需要在 runtime 被觀察」時才值得。

namespace MonoFSM.Core.Runtime.Action.ComponentPropertyAction
{
    /// <summary>
    /// 設定 GameObject 的 layer。
    /// 兩種 target 模式：
    ///   - _target: 單一 GameObject
    ///   - _rendererCollection: 對集合內每個 renderer.gameObject.layer 設值（有值時會隱藏 _target）
    /// 成對用法：Enter 勾 _cacheOriginalLayer 把原 layer 存進 VarInt；Exit 用另一個同 Action 勾 _fromVar 從該 VarInt 還原。
    /// RendererCollection 模式下 cache 讀取第一個 renderer 的 layer 當代表（預設集合內 layer 一致）。
    /// </summary>
    public class SetGameObjectLayerAction : AbstractStateAction
    {
        [SerializeField] private RendererCollection _rendererCollection;

        [HideIf(nameof(HasRendererCollection))] [Required] [SerializeField]
        private GameObject _target;

        [SerializeField] private bool _fromVar;

        [HideIf(nameof(_fromVar))]
#if UNITY_EDITOR
        [ValueDropdown(nameof(GetLayerOptions))]
#endif
        [SerializeField]
        private int _newLayerConst;

        [ShowIf(nameof(_fromVar))] [DropDownRef] [SerializeField]
        private VarInt _newLayerVar;

        [SerializeField] private bool _cacheOriginalLayer;

        [ShowIf(nameof(_cacheOriginalLayer))] [DropDownRef] [SerializeField]
        private VarInt _cacheVar;

        private bool HasRendererCollection => _rendererCollection != null;

        private int ResolvedNewLayer =>
            _fromVar && _newLayerVar != null ? _newLayerVar.Value : _newLayerConst;

        public override string Description
        {
            get
            {
                var targetName = HasRendererCollection
                    ? $"{_rendererCollection.name}.renderers"
                    : _target != null
                        ? _target.name
                        : "(no target)";
                var src = _fromVar
                    ? _newLayerVar != null ? _newLayerVar.name : "?"
                    : LayerMask.LayerToName(_newLayerConst);
                return $"Set {targetName}.layer = {src}" +
                       (_cacheOriginalLayer ? $" (cache→{_cacheVar?.name})" : "");
            }
        }

        protected override void OnActionExecuteImplement()
        {
            var newLayer = ResolvedNewLayer;

            if (HasRendererCollection)
            {
                var renderers = _rendererCollection.Renderers;
                if (renderers == null || renderers.Length == 0)
                {
                    Debug.LogWarning(
                        "[SetGameObjectLayerAction] RendererCollection has no renderers", this);
                    return;
                }

                if (_cacheOriginalLayer && _cacheVar != null)
                    _cacheVar.SetValue(renderers[0].gameObject.layer, this);

                foreach (var r in renderers)
                    if (r != null)
                        r.gameObject.layer = newLayer;
                return;
            }

            if (_target == null)
            {
                Debug.LogWarning("[SetGameObjectLayerAction] _target is null", this);
                return;
            }

            if (_cacheOriginalLayer && _cacheVar != null)
                _cacheVar.SetValue(_target.layer, this);

            _target.layer = newLayer;
        }

#if UNITY_EDITOR
        private static IEnumerable<ValueDropdownItem<int>> GetLayerOptions() =>
            Enumerable.Range(0, 32)
                .Where(i => !string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                .Select(i => new ValueDropdownItem<int>($"{i}: {LayerMask.LayerToName(i)}", i));
#endif
    }
}
