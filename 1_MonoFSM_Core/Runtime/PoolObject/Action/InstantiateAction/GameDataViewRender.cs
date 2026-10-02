using System;
using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using _1_MonoFSM_Core.Runtime.MonoData;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// 在 _anchor 底下畫出「某顆 VarGameData 目前指到的道具」長什麼樣子（純本地 View，不同步 Transform）。
    /// GameData 有 _viewPrefab 就 instantiate 它；沒有就從 bindPrefab（含 _entityPrefab fallback）的 ViewRoot
    /// 複製一份只有 MeshFilter + MeshRenderer 的外觀（見 ViewMeshCopyUtility），再依 bounds 等比縮放、置中塞進 _fitSize 方塊。
    /// 每個 GameData 只建一次並快取，換商品只切 active；_visibleVar 為 false 時全部藏起來。
    /// 生不出外觀（沒 prefab / 沒 mesh）時改開 _fallbackView。掛在 [Event] RenderLoop 底下。
    /// 例：掛貨滑車托盤上的商品外觀（_gameData = d_托盤商品，_anchor = 托盤 AttachPoint）。
    /// </summary>
    public class GameDataViewRender : AbstractRenderBehaviour
    {
        public enum ViewStatus
        {
            NotRendered,
            Shown,
            /// <summary>_visibleVar 為 false（或它所在物件停用）</summary>
            HiddenByVisibleVar,
            /// <summary>_gameData 目前沒有值</summary>
            NoGameData,
            /// <summary>_anchor 沒接</summary>
            NoAnchor,
            /// <summary>GameData 沒有 _viewPrefab，bindPrefab 也是 null</summary>
            NoPrefab,
            /// <summary>bindPrefab 底下找不到任何可複製的 mesh（顯示 fallback）</summary>
            NoMesh
        }

        public override string Description =>
            "View of " + (_gameData?._var != null ? _gameData._var.name : "GameData") +
            " @ " + (_anchor != null ? _anchor.name : "null") + " fit " + _fitSize;

        [SerializeField] private VarGameDataWrapper _gameData;

        [Tooltip("true 才顯示；留空 = 一直顯示。不用 _conditionGroup：condition 不成立時整顆 render 會被跳過，就沒機會把外觀藏起來")]
        [DropDownRef] [SerializeField] private VarBool _visibleVar;

        [Tooltip("外觀生在這個 transform 底下")] [SerializeField]
        private Transform _anchor;

        [Tooltip("外觀等比縮放塞進這個邊長的方塊（公尺），置中對齊 _anchor；<= 0 = 不縮放")] [SerializeField]
        private float _fitSize = 0.5f;

        [Tooltip("生不出外觀（沒 prefab / 沒 mesh）時打開的節點，例如一顆貨箱方塊；可留空")] [SerializeField]
        private GameObject _fallbackView;

        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private ViewStatus _status = ViewStatus.NotRendered;

        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private GameData _lastData;

        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private Transform _currentView;

        private struct CacheEntry
        {
            public Transform View; //null = 建過但生不出外觀，不要每幀重試
            public ViewStatus FailStatus;
        }

        [NonSerialized] private readonly Dictionary<GameData, CacheEntry> _cache = new();
        [NonSerialized] private readonly List<Renderer> _rendererBuffer = new();
        [NonSerialized] private readonly List<MeshFilter> _filterBuffer = new();

        public override void OnEnterRenderImplement()
        {
            Refresh();
        }

        public override void OnRenderImplement()
        {
            Refresh();
        }

        private void Refresh()
        {
            var visible = _visibleVar == null || (_visibleVar.isActiveAndEnabled && _visibleVar.CurrentValue);
            if (!visible)
            {
                _status = ViewStatus.HiddenByVisibleVar;
                Show(null, false);
                return;
            }

            if (_anchor == null)
            {
                _status = ViewStatus.NoAnchor;
                Show(null, false);
                return;
            }

            var data = _gameData != null ? _gameData.Value : null;
            _lastData = data;
            if (data == null)
            {
                _status = ViewStatus.NoGameData;
                Show(null, false);
                return;
            }

            if (!_cache.TryGetValue(data, out var entry))
            {
                entry.View = Build(data, out entry.FailStatus);
                _cache[data] = entry;
            }

            if (entry.View == null)
            {
                _status = entry.FailStatus;
                Show(null, true);
                return;
            }

            _status = ViewStatus.Shown;
            Show(entry.View, false);
        }

        private void Show(Transform view, bool fallback)
        {
            if (_currentView != view)
            {
                if (_currentView != null)
                    _currentView.gameObject.SetActive(false);
                _currentView = view;
                if (view != null)
                    view.gameObject.SetActive(true);
            }

            if (_fallbackView != null && _fallbackView.activeSelf != fallback)
                _fallbackView.SetActive(fallback);
        }

        //只在第一次看到這筆 GameData 時跑（會配置記憶體）
        private Transform Build(GameData data, out ViewStatus failStatus)
        {
            failStatus = ViewStatus.Shown;
            var viewPrefab = data.viewPrefab;
            MonoObj bindPrefab = null;
            if (viewPrefab == null)
            {
                bindPrefab = data.bindPrefab;
                if (bindPrefab == null)
                {
                    failStatus = ViewStatus.NoPrefab;
                    return null;
                }
            }

            var holder = new GameObject("[View] " + data.name).transform;
            holder.SetParent(_anchor, false);
            holder.gameObject.SetActive(false);
            var content = new GameObject("Content").transform;
            content.SetParent(holder, false);

            if (viewPrefab != null)
            {
                var inst = Object.Instantiate(viewPrefab, content, false);
                inst.transform.localPosition = Vector3.zero;
                //純外觀：展示 prefab 上若帶 collider 一律關掉，不能擋到包裹 / 玩家
                var colliders = inst.GetComponentsInChildren<Collider>(true);
                for (var i = 0; i < colliders.Length; i++)
                    colliders[i].enabled = false;
            }
            else
            {
                var viewRoot = bindPrefab.GetComponentInChildren<ViewRoot>(true);
                var source = viewRoot != null ? viewRoot.transform : bindPrefab.transform;
                var copied = ViewMeshCopyUtility.CopyActiveRenderers(source, content, HideFlags.None,
                    _rendererBuffer);
                if (copied == 0)
                {
                    Object.Destroy(holder.gameObject);
                    failStatus = ViewStatus.NoMesh;
                    return null;
                }
            }

            if (_fitSize > 0f &&
                ViewMeshCopyUtility.TryGetLocalBounds(content, holder, _filterBuffer, out var bounds))
            {
                var size = bounds.size;
                var longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                if (longest > 1e-4f)
                {
                    var s = _fitSize / longest;
                    content.localScale = new Vector3(s, s, s);
                    content.localPosition = -bounds.center * s;
                }
            }

            return holder;
        }
    }
}
