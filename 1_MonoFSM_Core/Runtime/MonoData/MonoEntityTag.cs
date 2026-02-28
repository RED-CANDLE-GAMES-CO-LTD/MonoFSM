using System;
using System.Collections.Generic;
using System.Linq;
using _1_MonoFSM_Core.Runtime._3_FlagData;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSM.Variable.TypeTag;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Runtime.Mono
{
    //為了DI可以找到相對應的物件用的tag, 先宣告下面有什麼變數可以用
    //先設計schema, 但這樣物件那邊又要對應，是不是很麻煩？
    //FIXME: 用DescriptableData是不是不太好？ 應該和data互斥？ 這個只是描述要尋找的類別
    //TODO: 要和
    //MonoEntityDef?
    //VarDef
    [CreateAssetMenu(menuName = "Assets/MonoFSM/MonoEntityTag", fileName = "NewMonoEntityTag")]
    public class MonoEntityTag : AbstractSOConfig, IStringKey
    {
        //
        public MySerializedType<MonoEntity> _entityType;

        public Type RestrictType
        {
            get => _entityType.RestrictType;
            set => _entityType.RestrictType = value;
        }

        //FIXME: 用不到？該拿掉
        [Obsolete]
        public MySerializedType<GameData> DataType;

        public IEnumerable<ValueDropdownItem<VariableTag>> GetVariableTagItems()
        {
            var tagDropdownItems = new List<ValueDropdownItem<VariableTag>>();
#if UNITY_EDITOR
            var tags = containsVariableTypeTags;
            foreach (var tempTag in tags)
            {
                if (tempTag != null)
                    tagDropdownItems.Add(new ValueDropdownItem<VariableTag>(tempTag.name, tempTag));
            }

#endif
            return tagDropdownItems;
        }

        [PreviewInInspector]
        string SampleDataFilter =>
            "t:"
            + (DataType.RestrictType != null ? DataType.RestrictType.Name : "DescriptableData");

        // [AssetSelector(Filter = "@SampleDataFilter")]
        // [AssetSelector]
        // bool TypeFilter(DescriptableData data)
        // {
        //     return DataType.RestrictType.IsAssignableFrom(data.GetType());
        // }

        // [AssetList(CustomFilterMethod = nameof(TypeFilter))]
#if UNITY_EDITOR
        IEnumerable<GameData> GetDescriptableData()
        {
            return AssetDatabase
                .FindAssets(SampleDataFilter)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameData>);
        }

        [ValueDropdown(nameof(GetDescriptableData))]
#endif

        public GameData SamepleData; //FIXME: 需要嗎？
        //FIXME: Data Type Restriction?
        public List<VariableTag> containsVariableTypeTags = new List<VariableTag>(); //VariableTag[] containsVariableTypeTags = Array.Empty<VariableTag>();

        //可用的 Schema types 列表，使用 AbstractTypeTag 來確保可序列化
        public List<AbstractTypeTag> containsSchemaTypeTags = new List<AbstractTypeTag>();

        public IEnumerable<ValueDropdownItem<AbstractTypeTag>> GetSchemaTypeTagItems()
        {
            var schemaTypeTagItems = new List<ValueDropdownItem<AbstractTypeTag>>();
            foreach (var schemaTypeTag in containsSchemaTypeTags)
            {
                if (schemaTypeTag != null && schemaTypeTag.Type != null)
                {
                    schemaTypeTagItems.Add(
                        new ValueDropdownItem<AbstractTypeTag>(
                            schemaTypeTag.Type.Name,
                            schemaTypeTag
                        )
                    );
                }
            }
            return schemaTypeTagItems;
        }

        //GameFlagDescriptable? Item?
        public bool IsCollectionTag; //還要繼承嗎？
#if UNITY_EDITOR
        [Button]
        void FindAllMonoDescriptable()
        {
            _allMonoDescriptable = FindObjectsByType<MonoEntity>(FindObjectsSortMode.None)
                .Where(x => x.DefaultTag == this)
                .ToArray();
        }

        [PreviewInInspector] MonoEntity[] _allMonoDescriptable;
#endif

        [SerializeField]
        private string _stringKey;
        public string GetStringKey => _stringKey;

// #if UNITY_EDITOR
        [HideInInlineEditors]
        [TextArea]
        public string Note;
// #endif

        public List<MonoEntity> Filter(List<MonoEntity> entities)
        {
            return entities.Where(e => e != null && e.DefaultTag == this).ToList();
        }

        public bool Equals(string other)
        {
            return GetStringKey == other;
        }
    }
}
