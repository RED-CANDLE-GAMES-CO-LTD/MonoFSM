using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using UnityEngine;

namespace RCGMaker.Runtime.Item_BuildSystem
{
    //為了DI可以找到相對應的物件用的tag, 先宣告下面有什麼變數可以用
    //先設計schema, 但這樣物件那邊又要對應，是不是很麻煩？
    [CreateAssetMenu(menuName = "RCGMaker/MonoDescriptableTag")]
    public class MonoDescriptableTag : ScriptableObject
    {
        public VariableTag[] containsVariableTypeTags = Array.Empty<VariableTag>();
        //GameFlagDescriptable? Item?
        public bool IsCollectionTag; //還要繼承嗎？


    }
}