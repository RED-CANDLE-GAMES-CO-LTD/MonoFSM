using System;
using RCGMaker.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VariableDictionary : MonoDict<VariableTag, VariableFloat>
    {
        [Button]
        void AddVariablesInChildren()
        {
            foreach (var variable in GetComponentsInChildren<VariableFloat>())
            {
                Add(variable.VarType, variable);
            }
        }

        protected override void RemoveImplement(VariableFloat item)
        {
        }
    }

    [Serializable]
    public class VirtualFloat : IFloatValue
    {
        [AutoParent] VariableDictionary injectedVariables; //其實這個用 autoparent應該要可以

        //先找一個singleton monobehaviour, 然後
        public VariableTag variableTag;
        public float FinalValue => injectedVariables[variableTag].FinalValue;
    }
}