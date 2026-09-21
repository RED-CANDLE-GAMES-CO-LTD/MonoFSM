using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Simulate;
using MonoFSM.Render;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.ComponentPropertyAction
{
    /// <summary>
    /// 限時把 RendererCollection 底下所有 renderer 的 GameObject layer 蓋成指定 layer，數秒後自動還原成基底
    /// （還原邏輯在 RendererCollection 自己的 IRenderUpdate，這裡只下指令）。
    /// 典型用法：探測儀的 ItemDetect receiver → EffectEnterNode 底下掛這顆，layer = CollectableVision，讓道具短暫透視可見。
    /// _rendererCollection 留空時會從所屬 MonoObj 的 Entity 找子樹第一顆 RendererCollection（ViewRoot 上那顆），
    /// 所以放在共用 ModulePack 裡不用每顆道具手動接引用。
    /// 純視覺，放在 receiver 底下時 EffectEnterNode 要勾 _forceExecuteWithoutStateAuthority 才會在 proxy 端也跑。
    /// </summary>
    public class SetRendererLayerOverrideAction : AbstractStateAction
    {
        public enum FailReason
        {
            None,
            NoRendererCollection, //_rendererCollection 沒接、往上也找不到 MonoObj / Entity 子樹沒有 RendererCollection
            NoRenderers, //RendererCollection 底下沒有 renderer
        }

        [SerializeField] private RendererCollection _rendererCollection;

#if UNITY_EDITOR
        [ValueDropdown(nameof(GetLayerOptions))]
#endif
        [SerializeField]
        private int _layer;

        [Tooltip("override 維持秒數，可綁 VarFloat 或直接填常數")] [SerializeField]
        private VarFloatWrapper _duration = new(5f);

        [AutoParent] private MonoObj _parentObj;

        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] private FailReason _lastFailReason;
        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] private RendererCollection _resolvedCollection;
        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] private float _lastAppliedDuration;

        public override string Description =>
            $"Layer override → {LayerMask.LayerToName(_layer)} for {_duration}s" +
            (_rendererCollection != null ? $" ({_rendererCollection.name})" : " (auto: entity ViewRoot)");

        private RendererCollection ResolveCollection()
        {
            if (_rendererCollection != null)
                return _rendererCollection;
            if (_resolvedCollection != null)
                return _resolvedCollection;
            if (_parentObj == null || _parentObj.Entity == null)
                return null;
            _resolvedCollection = _parentObj.Entity.GetCompCache<RendererCollection>();
            return _resolvedCollection;
        }

        protected override void OnActionExecuteImplement()
        {
            var collection = ResolveCollection();
            if (collection == null)
            {
                _lastFailReason = FailReason.NoRendererCollection;
                return;
            }

            var renderers = collection.Renderers;
            if (renderers == null || renderers.Length == 0)
            {
                _lastFailReason = FailReason.NoRenderers;
                return;
            }

            _lastFailReason = FailReason.None;
            _lastAppliedDuration = _duration.Value;
            collection.SetLayerOverride(_layer, _lastAppliedDuration);
        }

#if UNITY_EDITOR
        private static IEnumerable<ValueDropdownItem<int>> GetLayerOptions() =>
            Enumerable.Range(0, 32)
                .Where(i => !string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                .Select(i => new ValueDropdownItem<int>($"{i}: {LayerMask.LayerToName(i)}", i));
#endif
    }
}
