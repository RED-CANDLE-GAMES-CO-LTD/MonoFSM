using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoFSM.Core.Runtime;
using _1_MonoFSM_Core.Runtime.LifeCycle.Update;
using _1_MonoFSM_Core.Runtime.MonoData;
using Fusion.Addons.FSM;
using MonoDebugSetting;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Runtime.Mono;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSM.Variable.FieldReference;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Runtime
{
    public interface IMonoAddToBinderChecker //network想要看authority來決定要不要加到字典裡...這個性質是什麼
    {
        bool IsAddValid();
    }

    //FIXME: 必定需要MonoObj?
    // [SelectionBase]
    // [RequireComponent(typeof(MonoObj))]
    [Searchable]
    [FormerlyNamedAs("MonoDescriptable")]
    public class MonoEntity
        : AbstractMonoDescriptable<GameData>, //不需要這層？
            IInstantiated,
            IBeforePrefabSaveCallbackReceiver,
            // IGameDataProvider,
            IValueProvider,
            IAnimatorProvider,
            IValueOfKey<MonoEntityTag> //這樣data也要一直繼承，好ㄇ...

    {
        //hmm view還是不同步？只有state需要，另外寫view?
        //effect感覺需要同步？要不然debug會看不懂？
        //install後有個搬移的過程？但這樣資料就會不同步了(ex: override)

        private void Awake()
        {
            BindModulePackFolders(); //FIXME: 可以在這嗎？
        }


        [AutoChildren]
        private ViewRoot _viewRoot;

        public ViewRoot ViewRoot => _viewRoot;

        // public void MountViewRoot(Transform mountPoint)
        // {
        //     if (_viewRoot != null)
        //         _viewRoot.SetFollowTarget(mountPoint);
        // }

        public void UnmountViewRoot()
        {
            if (_viewRoot != null)
                _viewRoot.ClearFollowTarget();
        }

        [AutoChildren]
        private StateMachineLogic _fsmLogic;

        public StateMachineLogic FsmLogic
        {
            get
            {
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_fsmLogic));
                return _fsmLogic;
            }
        }
        //FIXME: nested? MonoEntity dictionary?
        public void OnInstantiated(WorldUpdateSimulator world)
        {
            //network要看authoring... network版的？啥？ NetworkMonoDescriptableBinder?
            //想要註冊世界了，顆顆
            //掉在外面就不能註冊了，binder是不是不好？
            //從world去bind?
            //FIXME: 哪些要註冊？localplayer才需要？
            _worldBinder = world.GetComponent<MonoEntityBinder>();
            if (_worldBinder)
            {
                // Debug.Log("Registering MonoEntity to WorldBinder: " + name, this);
                _worldBinder.Add(DefaultTag, this); //註冊法
            }
            else
                Debug.LogError(
                    "MonoDescriptableBinder not found in parent, cannot register to world binder",
                    this
                );
            // GetComponent<MonoDescriptableBinder>().Add(DescriptableTag, this);
        }

        [PreviewInInspector]
        MonoEntityBinder _worldBinder;

        public void OnBeforePrefabSave()
        {
            //FIXME :會漏掉嗎？
            FillVarTagsToMonoDescriptableTag();
            FillSchemaTypesToMonoEntityTag();
            BindModulePackFolders();
        }

        //撈出所有變數的tag和schema類型塞到 DescriptableTags
        protected void FillSchemaTypesToMonoEntityTag()
        {
            var descriptableTag = DefaultTag;
            if (descriptableTag == null)
                return;

            var isTagDirty = false;

            // 填充 Schema Types - 先確保 _schemaFolder 存在
            // this.EnsureComponentInChildren(ref _schemaFolder);
            AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_schemaFolder));
            if (_schemaFolder != null)
            {
                var schemas = _schemaFolder.GetValues;
                foreach (var schema in schemas)
                {
                    // 確保 Schema 有 _typeTag
                    schema.AutoAssignTypeTag();

                    if (schema._typeTag == null)
                    {
                        Debug.LogWarning(
                            $"Schema {schema.GetType().Name} 無法自動指派 TypeTag",
                            schema
                        );
                        continue;
                    }

                    Debug.Log($"Found schema with TypeTag: {schema._typeTag.name}");

                    // 檢查是否已經存在相同的 TypeTag
                    var schemaTypeTagExists = descriptableTag.containsSchemaTypeTags.Contains(
                        schema._typeTag
                    );

                    if (!schemaTypeTagExists)
                    {
                        // 使用 Schema 的 _typeTag 自動添加到 MonoEntityTag
                        descriptableTag.containsSchemaTypeTags.Add(schema._typeTag);
                        isTagDirty = true;
                        Debug.Log(
                            $"自動添加 Schema TypeTag: {schema._typeTag.name} 到 MonoEntityTag"
                        );
                    }
                    else
                    {
                        Debug.Log(
                            $"Schema TypeTag {schema._typeTag.name} 已存在於 MonoEntityTag 中"
                        );
                    }
                }
            }

#if UNITY_EDITOR
            if (isTagDirty)
            {
                EditorUtility.SetDirty(descriptableTag);
                Debug.Log($"已自動更新 {descriptableTag.name} 的 Schema Types");
            }
#endif
        }

        // public GameData GameData => Data;

        public T1 Get<T1>()
        {
            if (this is T1 t1)
                return t1;

            Debug.LogError($"Cannot cast {GetType()} to {typeof(T1)}", this);
            return default;
        }

        public Type ValueType => GetType();
        public string Description { get; }
        public Animator ChildAnimator => GetComponentInChildren<Animator>();

        public Animator[] ChildAnimators => GetComponentsInChildren<Animator>();

        public TSchema GetSchema<TSchema>()
            where TSchema : AbstractEntitySchema
        {
            return GetSchema(typeof(TSchema)) as TSchema;
        }

        public AbstractEntitySchema GetSchema(Type type)
        {
            if (SchemaFolder != null)
            {
                var result = SchemaFolder.Get(type);
                if (result == null)
                {
                    if (RuntimeDebugSetting.IsDebugMode)
                        Debug.LogError(
                            $"MonoEnity GetSchema Schema {type} not found in SchemaFolder of {name}",
                            this);
                }

                return result;
            }

            Debug.LogError($"Schema {type} not found in {name}", this);
            return null;
        }
    }

    //描述物件的monoNode, Entity? MonoEntity?
    //場景物件、角色、
    //應該要可以繼承這個嗎？Inventory
    //不該有variable嗎？
    //FIXME: 這層多餘嗎？
    public class AbstractMonoDescriptable<TMonoDescriptable>
        : MonoBlackboard,
            // IMonoDescriptable,
            ISceneAwake,
            ISceneStart
        where TMonoDescriptable : GameData //,IVariableOwner //VariableOwner?
    {
        //FIXME: 更複雜的描述組合？
        // [UsedImplicitly] //從UI直接選
        // public virtual string RuntimeDescription =>
        //     string.IsNullOrEmpty(Data.Description) ? Data.name : Data.Description;

        // #if UNITY_EDITOR
        //         [RequiredIn(PrefabKind.InstanceInScene)] [PreviewInInspector] [AutoParent]
        //         private MonoDescriptableBinder _binder;
        // #endif

        //GameLogic不該Nested?
        //FIXME: 太深了...會包到過多的東西
        // [PreviewInInspector]
        [ShowInDebugMode]
        [AutoChildren]
        private GeneralEffectDealer[] _dealers; //可以互動的性質門

        // private HashSet<GeneralEffectType> _dealerTypeSet = new HashSet<GeneralEffectType>(); //可以被互動的性質

        private readonly Dictionary<GeneralEffectType, GeneralEffectDealer> _dealerTypeMap = new(); //keys?
#if UNITY_EDITOR

        [ShowInDebugMode]
        private List<GeneralEffectType> DealerTypes => _dealerTypeMap.Keys.ToList();

        [ShowInDebugMode]
        private List<GeneralEffectType> ReceiverTypes => _receiverTypeMap.Keys.ToList();
#endif

        [ShowInDebugMode]
        private int DealerSetCount => _dealerTypeMap.Count;

        /// <summary>
        ///     直接讓兩個entity觸發作用
        /// </summary>
        /// <param name="effectType"></param>
        /// <param name="target"></param>
        // public void ApplyEffectTo(GeneralEffectType effectType, MonoEntity target)
        // {
        //     //FIXME: 可以保證一個entity下只有一個type的Dealer嗎？不一定吧？但receiver可以？ 還是拿到list?
        //     if (target == null)
        //     {
        //         Debug.LogError("Target is null", this);
        //         return;
        //     }
        //
        //     var dealer = GetDealer(effectType);
        //     var receiver = target.GetReceiver(effectType);
        //     receiver.ForceDirectEffectHit(dealer,target.gameObject);
        // }

        [ShowInDebugMode]
        [AutoChildren]
        private GeneralEffectReceiver[] _receivers; //可以互動的性質門

        // readonly HashSet<GeneralEffectType> _receiverTypeSet = new HashSet<GeneralEffectType>(); //可以被互動的性質
        private readonly Dictionary<GeneralEffectType, GeneralEffectReceiver> _receiverTypeMap =
            new();

        public GeneralEffectReceiver[] AllReceivers => _receivers;

        [ShowInDebugMode]
        private int ReceiverSetCount => _receiverTypeMap.Count;

        //帶有xx性質的物件
        public bool HasReceiverType(GeneralEffectType effectType)
        {
            return _receiverTypeMap.ContainsKey(effectType);
            // return _receiverTypeSet.Contains(effectType);
        }

        public bool HasDealerType(GeneralEffectType effectType)
        {
            return _dealerTypeMap.ContainsKey(effectType);
            // return _dealerTypeSet.Contains(effectType);
        }

        public GeneralEffectDealer GetDealer(GeneralEffectType effectType)
        {
            if (_dealerTypeMap.TryGetValue(effectType, out var dealer) == false)
            {
                Debug.LogError($"Dealer {effectType} not found in {name}", this);
                return null;
            }

            return dealer;
        }

        public GeneralEffectReceiver GetReceiver(GeneralEffectType effectType)
        {
            if (effectType == null)
            {
                Debug.LogError("EffectType is null", this);
                return null;
            }
            if (_receiverTypeMap.TryGetValue(effectType, out var receiver) == false)
            {
                Debug.LogError($"Receiver \"{effectType}\" not found in {name}", this);
                return null;
            }

            return receiver;
        }

        // public DescriptableData SampleData;
        //FIXME: 不一定需要data? VarGameData比較對？這樣就往下直接找，code就從schema define? 還是其實 MonoEntity要和Schema合併
        // [SOConfig("10_Flags/GameData")]
        // [SerializeField]
        // protected TMonoDescriptable data; //config

        // public virtual IDescriptableData Descriptable => data;
        //
        // public T GetData<T>()
        //     where T : GameData
        // {
        //     return data as T;
        // }

        // [ShowInInspector]
        // public TMonoDescriptable Data
        // {
        //     get => data;
        //     // set => data = value;
        // }

        // [SerializeField] private MonoDescriptableTag[] DescriptableTags; //

        public virtual void OnUIEventReceived() //FIXME; 這啥XD
        {
            Debug.Log("UI Event Received", this);
        }

        // public object GetValue(VariableTag varTag)
        // {
        //     var variable = VariableFolder.GetVariable(varTag);
        //     if (variable == null)
        //     {
        //         Debug.LogError($"Variable {varTag} not found in {name}", this);
        //         return null;
        //     }
        //
        //     return variable.objectValue;
        // }


        // public int GetIntValue(VariableTypeTag typeTag)
        // {
        //     return (_variableFolder.GetVariable(typeTag) as VariableInt).CurrentValue;
        // }
        //
        // public float GetFloatValue(VariableTypeTag typeTag)
        // {
        //     return (_variableFolder.GetVariable(typeTag) as VariableFloat).CurrentValue;
        // }
        //
        // public bool GetBoolValue(VariableTypeTag typeTag)
        // {
        //     return (_variableFolder.GetVariable(typeTag) as VariableBool).CurrentValue;
        // }
        //往下找variable?
        //任何型別呢？

        public Dictionary<string, Func<IMonoDescriptable, object>> propertyCache = new();

        public Func<IMonoDescriptable, object> GetPropertyCache(string propertyName)
        {
            if (propertyCache.TryGetValue(propertyName, out var info))
                return info;

            var propertyInfo = GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            // Debug.Log($"Property {propertyName} found in {sourceObject.GetType()}", sourceObject);

            if (propertyInfo == null)
            {
                propertyCache[propertyName] = null;
                //FIXME: 可能因為unknownData所以有可能會找不到 有點危險？
                // Debug.LogError($"Property {propertyName} not found in {GetType()}");
                return null;
            }

            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
            {
                Debug.LogError($"Property {propertyName} does not have a getter in {GetType()}");
                return null;
            }

            Func<IMonoDescriptable, object> _getMyProperty = (source) =>
                getMethod.Invoke(source, null);
            propertyCache[propertyName] = _getMyProperty;
            return _getMyProperty;
        }

        public MonoEntityTag Key => DefaultTag;

        public MonoEntityTag[] GetKeys()
        {
            return DescriptableTags.ToArray();
        }

        public void EnterSceneAwake()
        {

            // _receiverTypeSet = new HashSet<GeneralEffectType>();
            // Debug.Log("EnterSceneAwake: " +name,this); //跑兩次？

            //FIXME 不要用 type map
            if (_receivers != null)
                foreach (var receiver in _receivers)
                {
                    // _receiverTypeSet.Add(receiver.EffectType);
                    if (!_receiverTypeMap.TryAdd(receiver._effectType, receiver))
                    {
                        if (RuntimeDebugSetting.IsDebugMode)
                            Debug.Log("Receiver type already exists" + receiver._effectType,
                                receiver);
                    }
                }
            //
            foreach (var dealer in _dealers)
            {
                if (dealer._effectType == null)
                {
                    Debug.LogError($"Dealer {dealer.name} has null EffectType", dealer);
                    continue;
                }

                if (_dealerTypeMap.TryAdd(dealer._effectType, dealer) == false)
                {
                    if (RuntimeDebugSetting.IsDebugMode)
                        Debug.LogWarning($"Dealer {dealer._effectType} already exists", dealer);
                }

            }

        }

        public void EnterSceneStart()
        {
            //FIXME: 放這嗎？

        }

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private MonoModuleFolder _monoPackFolder;

        /// <summary>
        /// 將所有 MonoModulePack 中的 Folder 自動綁定到 MonoEntity 的 Folder 作為 external sources
        /// </summary>
        [Button]
        protected void BindModulePackFolders()
        {
            if (_monoPackFolder == null || _monoPackFolder.ModulePacks == null ||
                _monoPackFolder.ModulePacks.Length == 0) return;

            // 撈出 MonoEntity 直屬的所有 folder (不包含 ModulePack 下的)
            var entityFolders = GetEntityFolders();
            //先全部清掉
            foreach (var folder in entityFolders)
            {
                folder.ClearExternalSources();
            }

            foreach (var pack in _monoPackFolder.ModulePacks)
            {
                if (pack == null) continue;
                if (pack.GetComponentInParent<MonoEntity>() != this) //有別人就不算
                    continue;

                foreach (var sourceFolder in pack.GetAllFolders())
                {
                    if (sourceFolder == null) continue;

                    // 找到相同類型的 target folder
                    var sourceType = sourceFolder.GetType();
                    foreach (var targetFolder in entityFolders)
                    {
                        if (targetFolder == null)
                        {
                            Debug.LogError("Target folder is null", this);
                            continue;
                        }


                        if (targetFolder.GetType() == sourceType &&
                            targetFolder != sourceFolder)
                        {
                            targetFolder.AddExternalSource(sourceFolder);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 取得 MonoEntity 直屬的所有 MonoDictFolder (排除 ModulePack 下的)
        /// </summary>
        private IMonoDictFolder[] GetEntityFolders()
        {
            // 用 cast 把已知的 folder 收集起來
            var folders = new List<IMonoDictFolder>();
            if (VariableFolder is IMonoDictFolder vf) folders.Add(vf);
            if (StateFolder is IMonoDictFolder sf) folders.Add(sf);
            if (EffectDetectable is IMonoDictFolder ed) folders.Add(ed);
            if (SchemaFolder is IMonoDictFolder scf) folders.Add(scf);
            return folders.ToArray();
        }

        public IEnumerable<ValueDropdownItem<VariableTag>> GetVarTagOptions()
        {
            var tagDropdownItems = new List<ValueDropdownItem<VariableTag>>();
            foreach (var variable in VariableFolder.GetValues)
                tagDropdownItems.Add(
                    new ValueDropdownItem<VariableTag>(variable.name, variable._varTag)
                );
            return tagDropdownItems;
        }

        //繼承MonoDescriptable的class，可以透過這個方法來將所有的variable field mapping到VariableFolder
        private FieldInfo[] _variableFields;

        //撈出所有變數的tag塞到 DescriptableTags
        protected void FillVarTagsToMonoDescriptableTag()
        {
            var descriptableTag = DefaultTag;
            if (descriptableTag == null)
                return;

            var isTagDirty = false;

            // 填充 Variable Tags
            if (VariableFolder != null)
            {
                var variables = VariableFolder.Collections;
                foreach (var variable in variables)
                    if (!descriptableTag.containsVariableTypeTags.Contains(variable._varTag))
                    {
                        descriptableTag.containsVariableTypeTags.Add(variable._varTag);
                        isTagDirty = true;
                    }
            }

#if UNITY_EDITOR
            if (isTagDirty)
            {
                EditorUtility.SetDirty(descriptableTag);
                Debug.Log($"已自動更新 {descriptableTag.name} 的 Variable Tags");
            }
#endif
        }

        //FIXME: 好像不需要了？要繼承 MonoEntity 才需要
        [Button]
        private void FieldMapping()
        {
            //find all fields which inherit from AbstractMonoVariable
            //Check the value is not null
            //FIXME: 用名字mapping, 不好，直接用tag map, 沒有配到表示要生variable之類的

            _variableFields = GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.FieldType.IsSubclassOf(typeof(AbstractMonoVariable)))
                .ToArray();
            _variableFields.ForEach(field =>
            {
                //FIXME: 要加Type嗎...
                var fieldName = field.Name; //$"[{field.FieldType.Name}] {field.Name}";
                //把空白,_拿掉好了
                fieldName = fieldName.Replace(" ", "").Replace("_", "");
                //FIXME: 模糊搜尋？
                Debug.Log("fieldNameTarget: " + fieldName);
                var variable = VariableFolder.GetVariable(fieldName);
                if (variable != null)
                {
                    Debug.Log($"Set {fieldName} to {variable}", this);
                    field.SetValue(this, variable);
                }
                else
                {
                    Debug.Log("all variables count:" + VariableFolder.GetValues.Count);
                    VariableFolder.GetValues.ForEach(v =>
                        Debug.Log(v._varTag.GetStringKey, v._varTag)
                    );
                    Debug.LogError($"{fieldName} not found", this);
                }
                // var value = field.GetValue(this) as AbstractMonoVariable;
                // if (value == null)
                // {
                //     Debug.LogError($"Field {field.Name} is null", this);
                // }
            });
        }


    }
}
