using System;
using System.Collections.Generic;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Detection
{
    /// <summary>
    ///     「這次偵測要忽略哪些 collider」的共用設定。掛在 trigger / raycast / overlap 各種偵測器上，
    ///     以 MonoEntity 為單位指定，執行期攤平成 collider set，每次命中只做 O(1) 查表。
    ///     用法：欄位宣告成 [SerializeField] IgnoreColliderFilter，在 EnterSceneAwake 呼叫 Init(this)，
    ///     命中時用 IsIgnored(collider) 過濾。
    /// </summary>
    [Serializable]
    [InlineProperty]
    public class IgnoreColliderFilter
    {
        [Tooltip("忽略這些 Entity 底下所有 collider 的命中。偵測從玩家身上發出時會掃到自己，" +
                 "layer 表達不了「誰發射的」，只能在這裡明確指定")]
        [SerializeField]
        private List<MonoEntity> _ignoreEntities = new();

        [Tooltip("把偵測器往上一路找到的所有 MonoEntity 都忽略。裝備掛在角色身上時會有巢狀 entity" +
                 "（槍 → 角色），全部收進來才不會漏掉；最終是 collider 聯集，多幾層只是更保守")]
        [SerializeField]
        private bool _ignoreSelfEntity;

        //把要忽略的 collider 攤平成 set，每幀只做 O(1) 查表，不用每個 hit 爬 transform
        private readonly HashSet<Collider> _ignoredColliders = new();

        //GetComponentsInChildren(list) 的收集用 buffer，重複使用避免 GC
        private readonly List<Collider> _colliderQueryBuffer = new();

        private Component _owner;

        //GetComponentsInParent(list) 用，重複使用避免 GC
        private readonly List<MonoEntity> _selfEntities = new();

#if UNITY_EDITOR
        [ShowIf(nameof(_ignoreSelfEntity))]
        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(9)]
        [ListDrawerSettings(IsReadOnly = true)]
        [Tooltip("勾了 ignoreSelf 之後實際會被忽略的 parent entity（Editor 預覽用）")]
        private List<MonoEntity> _selfEntitiesPreview = new();
#endif

        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(10)]
        private int IgnoredColliderCount => _ignoredColliders.Count;

        /// <summary>
        ///     有沒有設定任何要忽略的對象。沒有的話呼叫端可以整段跳過過濾。
        /// </summary>
        public bool HasIgnoreTarget => _ignoredColliders.Count > 0;

        /// <summary>
        ///     在偵測器的 EnterSceneAwake 呼叫，owner 傳 this。會往 parent 找 MonoEntity 當作「自己」。
        /// </summary>
        public void Init(Component owner)
        {
            _owner = owner;
            Rebuild();
        }

        public bool IsIgnored(Collider col)
        {
            return col != null && _ignoredColliders.Contains(col);
        }

        /// <summary>
        ///     重建忽略用的 collider set。Entity 底下的 collider 有增減時要呼叫（生成部件、換裝備等）。
        /// </summary>
        public void Rebuild()
        {
            _ignoredColliders.Clear();
            if (_ignoreSelfEntity)
            {
                CollectSelfEntities();
                if (_selfEntities.Count == 0)
                    Debug.LogWarning(
                        "_ignoreSelfEntity 有開但 parent 鏈上找不到任何 MonoEntity，忽略自己不會生效",
                        _owner);
                for (var i = 0; i < _selfEntities.Count; i++)
                    CollectCollidersOf(_selfEntities[i]);
            }

            for (var i = 0; i < _ignoreEntities.Count; i++)
                CollectCollidersOf(_ignoreEntities[i]);
        }

        /// <summary>
        ///     執行期加入要忽略的 Entity（例如抓在手上的物件）。
        /// </summary>
        public void AddIgnoreEntity(MonoEntity entity)
        {
            if (entity == null || _ignoreEntities.Contains(entity))
                return;
            _ignoreEntities.Add(entity);
            CollectCollidersOf(entity);
        }

        /// <summary>
        ///     執行期移除要忽略的 Entity（例如放手）。collider 可能與其他忽略對象重疊，所以整份重建。
        /// </summary>
        public void RemoveIgnoreEntity(MonoEntity entity)
        {
            if (entity == null || !_ignoreEntities.Remove(entity))
                return;
            Rebuild();
        }

        //一路往上收，巢狀 entity（裝備 → 角色）全部都要忽略
        private void CollectSelfEntities()
        {
            _selfEntities.Clear();
            if (_owner == null)
                return;
            _owner.GetComponentsInParent(true, _selfEntities);
        }

#if UNITY_EDITOR
        /// <summary>
        ///     由持有者的 OnValidate 呼叫，讓 Inspector 在非播放時就能看到 ignoreSelf 會抓到哪些 entity。
        /// </summary>
        public void EditorRefreshPreview(Component owner)
        {
            _owner = owner;
            _selfEntitiesPreview.Clear();
            if (!_ignoreSelfEntity)
                return;
            CollectSelfEntities();
            _selfEntitiesPreview.AddRange(_selfEntities);
        }
#endif

        private void CollectCollidersOf(MonoEntity entity)
        {
            if (entity == null)
                return;
            _colliderQueryBuffer.Clear();
            entity.GetComponentsInChildren(true, _colliderQueryBuffer);
            for (var i = 0; i < _colliderQueryBuffer.Count; i++)
                _ignoredColliders.Add(_colliderQueryBuffer[i]);
        }
    }
}
