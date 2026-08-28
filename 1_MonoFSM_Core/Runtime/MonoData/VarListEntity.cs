using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.EditorExtension;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.Variable
{
    /// <summary>
    ///     MonoEntity 清單的 Var（VarList&lt;MonoEntity&gt;）。也實作 ICurrentEntityOwner，
    ///     所以 CurrentEntity（= CurrentListItem）可被子節點的 VarEntityCurrentItem 直接鏡射。
    ///     搭配 SelectRandomIndexAction / VarListToNextAction 之類的 Action 移動游標；
    ///     index 要跨端一致時，把 _currentIndexVar 綁一顆掛 NetworkedVarTag 的 VarInt。
    /// </summary>
    public class VarListEntity
        : VarList<MonoEntity>, //這個變
            MonoFSM.Runtime.Variable.ICurrentEntityOwner
    {
        //給掛在 child 的 VarEntityCurrentItem 取值用（proxy 模式時 CurrentListItem 自己會 forward）
        public MonoEntity CurrentEntity => CurrentListItem;
        public string ListDescription => Description;

#if UNITY_EDITOR
        //list 本身沒有「跑過一輪」的語意，內容直接看 list 就好
        public IReadOnlyList<MonoEntity> DebugIteratedEntities => null;
#endif
    }

    //FIXME: 用的到set或queue嗎？ 還是乾脆把List做完就好，其他要用再說另外實作？
    public class VarList<T> : AbstractVarList, ISerializationCallbackReceiver, IResetStateRestore
    {
        // value source（_valueSources / valueSource / HasValueSource）已上移到 AbstractMonoVariable，
        // 與其他變數共用同一套，可撿任何 child IValueProvider（含 GetVarFromParentEntitySource）。
        // 取值用 valueSource.Get<List<T>>()。

        // [ShowInInspector]
        // public bool IsReadOnly => HasValueSource;

        /// <summary>
        /// 從 ParentEntity 解析出的 proxy VarList（varRef 機制）。
        /// 有值時所有讀寫、index 操作都轉發給它；value source 優先於 varRef。
        /// </summary>
        // re-entrancy guard：VarList 不像 GenericObjectVariable 有 recursion guard，
        // 若 value-source / proxy 接成參照環（例如 X 的 source 讀 L、L 的 source 又繞回 X），
        // 沿 ProxyVarList→CurrentListItem 會無限遞迴 → StackOverflow → Unity 不寫 log 直接閃退。
        // 同一顆 instance 重入時直接回 null，把環降級成可被看見的 null 而非 crash。
        [NonSerialized] private bool _resolvingProxy;

        [ShowInPlayMode]
        private VarList<T> ProxyVarList
        {
            get
            {
                if (_resolvingProxy)
                {
                    Debug.LogError(
                        "VarList ProxyVarList re-entrant：value-source/proxy 接成參照環，已中止以避免 StackOverflow。請檢查接線。",
                        this);
                    return null;
                }

                _resolvingProxy = true;
                try
                {
                    // value source 有兩種：
                    //   (A) computed-list source（如 ListEntityFromEffectDealer）→ 只給純 List<T>、
                    //       沒有 index/cursor，不走這裡，由讀取方法用 ValueSourceList 取內容、疊本地 index。
                    //   (B) var-resolving source（如 GetVarFromParentEntitySource，tag 指向另一顆完整
                    //       VarList）→ 實作 IVariableProvider，能解析出來源 VarList<T> 本身，
                    //       當完整 proxy 用（index/cursor/寫入全部 forward）。
                    // 優先序：value-source-var(B) → varRef。
                    if (valueSource is IVariableProvider varProvider)
                    {
                        var sourceVar = varProvider.GetVar<VarList<T>>();
                        if (sourceVar != null)
                        {
                            if (ReferenceEquals(sourceVar, this))
                            {
                                Debug.LogError(
                                    "VarList value source resolved to self, possible misconfiguration.",
                                    this);
                                return null;
                            }

                            return sourceVar;
                        }
                    }

                    var resolved = varRef;
                    if (resolved == null)
                        return null;
                    if (ReferenceEquals(resolved, this))
                    {
                        Debug.LogError(
                            "VarList proxy resolved to self, possible misconfiguration.",
                            this);
                        return null;
                    }

                    if (resolved is VarList<T> varList)
                        return varList;
                    Debug.LogError(
                        $"Referenced variable {resolved.name} is not VarList<{typeof(T).Name}>.",
                        this);
                    return null;
                }
                finally
                {
                    _resolvingProxy = false;
                }
            }
        }

        /// <summary>
        /// value source 提供的唯讀集合內容（純 List&lt;T&gt;）。HasValueSource 為 true 但當下沒有
        /// 任何 IsValid 的 provider 時，valueSource 會是 null → 回傳 null，呼叫端需自行 null guard。
        /// index/cursor（RawIndex）仍走 local，等於對這顆 computed list 疊一個本地游標。
        /// </summary>
        private List<T> ValueSourceList => valueSource?.Get<List<T>>();

        public enum CollectionStorageType
        {
            List,
            Queue,
            HashSet,
        }

        [SerializeField]
        [ShowInInspector]
        [Tooltip("Determines the underlying collection type used.")]
        private CollectionStorageType _storageType = CollectionStorageType.List;

        //FIXME: 好像也不需要這個？runtime用而已？ 不一定

        [SerializeField] // This will be used by Unity for serialization
        protected List<T> _backingListForSerialization = new();

        /// <summary>
        /// 集合內容的來源清單。預設就是 prefab 上序列化的 backing list，
        /// 子類可以覆寫成「外部 asset 提供的清單」（見 VarListData._sourceConfig）。
        /// 注意：這個 getter 會在 OnAfterDeserialize 期間被呼叫，實作不可以碰 Unity API。
        /// </summary>
        protected virtual List<T> SourceList => _backingListForSerialization;

        [ShowInPlayMode]
        private object _activeCollection; // Runtime instance: List<T>, Queue<T>, or HashSet<T>

        /// <summary>
        /// 選配：掛在 children 的 index 游標變數。存在時 index 改用這顆 first-class Var，
        /// UI binder / Condition 可直接綁，改值會走 OnValueChangedHandler；
        /// 沒掛則 fallback 到下方的 _currentIndex wrapper。
        /// </summary>
        [ShowInInspector] [AutoChildren(DepthOneOnly = true)]
        private VarIntIndex _indexVar;

        //FIXME: 這個要弄成Field嗎...比較好reset?
        [FormerlySerializedAs("_currentIndex")]
        [HideIf(nameof(_indexVar))]
        [SerializeField]
        private VarIntWrapper _currentIndexVar;

        // index 讀寫統一收斂點：有掛 _indexVar 走它，否則用 wrapper fallback
        [ShowInInspector]
        private int RawIndex => _indexVar != null ? _indexVar.CurrentValue : _currentIndexVar.Value;

        private void SetRawIndex(int index)
        {
            if (_indexVar != null)
                _indexVar.SetValue(index, this);
            else
                _currentIndexVar.SetValue(index, this);
        }

        [PreviewInInspector]
        private int _lastIndex = -1;
        public int _defaultIndex;

        [ShowInInspector]
        public override int CurrentIndex
        {
            get
            {
                var proxy = ProxyVarList;
                if (proxy != null)
                    return proxy.CurrentIndex;
                return RawIndex;
            }
        }

        // proxy 模式（var-resolving value source 或 varRef）下，集合與 index 真正的 source 都在
        // proxy 那顆 VarList，本地 _indexVar 存的值會 stale。讓子層的 VarIntIndex 能據此判斷
        // 該讀 owner.CurrentIndex(→forward 給 proxy) 還是本地值，避免外部 binder 拿到舊 index。
        public override bool IsProxy => ProxyVarList != null;

        public override void SetCurrentIndexTo(int index)
        {
            var proxy = ProxyVarList;
            if (proxy != null)
            {
                proxy.SetCurrentIndexTo(index);
                return;
            }

            // -1 = 無選取（NoSelection），是合法狀態：GrabSlotHolder 空手時、GoToNext 遇到空 list
            // 時都會設 -1，而 CurrentListItem / GetItemAt 對負 index 本來就回 default。
            if (index < NoSelectionIndex || index >= Count)
            {
                Debug.LogError(
                    $"Index {index} is out of bounds for the collection of size {Count}.",
                    this
                );
                return;
            }

            _lastIndex = RawIndex;
            SetRawIndex(index);
        }

        public override void GoToNext()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
            {
                proxy.GoToNext();
                return;
            }

            EnsureActiveCollectionInitialized();
            if (Count == 0)
            {
                SetCurrentIndexTo(-1);
                return;
            }

            var index = (RawIndex + 1) % Count;
            SetCurrentIndexTo(index);
        }

        public override void GoToPrevious()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
            {
                proxy.GoToPrevious();
                return;
            }

            EnsureActiveCollectionInitialized();
            if (Count == 0)
            {
                SetCurrentIndexTo(-1);
                return;
            }

            var index = (RawIndex - 1 + Count) % Count;
            SetCurrentIndexTo(index);
        }

        public T GetFirstOrDefault()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
                return proxy.GetFirstOrDefault();
            if (HasValueSource)
            {
                var sourceList = ValueSourceList;
                return sourceList is { Count: > 0 } ? sourceList[0] : default;
            }

            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list && list.Count > 0)
                return list[0];
            if (_activeCollection is Queue<T> queue && queue.Count > 0)
                return queue.Peek();
            if (_activeCollection is HashSet<T> set && set.Count > 0)
                return set.FirstOrDefault();
            return default;
        }

        [ShowInPlayMode]
        public T LastItem //list內容如果會變動的話，這個感覺蠻有問題的？ConstCollection?...
        {
            get
            {
                var proxy = ProxyVarList;
                if (proxy != null)
                    return proxy.LastItem;
                if (_lastIndex < 0)
                    return default;
                if (Count == 0)
                    return default;
                return GetList()[_lastIndex];
            }
        }

        [ShowInPlayMode]
        public T CurrentListItem //不是object... current ListItem
        {
            get
            {
                var proxy = ProxyVarList;
                if (proxy != null)
                    return proxy.CurrentListItem;
                if (RawIndex < 0)
                    return default;

                // 先取得 list，再對同一個 list 做 bounds check，避免 Count 與 GetList() 來源不同（HasProxyValue）
                var list = GetList();
                if (list == null || RawIndex >= list.Count)
                    return default;

                return list[RawIndex];
            }
        }

        /// <summary>
        /// 取得指定 index 的項目；index 為 -1 時回傳 current index 的項目
        /// </summary>
        public T GetItemAt(int index)
        {
            if (index < 0)
                return CurrentListItem;

            var list = GetList();
            if (list == null || index >= list.Count)
                return default;
            return list[index];
        }

        public IEnumerable<T> CurrentItems
        {
            get
            {
                var proxy = ProxyVarList;
                if (proxy != null)
                    return proxy.CurrentItems;
                if (HasValueSource)
                    return ValueSourceList ?? Enumerable.Empty<T>();
                EnsureActiveCollectionInitialized();
                if (_activeCollection is IEnumerable<T> enumerable)
                    return enumerable;
                throw new InvalidOperationException(
                    "Active collection is not initialized or of an unknown type."
                );
            }
        }

        public IReadOnlyCollection<T> CurrentCollection
        {
            get
            {
                var proxy = ProxyVarList;
                if (proxy != null)
                    return proxy.CurrentCollection;
                if (HasValueSource)
                    return ValueSourceList ?? (IReadOnlyCollection<T>)Array.Empty<T>();
                EnsureActiveCollectionInitialized();
                if (_activeCollection is IReadOnlyCollection<T> collection)
                    return collection;
                throw new InvalidOperationException(
                    "Active collection is not initialized or of an unknown type."
                );
            }
        }

        public override T1 GetValue<T1>() //希望這個用不到？
        {
            if (typeof(T1) == typeof(List<T>))
            {
                EnsureActiveCollectionInitialized();
                return (T1)(object)GetList();
                // throw new InvalidOperationException("Active collection is not initialized or of an unknown type.");
            }

            return ((AbstractMonoVariable)this).GetValue<T1>();
        }

        public override void SetValueFromVar(AbstractMonoVariable source, Object byWho)
        {
            //好像也用不到？難道會需要抄list? copy?
            throw new NotImplementedException("什麼時候會用到？");
        }

        public List<T> Value => GetList();


        public List<T> GetList()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
                return proxy.GetList();
            if (HasValueSource)
                return ValueSourceList;

            EnsureActiveCollectionInitialized();

            //FIXME: 這裡應該都會GC
            if (_activeCollection is List<T> list)
                return list;

            //FIXME: 會轉型感覺沒效率
            if (_activeCollection is Queue<T> queue)
                return queue.ToList();
            if (_activeCollection is HashSet<T> set)
                return set.ToList();
            throw new InvalidOperationException(
                "Active collection is not initialized or of an unknown type."
            );
        }

        public HashSet<T> GetHashSet()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
                return proxy.GetHashSet();
            EnsureActiveCollectionInitialized();
            if (_activeCollection is HashSet<T> hashSet)
                return hashSet;
            throw new InvalidOperationException(
                "Active collection is not initialized or of an unknown type."
            );
        }

        public Queue<T> GetQueue()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
                return proxy.GetQueue();
            EnsureActiveCollectionInitialized();
            if (_activeCollection is Queue<T> queue)
                return queue;
            throw new InvalidOperationException(
                "Active collection is not initialized or of an unknown type."
            );
        }

        private void EnsureActiveCollectionInitialized()
        {
            // 直接讀 _valueSources 欄位，不走 HasValueSource（它會呼叫 AutoReferenceFieldEditor →
            // Application.isPlaying，序列化期間 OnAfterDeserialize 呼叫會丟 UnityException）
            if (_valueSources is { Length: > 0 })
                return;
            if (
                _activeCollection != null
                && GetCollectionTypeFromInstance(_activeCollection) == _storageType
            )
                return; // Already initialized with the correct type

            switch (_storageType)
            {
                case CollectionStorageType.List:
                    var list = new List<T>();
                    if (SourceList != null)
                        list.AddRange(SourceList);
                    _activeCollection = list;
                    break;
                case CollectionStorageType.Queue:
                    var queue = new Queue<T>();
                    if (SourceList != null)
                        foreach (var item in SourceList)
                            queue.Enqueue(item);
                    _activeCollection = queue;
                    break;
                case CollectionStorageType.HashSet:
                    var hashSet = new HashSet<T>();
                    if (SourceList != null)
                        foreach (var item in SourceList)
                            hashSet.Add(item);
                    _activeCollection = hashSet;
                    break;
                default: // Fallback to List<T>
                    var defaultList = new List<T>();
                    if (SourceList != null)
                        defaultList.AddRange(SourceList);
                    _activeCollection = defaultList;
                    break;
            }
        }

        private CollectionStorageType GetCollectionTypeFromInstance(object collection)
        {
            if (collection is List<T>)
                return CollectionStorageType.List;
            if (collection is Queue<T>)
                return CollectionStorageType.Queue;
            if (collection is HashSet<T>)
                return CollectionStorageType.HashSet;
            // This should not happen if EnsureActiveCollectionInitialized is working correctly
            throw new InvalidOperationException("Unknown collection type in _activeCollection.");
        }

        private Type DetermineRuntimeTypeFromStorage(CollectionStorageType type)
        {
            switch (type)
            {
                case CollectionStorageType.List:
                    return typeof(List<T>);
                case CollectionStorageType.Queue:
                    return typeof(Queue<T>);
                case CollectionStorageType.HashSet:
                    return typeof(HashSet<T>);
                default:
                    return typeof(List<T>);
            }
        }

        // public override void AddListener<T1>(UnityAction<T1> action)
        // {
        //     if (action == null) return;
        //     // This method is not implemented in VarList<T> as it does not support UnityAction<T1> directly.
        //     // If needed, implement a specific listener for the collection type.
        //     throw new NotImplementedException(
        //         "VarList<T> does not support AddListener with UnityAction<T1>. Use specific methods for collection manipulation.");
        // }

        //FIXME: 這裡有給ValueType耶
        //給list? queue的話我Provider根本吃不到？ realtime type還會變...乾
        public override void ResetStateRestore(bool IsHardReset)
        {
            SetRawIndex(_defaultIndex);
            _lastIndex = -1;
            // value source：內容由 source 計算，本地沒有集合可 reset（index 游標已在上面重置）
            // proxy(varRef) 模式：集合與游標都在 parent，由 parent 自己 reset
            if (HasValueSource || ProxyVarList != null)
                return;
            EnsureActiveCollectionInitialized();

            // 清空當前集合
            ClearValue();

            // 如果 backing list 有內容，恢復這些內容
            if (SourceList != null && SourceList.Count > 0)
                switch (_storageType)
                {
                    case CollectionStorageType.List:
                        ((List<T>)_activeCollection).AddRange(SourceList);
                        break;
                    case CollectionStorageType.Queue:
                        var queue = (Queue<T>)_activeCollection;
                        foreach (var item in SourceList)
                            queue.Enqueue(item);
                        break;
                    case CollectionStorageType.HashSet:
                        var hashSet = (HashSet<T>)_activeCollection;
                        foreach (var item in SourceList)
                            hashSet.Add(item);
                        break;
                }

            // 重置索引到預設值
            SetRawIndex(_defaultIndex);

            // 通知變更（Clear() 已經調用過，但如果有恢復內容需要��次通知）
            if (SourceList != null && SourceList.Count > 0)
                OnValueChanged();
        }

        public override void SetRaw<T1>(T1 value, Object byWho)
        {
            if (value is T tValue)
            {
                //... 用的到嗎？
            }
        }

        public override Type ValueType => typeof(List<T>); //_activeCollection?.GetType() ?? DetermineRuntimeTypeFromStorage(_storageType);

        // public override object objectValue => _activeCollection;

        public override Object CurrentRawObject => CurrentListItem as Object;

        public override Object GetRawObjectAt(int index) => GetItemAt(index) as Object;

        // protected void SetValueInternal<T1>(T1 value, Object byWho = null)
        // {
        //     // Base implementation is empty. If specific behavior is needed for setting the whole collection,
        //     // it could be implemented here (e.g., clear and add all from an IEnumerable<T>).
        // }

        public override void Add(object item)
        {
            if (item is T typedItem)
                Add(typedItem);
            else
                throw new InvalidCastException(
                    $"Cannot add item of type {item.GetType()} to VarList<{typeof(T)}>"
                );
        }

        public override void Remove(object item)
        {
            if (item is T typedItem)
                Remove(typedItem);
            else
                throw new InvalidCastException(
                    $"Cannot remove item of type {item.GetType()} from VarList<{typeof(T)}>"
                );
        }

        // public List<T> _list = new(); // This is replaced by _activeCollection and serialization logic

        public void Add(T item)
        {
            var proxy = ProxyVarList;
            if (proxy != null)
            {
                proxy.Add(item);
                return;
            }

            if (HasValueSource)
            {
                Debug.LogError(
                    "VarList 有 computed value source 時為唯讀（內容由 source 計算），無法 Add。", this);
                return;
            }
            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list)
                list.Add(item);
            else if (_activeCollection is Queue<T> queue)
                queue.Enqueue(item);
            else if (_activeCollection is HashSet<T> set)
                set.Add(item);
            else
                throw new InvalidOperationException(
                    "Collection not properly initialized or unknown type."
                );
            OnValueChanged();
        }

        public void SetItemAt(int index, T item)
        {
            var proxy = ProxyVarList;
            if (proxy != null)
            {
                proxy.SetItemAt(index, item);
                return;
            }

            if (HasValueSource)
            {
                Debug.LogError(
                    "VarList 有 computed value source 時為唯讀（內容由 source 計算），無法 SetItemAt。", this);
                return;
            }

            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list)
            {
                if (index < 0 || index >= list.Count)
                {
                    Debug.LogError($"Index {index} out of bounds (Count: {list.Count}).", this);
                    return;
                }

                list[index] = item;
                OnValueChanged();
            }
            else
                throw new NotSupportedException(
                    "SetItemAt is only supported for List storage type."
                );
        }

        public void Remove(T item)
        {
            var proxy = ProxyVarList;
            if (proxy != null)
            {
                proxy.Remove(item);
                return;
            }

            if (HasValueSource)
            {
                Debug.LogError(
                    "VarList 有 computed value source 時為唯讀（內容由 source 計算），無法 Remove。", this);
                return;
            }
            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list)
                list.Remove(item);
            else if (_activeCollection is HashSet<T> set)
                set.Remove(item);
            else if (_activeCollection is Queue<T> queue)
            {
                throw new NotSupportedException(
                    "Remove(T item) is not supported for Queue. Use Dequeue() to remove the item from the front, or manage items by clearing and re-adding if specific item removal is needed."
                );
            }
            else
                throw new InvalidOperationException(
                    "Collection not properly initialized or unknown type."
                );
            OnValueChanged();
        }

        public override void ClearValue()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
            {
                proxy.ClearValue();
                return;
            }

            if (HasValueSource)
            {
                Debug.LogError(
                    "VarList 有 computed value source 時為唯讀（內容由 source 計算），無法 ClearValue。", this);
                return;
            }
            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list)
                list.Clear();
            else if (_activeCollection is Queue<T> queue)
                queue.Clear();
            else if (_activeCollection is HashSet<T> set)
                set.Clear();
            else
                throw new InvalidOperationException(
                    "Collection not properly initialized or unknown type."
                );
            OnValueChanged();
        }

        public float CountFloat => Count;

        [ShowInInspector]
        public override int Count
        {
            get
            {
                var proxy = ProxyVarList;
                if (proxy != null)
                    return proxy.Count;
                if (HasValueSource)
                    return ValueSourceList?.Count ?? 0;

                EnsureActiveCollectionInitialized();
                if (_activeCollection is List<T> list)
                    return list.Count;
                if (_activeCollection is Queue<T> queue)
                    return queue.Count;
                if (_activeCollection is HashSet<T> set)
                    return set.Count;
                return 0;
            }
        }

        public IEnumerable<T> GetItems()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
                return proxy.GetItems();
            if (HasValueSource)
                return ValueSourceList ?? Enumerable.Empty<T>();
            EnsureActiveCollectionInitialized();
            if (_activeCollection is IEnumerable<T> enumerable)
                return enumerable;
            return Enumerable.Empty<T>();
        }

        public T Dequeue()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
                return proxy.Dequeue();

            if (HasValueSource)
            {
                Debug.LogError(
                    "VarList 有 computed value source 時為唯讀（內容由 source 計算），無法 Dequeue。", this);
                return default;
            }
            EnsureActiveCollectionInitialized();
            switch (_activeCollection)
            {
                case Queue<T> queue:
                {
                    var item = queue.Dequeue();
                    OnValueChanged();
                    if (item == null)
                        Debug.LogError(
                            "Dequeue returned null. This may indicate the queue was empty.");
                    return item;
                }
                case List<T> { Count: 0 }:
                    Debug.LogError("Cannot dequeue from an empty List.");
                    return default;
                case List<T> list:
                {
                    var item = list[0];
                    list.RemoveAt(0);
                    OnValueChanged();
                    return item;
                }
                default:
                    throw new InvalidOperationException(
                        "Dequeue is only available if the collection type is Queue."
                    );
            }
        }

        public T Peek()
        {
            var proxy = ProxyVarList;
            if (proxy != null)
                return proxy.Peek();
            EnsureActiveCollectionInitialized();
            if (_activeCollection is Queue<T> queue)
                return queue.Peek();
            throw new InvalidOperationException(
                "Peek is only available if the collection type is Queue."
            );
        }

        public bool Contains(T item)
        {
            var proxy = ProxyVarList;
            if (proxy != null)
                return proxy.Contains(item);
            if (HasValueSource)
                return ValueSourceList?.Contains(item) ?? false;
            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list)
                return list.Contains(item);
            if (_activeCollection is Queue<T> queue)
                return queue.Contains(item);
            if (_activeCollection is HashSet<T> set)
                return set.Contains(item);
            return false;
        }

        // ISerializationCallbackReceiver
        public void OnBeforeSerialize()
        {
            // If _activeCollection has been initialized (is not null), it is the source of truth.
            // We need to update _backingListForSerialization to match it before serialization.
            // If _activeCollection is null, it means it hasn't been initialized yet.
            // In this case, _backingListForSerialization holds the most recent serialized state,
            // so we do nothing and let Unity serialize it as is.
            // if (_activeCollection != null)
            // {
            //     _backingListForSerialization.Clear();
            //     if (_activeCollection is IEnumerable<T> enumerable) _backingListForSerialization.AddRange(enumerable);
            // }
        }

        public void OnAfterDeserialize()
        {
            // _activeCollection will be (re)created from _backingListForSerialization and _storageType
            // This is best done in Awake or OnEnable, or an explicit Init method.
            // For editor-time changes to _storageType to take effect immediately, we can call it here.

            EnsureActiveCollectionInitialized();
            // Debug.Log("[VarList] OnAfterDeserialize called. Initializing active collection." +
            //           _backingListForSerialization.Count);
        }

        // It's good practice to initialize in Awake/OnEnable if this class were a MonoBehaviour.
        // Since it's not, users of this class or an explicit Init() method would handle it.
        // OnAfterDeserialize helps with editor changes.
        // Methods also call EnsureActiveCollectionInitialized() as a safeguard.
    }

    //不想定義型別
    public abstract class AbstractVarList : AbstractMonoVariable, IHierarchyValueInfo
    {
        /// <summary>「沒有選取任何項目」的 current index，例如 GrabSlotHolder 空手時。</summary>
        public const int NoSelectionIndex = -1;

        public override string StringValue => $"Count: {Count}";
        //Count 本身已經處理 value-source / proxy 的轉發，且「空 list」要算沒有值——
        //base 的 source 委派對 List<T> 只判 null，空 list 會被誤判成有值，所以整顆蓋掉。
        public override bool IsValueExist => Count > 0;
        protected override bool IsLocalValueExist => Count > 0;

        // public override Type ValueType => typeof(List<T>);
        // public override object objectValue => _list;
        [ShowInPlayMode]
        public abstract Object CurrentRawObject { get; }

        /// <summary>
        /// 取得指定 index 的項目；index 為 -1 時回傳 current index 的項目
        /// </summary>
        public abstract Object GetRawObjectAt(int index);

        public abstract int CurrentIndex { get; }

        /// <summary>
        /// 是否處於 proxy 模式（內容與 index 由另一顆 VarList 提供：var-resolving value source 或 varRef）。
        /// 供子層 VarIntIndex 判斷 index 該讀 owner.CurrentIndex 還是本地值。
        /// </summary>
        public abstract bool IsProxy { get; }

        public abstract void SetCurrentIndexTo(int index);
        public abstract void GoToNext();
        public abstract void GoToPrevious();

        // protected override void SetValueInternal<T1>(T1 value, Object byWho = null) { }

        public abstract int Count { get; }

        public abstract void Add(object item);

        public abstract void Remove(object item);

        // public abstract void Clear();
        public override string ValueInfo => $"Count: {Count}";
        public override bool IsDrawingValueInfo => true;
    }
}
