using System;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core.Runtime;
using _1_MonoFSM_Core.Runtime.Action.VariableAction;
using Cysharp.Text;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Utilities;
using MonoFSM.EditorExtension;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using MonoFSM.Variable.FieldReference;
using MonoFSM.Variable.TypeTag;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Profiling;
using Object = System.Object;

namespace MonoFSM.Core.DataProvider
{
    public class ValueProvider<T> : ValueProvider, IValueProvider<T>
    {
        public T Value => Get<T>();
        public override Type ValueType => typeof(T);
    }

    //EntityTag可以拿到Schema的話更好？這樣寫什麼都可以了

    //用這顆就夠了，其他應該都不需要了？除了literal
    [Searchable]
    public class ValueProvider
        : AbstractVariableProviderRef,
            IOverrideHierarchyIcon,
            IHierarchyValueInfo,
            IFieldPathRootTypeProvider
    {
        // 遞迴檢測相關變數
        [System.NonSerialized]
        private int _recursionDepth = 0;
        private const int MAX_RECURSION_DEPTH = 50;

        //自引用？
        // [SerializeField] ValueProvider _valueProviderRef;
        //Entity?
        // [DropDownRef] [SerializeField] private ValueProvider _valueProviderRef; //還是是entity想要Ref?

        private Color GetVarTagColor()
        {
            if (!IsDropDownVarTagValid())
                return new Color(0.9f, 0.2f, 0.3f, 0.5f);
            return Color.white;
        }

        //FIXME: 要檢查目前選到的有沒有在dropdown的選項內容裡(換VarEntity後就會錯)
        [Required]
        [PropertyOrder(0)]
        [BoxGroup("Field Path", ShowLabel = true)]
        public VarEntity _varEntity;

        [ShowInDebugMode]
        public MonoEntity debugEntity => _varEntity?.Value;

        [BoxGroup("Field Path", ShowLabel = true)]
        [ShowInInspector]
        [ValueDropdown(nameof(GetVarTagsFromEntity), NumberOfItemsBeforeEnablingSearch = 5)]
        [GUIColor(nameof(GetVarTagColor))] //FIXME: 整合成attribute drawer? 很接近DropdownRef, 不確定泛用嗎
        public VariableTag DropDownVarTag
        {
            set
            {
                if (Application.isPlaying)
                {
                    Debug.LogError(
                        "VarRef: Cannot set varTag in play mode. Please set it in edit mode.",
                        this
                    );
                    return;
                }

                _varTag = value;
            }
            //only editor Set? 另外開？
            get => _varTag;
        }

        public IEnumerable<ValueDropdownItem<VariableTag>> GetVarTagsFromEntity()
        {
            if (Application.isPlaying) //暫時解
                return null;
            if (_varEntity != null)
            {
                if (_varEntity.Value != null)
                {
                    // Debug.Log("ValueProvider: Getting variable tags from assigned entity.", this);
                    return _varEntity.Value.GetVariableTagDropdownItems<AbstractMonoVariable>();
                }

                if (_varEntity.EntityTag != null)
                {
                    // Debug.Log("ValueProvider: Getting variable tags from assigned entity tag.",
                    // this);
                    return _varEntity.EntityTag.GetVariableTagItems();
                }
                else
                {
                    Debug.LogError(
                        "ValueProvider: Assigned VarEntity has no entity tag, returning empty variable tags.",
                        this
                    );
                }
            }

            // Debug.Log(
            //     "ValueProvider: No assigned entity or entity tag, returning empty variable tags.",
            //     this);
            return new List<ValueDropdownItem<VariableTag>>();
            // //FIXME: 不看Parent entity的entity tag嗎？
            // if (entityProvider != null)
            //     if (entityProvider.entityTag == null)
            //     {
            //         return entityProvider.GetVariableTagDropdownItems<AbstractMonoVariable>();
            //         // Debug.LogError("EntityProvider has no entity tag, returning empty variable tags.", this);
            //         return new List<ValueDropdownItem<VariableTag>>();
            //     }
            //
            // return entityProvider?.entityTag?.GetVariableTagItems()
            //     ?? ParentEntity?.GetVarTagOptions();
        }

        public IEnumerable<ValueDropdownItem<AbstractTypeTag>> GetSchemaTypeTagsFromEntity()
        {
            if (_varEntity != null)
                // if (_varEntity.Value != null)
                //     return _varEntity.Value.GetSchemaTypeTagItems();
                if (_varEntity.EntityTag != null)
                    return _varEntity.EntityTag.GetSchemaTypeTagItems();

            // if (entityProvider != null && entityProvider.entityTag != null)
            //     return entityProvider.entityTag.GetSchemaTypeTagItems();

            // 如果沒有 entityProvider，可以考慮從 ParentEntity 獲取
            // 或者返回空列表
            return new List<ValueDropdownItem<AbstractTypeTag>>();
        }

        [ShowInDebugMode]
        // [Required]
        [SerializeField]
        private VariableTag _varTag;

        public void ClearVarTag()
        {
            _varTag = null;
            // Debug.Log("VarRef: Cleared varTag.", this);
        }

        public void ClearSchemaTypeTag()
        {
            _schemaTypeTag = null;
            // Debug.Log("ValueProvider: Cleared schemaTypeTag.", this);
        }

        // private bool TypeCheckFail()
        // {
        //     if (_varTag == null) return false;
        //     return typeof(TValueType).IsAssignableFrom(_varTag._valueFilterType.RestrictType) == false;
        // }

        //代表這個ValueProvider可以拿到對應的AbstractMonoVariable


        [ShowInDebugMode]
        public override object StartingObject => GetTarget();

        [ShowInPlayMode]
        public override AbstractMonoVariable VarRaw //可以去拿MonoEntity的資料？而不是一定要透過Var?
        {
            get
            {
                if (_varEntity != null)
                    return _varEntity.Value?.GetVar(_varTag);
                // if (entityProvider != null) //這個可以是null...hmmm
                //     return entityProvider?.monoEntity?.GetVar(_varTag);

                // if (_monoVariable != null)
                //     return _monoVariable;
                // Debug.LogError("VarRef: No variable found", this);
                return ParentEntity?.GetVar(_varTag); //如果沒有黑板就從parent entity拿
            }
        }

        private new MonoEntity ParentEntity //fixme; 避免這個？ fsm結構裡一定要放this.(ParentEntity)嗎？
        {
            get
            {
                // this.EnsureComponentInParent(ref _parentEntity);
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_parentEntity));
                return _parentEntity;
            }
        }

        public MonoEntity fromEntity //tag?
        {
            get
            {
                if (_varEntity != null)
                    return _varEntity.Value;

                // if (_entityProvider != null)
                //     return _entityProvider.monoEntity;
                return ParentEntity;
            }
        }

        [AutoParent]
        private MonoEntity _parentEntity;

        //可auto? 有ref就不覆蓋？還是身上的比較大？
        //自己也是？
        // [Required]
        // [DropDownRef]
        // [PropertyOrder(0)]
        // [BoxGroup("Field Path", ShowLabel = true)]
        // // [SerializeField]
        // public AbstractEntityProvider _entityProvider;
        //
        // private AbstractEntityProvider entityProvider
        // {
        //     get
        //     {
        //         this.EnsureComponent(ref _entityProvider, false); //不一定需要這個物件
        //         return _entityProvider;
        //     }
        // }


        public string _declarationName; //可以有default name?

        // Schema selection related fields
        [PropertyOrder(0)]
        [BoxGroup("Field Path", ShowLabel = true)]
        [ShowInInspector]
        [ValueDropdown(nameof(GetSchemaTypeTagsFromEntity), NumberOfItemsBeforeEnablingSearch = 5)] //這個dropdown改成attirubet Drawer?
        public AbstractTypeTag DropDownSchemaTypeTag
        {
            set
            {
                if (Application.isPlaying)
                {
                    Debug.LogError(
                        "ValueProvider: Cannot set schemaTypeTag in play mode. Please set it in edit mode.",
                        this
                    );
                    return;
                }

                _schemaTypeTag = value;
            }
            get => _schemaTypeTag;
        }

        [ShowInDebugMode]
        [HideInInspector]
        [SerializeField]
        private AbstractTypeTag _schemaTypeTag;

        public string DeclarationName
        {
            get
            {
                if (!_declarationName.IsNullOrWhitespace())
                    return _declarationName;
                //FIXME: 參考VarEntity的Tag?
                // if (entityProvider?.SuggestDeclarationName != null)
                //     return entityProvider?.SuggestDeclarationName;
                // Debug.Log($"VarRef: Using entityProvider declaration name: {_declarationName}", this);
                return _declarationName;
            }
        }

        public override string Description
        {
            get
            {
                var stringBuilder = ZString.CreateStringBuilder();
                if (!DeclarationName.IsNullOrWhitespace())
                {
                    // Debug.Log($"VarRef: Using declaration name: {DeclarationName}", this);
                    stringBuilder.Append(DeclarationName);
                    // stringBuilder.Append(".");
                }

                if (_varEntity != null && _varEntity.EntityTag != null)
                {
                    //world Entity => world.player
                    //parentEntity => this(Player)
                    // stringBuilder.Append(entityProvider.GetType());
                    // Debug.Log($"VarRef: EntityProvider found: {entityProvider.entityTag}", this);
                    stringBuilder.Append("(");
                    stringBuilder.Append(_varEntity.EntityTag.name.Split("_")[^1]);
                    stringBuilder.Append(")");
                }

                // if (entityProvider != null && entityProvider.entityTag != null)
                // {
                //     //world Entity => world.player
                //     //parentEntity => this(Player)
                //     // stringBuilder.Append(entityProvider.GetType());
                //     // Debug.Log($"VarRef: EntityProvider found: {entityProvider.entityTag}", this);
                //     stringBuilder.Append("(");
                //     stringBuilder.Append(entityProvider.entityTag.name.Split("_")[^1]);
                //     stringBuilder.Append(")");
                // }

                // stringBuilder.Append('.');
                if (varTag != null)
                {
                    if (stringBuilder.Length > 0)
                        stringBuilder.Append('.');
                    stringBuilder.Append(varTag.name.Split("_")[^1]);
                    // stringBuilder.Append('.');
                }

                if (stringBuilder.Length > 0 && HasFieldPath)
                    stringBuilder.Append('.');
                stringBuilder.Append(PropertyPath);
                var final = stringBuilder.ToString();
                //把final裡的_都拿掉
                // final = final.Replace("_", " ");
                return final;
            }
        }

        // public override AbstractMonoVariable VarRaw => _monoVariable;

        [LabelText("最終資料型別")]
        [PreviewInInspector]
        public override Type ValueType =>
            HasFieldPath ? lastPathEntryType : GetValueTypeFromSource();

        private Type GetValueTypeFromSource()
        {
            // 優先級 1: VarTag
            if (_varTag != null)
                return _varTag.ValueType;

            // 優先級 2: Schema Type
            if (_schemaTypeTag != null)
                return _schemaTypeTag.Type;

            // 優先級 3: Entity Type
            return _varEntity?.EntityTag?.RestrictType ?? typeof(MonoEntity);

            // return entityProvider?.entityTag?.RestrictType ?? typeof(MonoEntity);
        }

        [PreviewInDebugMode]
        public string ValueTypeSourceFrom
        {
            get
            {
                if (HasFieldPath)
                    return "Field Path";
                if (_varTag != null)
                    return "Var Tag";
                if (_schemaTypeTag != null)
                    return "Schema Type";
                if (_varEntity != null && _varEntity.EntityTag != null)
                    return "Entity Tag";
                // else if (entityProvider != null && entityProvider.entityTag != null)
                //     return "Entity Tag";
                if (ParentEntity != null)
                    return "Parent Entity";
                return "Unknown";
            }
        }

        //FIXME: 型別有可能和實際不符合嗎？
        //選了VarRaw.Value後反而變成原本的type...這樣外面就沒有提示了

        //FIXME: 拔掉？
        private UnityEngine.Object GetTarget()
        {
            // 優先級 1: VarTag（如果有選擇 Variable）
            if (_varTag != null)
            {
                return VarRaw;
            }

            // 優先級 2 & 3: Schema 模式下不能使用 IValueProvider，返回 Entity
            // Schema 會在 Get<T>() 方法中特殊處理

            if (_varEntity != null)
                return _varEntity.Value;
            // if (entityProvider != null)
            //     return entityProvider.monoEntity;
            return ParentEntity;
        }

        // public override Type GetValueType =>
        // [PropertyOrder(-1)]
        [GUIColor(0.6f, 0.8f, 1f)]
        [PreviewInInspector]
        public override Type GetObjectType
        {
            get
            {
                // 優先級 1: VarTag
                if (_varTag != null)
                    return _varTag.VariableMonoType;

                // 優先級 2: Schema Type
                if (_schemaTypeTag != null)
                    return _schemaTypeTag.Type;

                // 優先級 3: Entity Type (from tag)
                if (_varEntity != null)
                    return _varEntity.EntityTag?._entityType?.RestrictType ?? typeof(MonoEntity);

                // 優先級 4: Parent Entity Type (from instance)
                if (ParentEntity != null)
                    return ParentEntity.GetType();

                // Debug.LogError("VarRef: No target entity, schema, or variable tag found.", this);
                return typeof(object);
            }
        }

        public override VariableTag varTag => _varTag;

        public override TVariable GetVar<TVariable>()
        {
            if (VarRaw is TVariable variable)
                return variable;
            if (VarRaw == null)
            {
                Debug.LogError("VarRef: VarRaw is null, cannot get variable.", this);
            }
            else
                Debug.LogError("VarRef: VarRaw is not of type " + typeof(TVariable), this);

            return null;
        }

        public MonoEntity GetMonoEntity()
        {
            // if (entityProvider != null)
            //     return entityProvider.monoEntity;
            if (_varEntity != null)
                return _varEntity.Value;
            return ParentEntity;
        }

        public AbstractEntitySchema GetSelectedSchema()
        {
            if (_schemaTypeTag == null)
            {
                Debug.LogError("ValueProvider: No schema type selected.", this);
                return null;
            }

            var schemaType = _schemaTypeTag.Type;
            if (schemaType == null)
            {
                Debug.LogError("ValueProvider: Schema type tag has no type.", this);
                return null;
            }

            var entity = GetMonoEntity();
            if (entity == null)
            {
                if (Application.isPlaying)
                    Debug.LogError("ValueProvider: No target entity found for schema.", this);
                return null;
            }


            // 使用反射調用泛型方法
            // var method = typeof(MonoEntity).GetMethod("GetSchema").MakeGenericMethod(schemaType);
            // return (AbstractEntitySchema)method.Invoke(entity, null);
            return entity.GetSchema(_schemaTypeTag.Type);
        }

        public TSchema GetSchema<TSchema>()
            where TSchema : AbstractEntitySchema
        {
            var entity = GetMonoEntity();
            if (entity == null)
            {
                Debug.LogError("VarRef: No target entity found.", this);
                return null;
            }

            return entity.GetSchema<TSchema>();
        }

        /// <summary>
        ///     檢查當前選擇的欄位是否支援設定值
        ///     FIXME: 這個現在還是錯的，跑去拿GetterMember了
        /// </summary>
        // public bool CanSetProperty
        // {
        //     get
        //     {
        //         // Variable 模式：變數都可以設定
        //         if (_varTag != null && !HasFieldPath)
        //             return true;
        //
        //         // 其他模式需要檢查欄位路徑
        //         if (!HasFieldPath)
        //         {
        //             Debug.LogError("ValueProvider: 需要設定欄位路徑才能進行屬性設定。", this);
        //             return false;
        //         }
        //
        //         return CanSetFieldPath();
        //     }
        // }

        /// <summary>
        ///     檢查欄位路徑的最終欄位是否可以設定
        /// </summary>
        // private bool CanSetFieldPath()
        // {
        //     if (_pathEntries == null || _pathEntries.Count == 0)
        //         return false;
        //
        //     var lastEntry = _pathEntries[^1];
        //     var targetType = GetTargetTypeForFieldPath();
        //
        //     if (targetType == null || string.IsNullOrEmpty(lastEntry._propertyName))
        //     {
        //         Debug.LogError("ValueProvider: 無法確定欄位路徑的目標型別或屬性名稱。", this);
        //         return false;
        //     }
        //
        //     // 使用 RefactorSafeNameResolver 查找成員
        //     var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(
        //         targetType,
        //         lastEntry._propertyName
        //     );
        //
        //     if (member is PropertyInfo prop)
        //     {
        //         if (!prop.CanWrite)
        //             Debug.LogError(
        //                 $"ValueProvider: 屬性 {targetType}.{lastEntry._propertyName} 是唯讀的，無法設定值。",
        //                 this
        //             );
        //         return prop.CanWrite;
        //     }
        //
        //     if (member is FieldInfo field)
        //         return !field.IsInitOnly && !field.IsLiteral; // 不是 readonly 和 const
        //
        //     Debug.LogError(
        //         $"ValueProvider: 無法找到 {targetType} 中的成員 {lastEntry._propertyName}，無法設定值。",
        //         this
        //     );
        //     return false;
        // }

        /// <summary>
        ///     取得欄位路徑檢查的目標型別
        /// </summary>
        // private Type GetTargetTypeForFieldPath()
        // {
        //     if (_pathEntries.Count == 1)
        //     {
        //         // 只有一層，直接檢查來源型別
        //         if (_varTag != null)
        //             return _varTag.ValueType;
        //         if (_schemaTypeTag != null)
        //             return _schemaTypeTag.Type;
        //         return GetMonoEntity()?.GetType();
        //     }
        //
        //     // 多層路徑，需要遍歷到倒數第二層
        //     try
        //     {
        //         var target = GetTarget() as object;
        //         if (target == null)
        //             return null;
        //
        //         var currentObj = target; // 明確宣告為 object 型別
        //         for (var i = 0; i < _pathEntries.Count - 1; i++)
        //         {
        //             var entry = _pathEntries[i];
        //             var type = currentObj.GetType();
        //             var getter = ReflectionUtility.GetMemberGetter(type, entry._propertyName);
        //
        //             if (getter != null)
        //                 currentObj = getter(currentObj);
        //             else
        //                 return null;
        //
        //             if (currentObj == null)
        //                 return null;
        //         }
        //
        //         return currentObj.GetType();
        //     }
        //     catch
        //     {
        //         return null;
        //     }
        // }

        // public void SetProperty<T>(T settingValue)
        // {
        //     // 特殊處理：Schema 模式
        //     if (_schemaTypeTag != null && _varTag == null)
        //     {
        //         var schemaInstance = GetSelectedSchema();
        //         if (schemaInstance == null)
        //         {
        //             Debug.LogError("ValueProvider: 無法獲取 Schema 實例進行設定。", this);
        //             return;
        //         }
        //
        //         if (!HasFieldPath)
        //         {
        //             Debug.LogError(
        //                 "ValueProvider: Schema 模式需要設定欄位路徑才能進行屬性設定。",
        //                 this
        //             );
        //             return;
        //         }
        //
        //         // 設定 Schema 欄位值
        //         ReflectionUtility.SetFieldValueFromPath(
        //             schemaInstance,
        //             _pathEntries,
        //             settingValue,
        //             gameObject
        //         );
        //         return;
        //     }
        //
        //     // Variable 模式：如果選擇了變數，直接設定變數值
        //     if (_varTag != null)
        //     {
        //         var variable = VarRaw;
        //         if (variable == null)
        //         {
        //             Debug.LogError("ValueProvider: 變數為 null，無法設定值。", this);
        //             return;
        //         }
        //
        //         if (!HasFieldPath)
        //         {
        //             // 直���設定變數值
        //             variable.SetValue(settingValue, this);
        //             return;
        //         }
        //
        //         // 透過欄位路徑設定變數的特定屬性
        //         ReflectionUtility.SetFieldValueFromPath(
        //             variable,
        //             _pathEntries,
        //             settingValue,
        //             gameObject
        //         );
        //         return;
        //     }
        //
        //     // Entity 模式：直接設定實體的欄位
        //     var target = GetTarget();
        //     if (target == null)
        //     {
        //         Debug.LogError("ValueProvider: 目標實體為 null，無法設定值。", this);
        //         return;
        //     }
        //
        //     if (!HasFieldPath)
        //     {
        //         Debug.LogError(
        //             "ValueProvider: Entity 模式需要設定欄位路徑才能進行屬性設定。",
        //             this
        //         );
        //         return;
        //     }
        //
        //     // 透過欄位路徑設定實體的特定屬性
        //     ReflectionUtility.SetFieldValueFromPath(target, _pathEntries, settingValue, gameObject);
        // }

        //FIXME: 顯示失敗？
        [ShowInDebugMode]
        private object previewObject => Get<object>();

        /// <summary>
        ///     驗證當前選擇的 DropDownVarTag 是否在可用選項中
        /// </summary>
        private bool IsDropDownVarTagValid()
        {
            if (_varTag == null)
                return true; // 沒有選擇任何 VarTag，不算錯誤

            var availableOptions = GetVarTagsFromEntity();
            if (availableOptions == null)
                return false;

            foreach (var option in availableOptions)
                if (option.Value == _varTag)
                    return true;
            _errorMessage =
                $"選擇的變數標籤 '{_varTag?.name}' 不在當前實體的可用選項中。請重新選擇正確的變數標籤或是 assign var entity。";
            return false; // VarTag 不在可用選項中
        }

        // private string _validationErrorMessage;

        /// <summary>
        ///     檢查並更新驗證錯誤訊息
        /// </summary>
        // private bool HasValidationError()
        // {
        //     if (!IsDropDownVarTagValid())
        //     {
        //         _errorMessage = $"選擇的變數標籤 '{_varTag?.name}' 不在當前實體的可用選項中。請重新選擇正確的變數標籤。";
        //         return true;
        //     }
        //
        //     // _validationErrorMessage = "";
        //     return false;
        // }
        protected override bool HasError()
        {
            if (!IsDropDownVarTagValid())
                return true;
            return base.HasError();
        }

        [ShowInDebugMode]
        private string _getValueDebugInfo;

        public override T1 Get<T1>() //GetAs?
        {
            // 遞迴檢測
            _recursionDepth++;
            if (_recursionDepth > MAX_RECURSION_DEPTH)
            {
                Debug.LogError(
                    $"[遞迴檢測] ValueProvider.Get 遞迴深度超過 {MAX_RECURSION_DEPTH}！可能發生循環引用。Target: {GetTarget()}, VarTag: {_varTag}, SchemaTag: {_schemaTypeTag}",
                    this
                );
                Debug.Break();
                _recursionDepth = 0;
                return default;
            }

            try
            {
                // Debug.Log($"ValueProvider: Getting value of type {typeof(T1)}", this); //會無窮迴圈嗎？
                if (ValueType == null)
                    // Debug.LogError("VarRef: ValueType is null, cannot get value.", this);
                    return default;
                if (!typeof(T1).IsAssignableFrom(ValueType))
                {
                    Debug.LogError(
                        $"無法將 {ValueType} 轉換為 {typeof(T1)}，請檢查變數類型或欄位路徑設定。",
                        this
                    );
                    return default;
                }

                // 特殊處理：Schema 模式
                if (_schemaTypeTag != null && _varTag == null)
                {
                    var schemaInstance = GetSelectedSchema();
                    if (schemaInstance == null)
                    {
                        if (Application.isPlaying)
                            Debug.LogError("ValueProvider: 無法獲取 Schema 實例。", this);
                        return default;
                    }

                    // 如果沒有設定欄位路徑，直接返回 Schema 實例
                    if (!HasFieldPath)
                    {
                        if (schemaInstance is T1 schemaValue)
                            return schemaValue;

                        Debug.LogError(
                            $"無法將 Schema {schemaInstance.GetType()} 轉換為 {typeof(T1)}",
                            this
                        );
                        return default;
                    }

                    // 有欄位路徑時，從 Schema 實例中存取欄位
                    Profiler.BeginSample(
                        "ReflectionUtility.GetFieldValueFromPath schemaFieldValue",
                        this
                    );
                    var (schemaFieldValue, info) = ReflectionUtility.GetFieldValueFromPath<T1>(
                        schemaInstance,
                        _pathEntries,
                        gameObject
                    );
                    Profiler.EndSample();

                    // if (schemaFieldValue != null)
                    return schemaFieldValue;

                    return default;
                }

                // 原有的 VarTag 或 Entity 模式
                var target = GetTarget();
                if (target == null)
                    // Debug.LogError("VarRef: 目標變數或實體為 null，無法獲取值。", this);
                    return default;

                // 如果沒有設定欄位路徑，直接回傳變數值
                if (!HasFieldPath)
                {
                    // Debug.Log($"VarRef: 直接從變數取得值: {target}", this);

                    Profiler.BeginSample("ValueProvider.Get No Field Path", this);
                    if (target is AbstractMonoVariable targetVar)
                    {
                        Profiler.EndSample();
                        return targetVar.GetValue<T1>();
                    }

                    //FIXME: 需要這個嗎？
                    if (target is T1 tObj)
                    {
                        Profiler.EndSample();
                        return tObj;
                    }

                    Profiler.EndSample();

                    return default;
                }

                // 不選varTag的話就用Entity?
                // 使用欄位路徑存取特定欄位值
                Profiler.BeginSample("ReflectionUtility.GetFieldValueFromPath _pathEntries", this);
                // Debug.Log("GetValue from target" + target, target);
                var (fieldValue, infoo) = ReflectionUtility.GetFieldValueFromPath<T1>(
                    target,
                    _pathEntries,
                    gameObject
                );
                _getValueDebugInfo = infoo;
                Profiler.EndSample();
                //TODO：看info? fieldValue == null 會有gc
                return fieldValue;
            }
            finally
            {
                _recursionDepth--;
            }
        }

        public string IconName => "Linked@2x";
        public bool IsDrawingIcon => true;
        public Texture2D CustomIcon => null;
        public string ValueInfo => $"{DeclarationName}"; //一play會call這個...

        // public string ValueInfo => $"{ValueType.Name} {DeclarationName}"; //一play會call這個...
        public bool IsDrawingValueInfo => true;

        protected override string DescriptionTag
        {
            get
            {
                //Value, Var, Schema, Entity
                if (HasFieldPath)
                    return "Value";
                if (_varTag != null)
                    return "Var";
                if (_schemaTypeTag != null)
                    return "Schema";
                if (_varEntity != null)
                    return "Entity";
                return "Unknown";
            }
        }
    }
}
