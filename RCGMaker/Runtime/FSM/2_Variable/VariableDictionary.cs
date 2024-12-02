using System;
using RCGMaker.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VariableDictionary : MonoDict<VariableTag, VariableFloat>
    {
        protected override void RemoveImplement(VariableFloat item)
        {
        }
    }

    [Serializable]
    public class VirtualFloat : IFloatValue
    {
        [AutoParent] VariableDictionary injectedVariables; //其實這個用 autoparent應該要可以

        //先找一個singleton monobehaviour, 然後
        [FormerlySerializedAs("VariableTypeTag")] [FormerlySerializedAs("variableTag")] public VariableTag VariableTag;
        public float FinalValue => injectedVariables[VariableTag].FinalValue;
    }
}