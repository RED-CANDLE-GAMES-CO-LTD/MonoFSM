using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.Variable
{
    public class VarListEntity : VarList<MonoEntity> //這個變
    {
    }

    public class VarList<T> : AbstractVarList, ISerializationCallbackReceiver
    {
        
        public enum CollectionStorageType
        {
            List,
            Queue,
            HashSet
        }
        
        [SerializeField] [ShowInInspector] [Tooltip("Determines the underlying collection type used.")]
        private CollectionStorageType _storageType = CollectionStorageType.List;

        //FIXME: 好像也不需要這個？runtime用而已？ 不一定
        
        [SerializeField] // This will be used by Unity for serialization
        protected List<T> _backingListForSerialization = new();

        [ShowInPlayMode]
        private object _activeCollection; // Runtime instance: List<T>, Queue<T>, or HashSet<T>

        public int _currentIndex = 0;

        public override void SetIndex(int index)
        {
            if (index < 0 || index >= Count)
            {
                Debug.LogError($"Index {index} is out of bounds for the collection of size {Count}.");
                return;
            }
            _currentIndex = index;
            // Debug.Log("Setting current index to: " + index + " CurrentObj: " + CurrentObj);
        }

        public T CurrentObj
        {
            get
            {
                if (_currentIndex < 0 || _currentIndex >= Count)
                {
                    Debug.LogError("Current index is out of bounds.");
                    return default;
                }

//FIXME: 只有list可以有這個？
                return GetList()[_currentIndex];
            }
        }

        public IEnumerable<T> CurrentItems
        {
            get
            {
                EnsureActiveCollectionInitialized();
                if (_activeCollection is IEnumerable<T> enumerable) return enumerable;
                throw new InvalidOperationException("Active collection is not initialized or of an unknown type.");
            }
        }

        public IReadOnlyCollection<T> CurrentCollection
        {
            get
            {
                EnsureActiveCollectionInitialized();
                if (_activeCollection is IReadOnlyCollection<T> collection) return collection;
                throw new InvalidOperationException("Active collection is not initialized or of an unknown type.");
            }
        }

        public override T1 GetValue<T1>()
        {
            if (typeof(T1) == typeof(List<T>))
            {
                EnsureActiveCollectionInitialized();
                return (T1)(object)GetList();
                // throw new InvalidOperationException("Active collection is not initialized or of an unknown type.");
            }

            return ((AbstractMonoVariable)this).GetValue<T1>();
        }

        public List<T> GetList()
        {
            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list) return list;
            if (_activeCollection is Queue<T> queue) return queue.ToList();
            if (_activeCollection is HashSet<T> set) return set.ToList();
            throw new InvalidOperationException("Active collection is not initialized or of an unknown type.");
        }

        public HashSet<T> GetHashSet()
        {
            EnsureActiveCollectionInitialized();
            if (_activeCollection is HashSet<T> hashSet) return hashSet;
            throw new InvalidOperationException("Active collection is not initialized or of an unknown type.");
        }

        public Queue<T> GetQueue()
        {
            EnsureActiveCollectionInitialized();
            if (_activeCollection is Queue<T> queue) return queue;
            throw new InvalidOperationException("Active collection is not initialized or of an unknown type.");
        }
        
        
        private void EnsureActiveCollectionInitialized()
        {
            if (_activeCollection != null &&
                GetCollectionTypeFromInstance(_activeCollection) ==
                _storageType) return; // Already initialized with the correct type

            switch (_storageType)
            {
                case CollectionStorageType.List:
                    var list = new List<T>();
                    if (_backingListForSerialization != null) list.AddRange(_backingListForSerialization);
                    _activeCollection = list;
                    break;
                case CollectionStorageType.Queue:
                    var queue = new Queue<T>();
                    if (_backingListForSerialization != null)
                        foreach (var item in _backingListForSerialization)
                            queue.Enqueue(item);
                    _activeCollection = queue;
                    break;
                case CollectionStorageType.HashSet:
                    var hashSet = new HashSet<T>();
                    if (_backingListForSerialization != null)
                        foreach (var item in _backingListForSerialization)
                            hashSet.Add(item);
                    _activeCollection = hashSet;
                    break;
                default: // Fallback to List<T>
                    var defaultList = new List<T>();
                    if (_backingListForSerialization != null) defaultList.AddRange(_backingListForSerialization);
                    _activeCollection = defaultList;
                    break;
            }
        }

        private CollectionStorageType GetCollectionTypeFromInstance(object collection)
        {
            if (collection is List<T>) return CollectionStorageType.List;
            if (collection is Queue<T>) return CollectionStorageType.Queue;
            if (collection is HashSet<T>) return CollectionStorageType.HashSet;
            // This should not happen if EnsureActiveCollectionInitialized is working correctly
            throw new InvalidOperationException("Unknown collection type in _activeCollection.");
        }

        private Type DetermineRuntimeTypeFromStorage(CollectionStorageType type)
        {
            switch (type)
            {
                case CollectionStorageType.List: return typeof(List<T>);
                case CollectionStorageType.Queue: return typeof(Queue<T>);
                case CollectionStorageType.HashSet: return typeof(HashSet<T>);
                default: return typeof(List<T>);
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
        public override Type ValueType =>
            typeof(List<T>); //_activeCollection?.GetType() ?? DetermineRuntimeTypeFromStorage(_storageType);
        public override object objectValue => _activeCollection;

        public override Object CurrentRawObject => CurrentObj as Object;

        protected override void SetValueInternal<T1>(T1 value, Object byWho = null)
        {
            // Base implementation is empty. If specific behavior is needed for setting the whole collection,
            // it could be implemented here (e.g., clear and add all from an IEnumerable<T>).
        }

        public override void Add(object item)
        {
            if (item is T typedItem)
                Add(typedItem);
            else
                throw new InvalidCastException($"Cannot add item of type {item.GetType()} to VarList<{typeof(T)}>");
        }

        public override void Remove(object item)
        {
            if (item is T typedItem)
                Remove(typedItem);
            else
                throw new InvalidCastException(
                    $"Cannot remove item of type {item.GetType()} from VarList<{typeof(T)}>");
        }

        // public List<T> _list = new(); // This is replaced by _activeCollection and serialization logic

        public void Add(T item)
        {
            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list) list.Add(item);
            else if (_activeCollection is Queue<T> queue) queue.Enqueue(item);
            else if (_activeCollection is HashSet<T> set) set.Add(item);
            else throw new InvalidOperationException("Collection not properly initialized or unknown type.");
            OnValueChanged();
        }

        public void Remove(T item)
        {
            
            EnsureActiveCollectionInitialized();
            var removed = false;
            if (_activeCollection is List<T> list) removed = list.Remove(item);
            else if (_activeCollection is HashSet<T> set) removed = set.Remove(item);
            else if (_activeCollection is Queue<T>)
                throw new NotSupportedException(
                    "Remove(T item) is not supported for Queue. Use Dequeue() to remove the item from the front, or manage items by clearing and re-adding if specific item removal is needed.");
            else throw new InvalidOperationException("Collection not properly initialized or unknown type.");
            OnValueChanged();
        }

        public override void Clear()
        {
          
            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list) list.Clear();
            else if (_activeCollection is Queue<T> queue) queue.Clear();
            else if (_activeCollection is HashSet<T> set) set.Clear();
            else throw new InvalidOperationException("Collection not properly initialized or unknown type.");
            OnValueChanged();
        }

        public float CountFloat => Count;
        
        public override int Count
        {
            get
            {
                EnsureActiveCollectionInitialized();
                if (_activeCollection is List<T> list) return list.Count;
                if (_activeCollection is Queue<T> queue) return queue.Count;
                if (_activeCollection is HashSet<T> set) return set.Count;
                return 0;
            }
        }

        public IEnumerable<T> GetItems()
        {
            EnsureActiveCollectionInitialized();
            if (_activeCollection is IEnumerable<T> enumerable) return enumerable;
            return Enumerable.Empty<T>();
        }

        public T Dequeue()
        {
            EnsureActiveCollectionInitialized();
            if (_activeCollection is Queue<T> queue)
            {
                var item = queue.Dequeue();
                OnValueChanged();
                return item;
            }
            
            throw new InvalidOperationException("Dequeue is only available if the collection type is Queue.");
            
        }

        public T Peek()
        {
            EnsureActiveCollectionInitialized();
            if (_activeCollection is Queue<T> queue) return queue.Peek();
            throw new InvalidOperationException("Peek is only available if the collection type is Queue.");
        }

        public bool Contains(T item)
        {
            EnsureActiveCollectionInitialized();
            if (_activeCollection is List<T> list) return list.Contains(item);
            if (_activeCollection is Queue<T> queue) return queue.Contains(item);
            if (_activeCollection is HashSet<T> set) return set.Contains(item);
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
    public abstract class AbstractVarList : AbstractMonoVariable
    {
        public override bool IsValueExist => Count > 0;
        // public override Type ValueType => typeof(List<T>);
        // public override object objectValue => _list;
        [ShowInPlayMode]
        public abstract Object CurrentRawObject { get; }
        public abstract void SetIndex(int index);
        protected override void SetValueInternal<T1>(T1 value, Object byWho = null)
        {
        }

        public abstract int Count { get; }


        public abstract void Add(object item);


        public abstract void Remove(object item);

        public abstract void Clear();
    }
}

