using MonoFSM.Core.Simulate;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Render
{
    /// <summary>
    /// 收集子樹的 Renderer 供 material / renderingLayerMask / GameObject layer 類的 action 共用。
    /// GameObject layer 由這裡統一維護：<see cref="SetLayer"/> 改「基底 layer」（例：被抓取 → PickingVision），
    /// <see cref="SetLayerOverride"/> 蓋一層「限時 layer」（例：探測儀 → CollectableVision，數秒後自動還原成基底），
    /// 兩者互不打架：override 期間改基底只記錄不落地，override 到期就還原到「最新的基底」。
    /// 到期檢查掛在 IRenderUpdate（由 root MonoObj 收集），沒有 override 時直接 return。
    /// </summary>
    public class RendererCollection : MonoBehaviour, IRenderUpdate
    {
        public RendererCollection _rendererCollectionRef;

        [ShowInInspector] [AutoChildren] private Renderer[] _renderers;

        public Renderer[] Renderers => _rendererCollectionRef != null
            ? _rendererCollectionRef.Renderers
            : _renderers;

        //每個 renderer 的 material instance 陣列，lazy 建立後 cache，避免每幀 renderer.materials 的 alloc 與重複 instancing
        private Material[][] _cachedMaterials;

        public Material[][] CachedMaterials
        {
            get
            {
                if (_rendererCollectionRef != null)
                    return _rendererCollectionRef.CachedMaterials;

                if (_cachedMaterials == null)
                {
                    if (_renderers == null)
                        return null;
                    var result = new Material[_renderers.Length][];
                    var allReady = true;
                    for (var i = 0; i < _renderers.Length; i++)
                    {
                        var r = _renderers[i];
                        result[i] = r != null ? r.materials : null;
                        //renderer 還沒 ready 時 materials 會回長度 0，這種結果不能 cache，
                        //否則之後永遠拿不到真的 material instance
                        if (r != null && (result[i] == null || result[i].Length == 0))
                            allReady = false;
                    }

                    //還沒 ready：這次先用暫時結果，下次再重取
                    if (allReady == false)
                        return result;

                    _cachedMaterials = result;
                }

                return _cachedMaterials;
            }
        }

        public void SetRenderingLayerMask(uint mask)
        {
            if (_rendererCollectionRef != null)
            {
                _rendererCollectionRef.SetRenderingLayerMask(mask);
                return;
            }

            if (_renderers == null || _renderers.Length == 0)
                return;
            foreach (var r in _renderers)
            {
                r.renderingLayerMask = mask;
            }
        }

        #region GameObject Layer

        public enum LayerState
        {
            None,
            Base, //只有基底 layer 落地
            Overriding, //限時 override 中，基底暫存在 _baseLayers
        }

        [FoldoutGroup("Layer")] [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private LayerState _layerState = LayerState.None;

        //每顆 renderer 各自的基底 layer（第一次動 layer 時從當下值抓，之後由 SetLayer 覆寫）
        [FoldoutGroup("Layer")] [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private int[] _baseLayers;

        [FoldoutGroup("Layer")] [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private int _overrideLayer = -1;

        [FoldoutGroup("Layer")] [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private int _overrideEndTick;

        [FoldoutGroup("Layer")] [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private float _lastOverrideDuration;

        /// <summary>目前落在 renderer 上的「基底」layer；取第一顆 renderer 當代表（集合內預設一致）</summary>
        public int BaseLayer
        {
            get
            {
                if (_rendererCollectionRef != null)
                    return _rendererCollectionRef.BaseLayer;
                EnsureBaseLayers();
                return _baseLayers != null && _baseLayers.Length > 0 ? _baseLayers[0] : 0;
            }
        }

        public bool IsLayerOverriding => _rendererCollectionRef != null
            ? _rendererCollectionRef.IsLayerOverriding
            : _layerState == LayerState.Overriding;

        //第一次接觸 layer 時把當下每顆 renderer 的 layer 記成基底
        private void EnsureBaseLayers()
        {
            if (_renderers == null)
                return;
            if (_baseLayers != null && _baseLayers.Length == _renderers.Length)
                return;

            _baseLayers = new int[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++)
                _baseLayers[i] = _renderers[i] != null ? _renderers[i].gameObject.layer : 0;
            if (_layerState == LayerState.None)
                _layerState = LayerState.Base;
        }

        /// <summary>
        /// 改基底 layer（持久，直到下一次 SetLayer）。override 期間只記錄，不落地，等 override 到期再還原成這個值。
        /// </summary>
        public void SetLayer(int layer)
        {
            if (_rendererCollectionRef != null)
            {
                _rendererCollectionRef.SetLayer(layer);
                return;
            }

            if (_renderers == null || _renderers.Length == 0)
                return;
            EnsureBaseLayers();
            for (var i = 0; i < _baseLayers.Length; i++)
                _baseLayers[i] = layer;

            if (_layerState == LayerState.Overriding)
                return; //override 還在，基底先記著

            ApplyLayer(layer);
        }

        /// <summary>
        /// 限時把所有 renderer 蓋成 layer，duration 秒後（依 WorldUpdateSimulator tick）還原成基底。
        /// 重複呼叫會延長 / 換 layer，不會疊加。duration &lt;= 0 視為立刻清掉。
        /// </summary>
        public void SetLayerOverride(int layer, float duration)
        {
            if (_rendererCollectionRef != null)
            {
                _rendererCollectionRef.SetLayerOverride(layer, duration);
                return;
            }

            if (_renderers == null || _renderers.Length == 0)
                return;
            if (duration <= 0f)
            {
                ClearLayerOverride();
                return;
            }

            EnsureBaseLayers();
            _lastOverrideDuration = duration;
            var dt = WorldUpdateSimulator.DeltaTime;
            var ticks = dt > 0f ? Mathf.CeilToInt(duration / dt) : 1;
            _overrideEndTick = WorldUpdateSimulator.CurrentTick + Mathf.Max(1, ticks);
            _overrideLayer = layer;
            _layerState = LayerState.Overriding;
            ApplyLayer(layer);
        }

        /// <summary>立刻結束 override，還原成基底 layer</summary>
        public void ClearLayerOverride()
        {
            if (_rendererCollectionRef != null)
            {
                _rendererCollectionRef.ClearLayerOverride();
                return;
            }

            if (_layerState != LayerState.Overriding)
                return;
            _layerState = LayerState.Base;
            _overrideLayer = -1;
            if (_renderers == null || _baseLayers == null)
                return;
            for (var i = 0; i < _renderers.Length && i < _baseLayers.Length; i++)
                if (_renderers[i] != null)
                    _renderers[i].gameObject.layer = _baseLayers[i];
        }

        private void ApplyLayer(int layer)
        {
            foreach (var r in _renderers)
                if (r != null)
                    r.gameObject.layer = layer;
        }

        public void Render(float runnerLocalRenderTime)
        {
            if (_layerState != LayerState.Overriding)
                return;
            if (WorldUpdateSimulator.CurrentTick < _overrideEndTick)
                return;
            ClearLayerOverride();
        }

        //被收進 grab slot（SetActive false）或回收進 pool 時把 override 收掉，不要把限時 layer 帶進下一次啟用
        private void OnDisable()
        {
            ClearLayerOverride();
        }

        #endregion
    }
}
