using System;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Variable
{
    //指向外部
    //需要再定義更細的class嗎？還是MonoDescriptable就夠了
    //最常用的Variable? MonoDescriptable下也會有MonoDescriptable
    //FIXME: 回到pool後，reference要清掉？還是是detector的責任？

    /// <summary>
    ///     指向一顆 <see cref="MonoEntity" /> 的變數，是所有「跨物件取值」的入口：
    ///     把 VarEntity 當 parent，底下掛 VarFloat / VarBool 等子 var（只填 _varTag），
    ///     子 var 就會去那顆 entity 的 VariableFolder 撈同 tag 的 var。
    ///     值的來源有三條，順序是 **子節點 value source（GlobalEntitySource / VarEntityRef …）
    ///     → parent VarEntity proxy（靠自己的 _varTag）→ _defaultValue**，
    ///     見 <c>GenericUnityObjectVariable.GetValueInternal</c>。
    ///     ⚠ 只要子節點掛了任何一顆 value source，_defaultValue 那條 fallback 就吃不到了 ——
    ///     「由程式在 runtime 用 SetOverrideDefaultValue 塞值」的 VarEntity（例如 GameCore 的
    ///     <c>[Var] playerBrain</c> 由 FusionPlayerObject 塞本機 brain）**底下不能有 value source**。
    ///     <see cref="_monoEntityTag" /> 只是限定型別與自動命名用，**不會自己去解值**。
    /// </summary>
    [FormerlyNamedAs("VarBlackboard")]
    public class VarEntity : GenericUnityObjectVariable<MonoEntity>
    {

        // [HideIf(nameof(HasValueProvider))]
        [FormerlySerializedAs("_MonoDescriptableTag")]
        [SOConfig("10_Flags/VarMono")]
        [BoxGroup("定義型別")]
        //FIXME: 這用了感覺就...沒彈性了？，限定schema如何？感覺在做差不多的事？[PropertyOrder(-1)]
        [SerializeField]
        private MonoEntityTag _monoEntityTag; //FIXME: Expected MonoEntityTag, but can be null?

        protected override void Rename()
        {
            if (_monoEntityTag != null && HasProxySource)
                name = $"Get<{_monoEntityTag.name}>";
            else
                base.Rename();
        }
        //FIXME: 好像會需要getter喔，從source來的話

        [PreviewInInspector]
        public MonoEntityTag EntityTag
        {
            get
            {
                var isProxy = HasValueSource;
                // Debug.Log("Get EntityTag from VarEntity isProxy:" + isProxy);
                if (isProxy && valueSource is IEntityValueProvider entityValueSource)
                    return entityValueSource.entityTag;
                // return valueSource.EntityTag; //hmm 怎麼額外定義QQ
                return _monoEntityTag;
            }
        }

        // [PreviewInInspector]
        // [AutoChildren(DepthOneOnly = true)]
        // [CompRef]
        // private IEntityValueProvider _entityValueSource;

        //         [BoxGroup("定義型別")]
        //         [PropertyOrder(-1)]
        //         [PreviewInInspector]
        //         public GameData SampleData
        // #if UNITY_EDITOR
        //             => _monoEntityTag ? _monoEntityTag.SamepleData : null;
        // #else
        //             => null;
        // #endif

        //FIXME: 要用T? VarComponent?

        //FIXME: 什麼意四？
        // [Header("預設值")] [SerializeField]
        // [DropDownRef(null, nameof(SiblingValueFilter))]
        // private MonoEntity _siblingDefaultValue;
        //
        // private Type SiblingValueFilter()
        // {
        //     if (_varTag == null)
        //         return typeof(MonoEntity);
        //     // Debug.Log("RestrictType is " + varTag._valueFilterType.RestrictType);
        //     return _varTag.ValueFilterType;
        // }

        //FIXME: 繼承時想要加更多attribute
        // [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))] [SerializeField]
        // protected Component _defaultValue;


        // protected override MonoEntity DefaultValue => _defaultValue;


        // [Header("預設值")]
        // [DropDownRef]
        // [ShowInInspector]
        // MonoEntity SiblingDefaultValue
        // {
        //     set => _defaultValue = value;
        //     get => _defaultValue;
        // }

        // _siblingDefaultValue != null ? _siblingDefaultValue :

        //FIXME: 用Type更好嗎？
        // public override GameFlagBase FinalData => Value != null ? Value.Data : SampleData;

        //         public string IconName => "vcs_document";
        //         public bool IsDrawingIcon => true;
        //         //Fixme: 還是應該要外部登記比較好？
        // #if UNITY_EDITOR
        //         public Texture2D CustomIcon => UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.rcgmaker.fsm/RCGMakerFSMCore/Runtime/2_Variable/VarMonoIcon.png");
        // #endif
        // public string ValueInfo => Value != null ? Value.name : "null";
        // public bool IsDrawingValueInfo => true;

        // [Button]
        // private void AddEntityFromVarEntityProvider()
        // {
        //     this.AddChildrenComponent<EntityFromVarEntityProvider>("entityProvider");
        // }

        //來源需要強型別嗎？

#if UNITY_EDITOR
        //Schema 自動提示：EntityTag.containsVariableTypeTags 是這個 entity 的欄位宣告（來源 prefab 存檔時自動補齊），
        //下拉只列出還沒加過的 tag，選了按「加入」就生出對應型別的 proxy [Var] 子節點
        [BoxGroup("Schema 變數")]
        [PropertyOrder(60)]
        [LabelText("加入 Var")]
        [ValueDropdown(nameof(GetMissingSchemaTagItems))]
        [InlineButton(nameof(AddVarOfSelectedSchemaTag), "加入")]
        [ShowInInspector]
        [NonSerialized]
        private VariableTag _schemaTagToAdd;

        private IEnumerable<ValueDropdownItem<VariableTag>> GetMissingSchemaTagItems()
        {
            var items = new List<ValueDropdownItem<VariableTag>>();
            foreach (var varTag in GetMissingSchemaTags())
                items.Add(
                    new ValueDropdownItem<VariableTag>(
                        $"{varTag.name} <{varTag.VariableMonoType?.Name ?? "型別未設定"}>",
                        varTag
                    )
                );
            return items;
        }

        /// <summary>schema 宣告了、但底下還沒生出對應 [Var] 節點的 tag（Shift+V 快捷也用這份）</summary>
        public List<VariableTag> GetMissingSchemaTags()
        {
            var tags = new List<VariableTag>();
            var entityTag = EntityTag;
            if (entityTag == null)
                return tags;

            var existing = new HashSet<VariableTag>();
            foreach (var v in GetComponentsInChildren<AbstractMonoVariable>(true))
                if (v != this && v._varTag != null)
                    existing.Add(v._varTag);

            foreach (var varTag in entityTag.containsVariableTypeTags)
            {
                if (varTag == null || existing.Contains(varTag))
                    continue;
                tags.Add(varTag);
            }

            return tags;
        }

        private void AddVarOfSelectedSchemaTag()
        {
            if (_schemaTagToAdd == null)
            {
                Debug.LogWarning("先從下拉選單挑一個 VariableTag 再按加入", this);
                return;
            }

            AddVarOfSchemaTag(_schemaTagToAdd);
            _schemaTagToAdd = null;
        }

        /// <summary>依 schema tag 生出對應型別的 proxy [Var] 子節點，回傳建好的 Var（失敗回 null）</summary>
        public AbstractMonoVariable AddVarOfSchemaTag(VariableTag tag)
        {
            if (tag == null)
                return null;

            var varType = tag.VariableMonoType;
            if (varType == null || !typeof(AbstractMonoVariable).IsAssignableFrom(varType))
            {
                Debug.LogError($"VariableTag {tag.name} 的變數綁定型別未設定，無法生成", tag);
                return null;
            }

            var variable = (AbstractMonoVariable)
                gameObject.AddChildrenComponent(varType, $"[Var] {tag.name}");
            variable._varTag = tag;
            AutoAttributeManager.AutoReferenceFieldEditor(variable, "_parentVarEntity");
            UnityEditor.EditorUtility.SetDirty(variable);
            return variable;
        }
#endif
    }
}
