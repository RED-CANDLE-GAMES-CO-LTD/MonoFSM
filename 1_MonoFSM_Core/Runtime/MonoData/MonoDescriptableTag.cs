using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Mono
{
    //為了DI可以找到相對應的物件用的tag, 先宣告下面有什麼變數可以用
    //先設計schema, 但這樣物件那邊又要對應，是不是很麻煩？
    //FIXME: 用DescriptableData是不是不太好？ 應該和data互斥？ 這個只是描述要尋找的類別
    [CreateAssetMenu(menuName = "RCGMaker/MonoDescriptableTag")]
    public class MonoDescriptableTag : ScriptableObject, IStringKey
    {
        public MySerializedType<DescriptableData> DataType;


        [PreviewInInspector]
        string SampleDataFilter =>
            "t:" + (DataType.RestrictType != null ? DataType.RestrictType.Name : "DescriptableData");

        // [AssetSelector(Filter = "@SampleDataFilter")]
        // [AssetSelector] 
        // bool TypeFilter(DescriptableData data)
        // {
        //     return DataType.RestrictType.IsAssignableFrom(data.GetType());
        // }

        // [AssetList(CustomFilterMethod = nameof(TypeFilter))]
#if UNITY_EDITOR
        IEnumerable<DescriptableData> GetDescriptableData()
        {
            return AssetDatabase.FindAssets(SampleDataFilter).Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<DescriptableData>);
        }

        [ValueDropdown(nameof(GetDescriptableData))]
        public DescriptableData SamepleData;
#endif


        //FIXME: Data Type Restriction?
        public List<VariableTag> containsVariableTypeTags = new List<VariableTag>(); //VariableTag[] containsVariableTypeTags = Array.Empty<VariableTag>();

        //GameFlagDescriptable? Item?
        public bool IsCollectionTag; //還要繼承嗎？
#if UNITY_EDITOR
        [Button]
        void FindAllMonoDescriptable()
        {
            _allMonoDescriptable = FindObjectsByType<MonoDescriptable>(FindObjectsSortMode.None).Where(x => x.Tag == this).ToArray();
        }

        [PreviewInInspector] public MonoDescriptable[] _allMonoDescriptable;
#endif

        [SerializeField] private string _stringKey;
        public string GetStringKey => _stringKey;
    }
}