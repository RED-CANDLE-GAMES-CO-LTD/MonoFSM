using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class FindStatToModifierAction : AbstractStateAction //FIXME: 好像做成一個action比較好？
    {
        [FormerlySerializedAs("TargetVariableTypeType")]
        [FormerlySerializedAs("targetVariableType")]
        [InfoBox("This action will find the VariableStatOwner in the parent of the current GameObject and add the modifiers from the ModifierInjector to the VariableStat with the same type.")]
        [SerializeField] VariableTag TargetVariable;
        public VariableTag targetVariable => TargetVariable;
        [Component]
        [PreviewInInspector]
        [AutoChildren] private VariableStatModifier[] _modifiers;
        public VariableStatModifier[] Modifiers => _modifiers; //有需要陣列嗎？
        //onstateenter時，找到parent的VariableStatOwner，然後找到相同type的VariableStat，然後加上modifier
        //onstateexit時，移除modifier

        VariableStatOwner _foundStatOwner;
        protected override void OnStateEnterImplement()
        {
            _foundStatOwner = GetComponentInParent<VariableStatOwner>();
            if (_foundStatOwner == null)
            {
                Debug.LogError("No VariableStatOwner found in parent of " + gameObject.name,this);
                return;
            }
            var variableStats = _foundStatOwner.VariableStats;
            foreach (var stat in variableStats)
            {
                if (stat.varTag == TargetVariable)
                {
                    foreach (var modifier in _modifiers)
                    {
                        stat.AddModifier(modifier);
                    }
                }
            }
        }
        
        protected override void OnStateExitImplement()
        {
            var variableStats = _foundStatOwner.VariableStats;
            foreach (var stat in variableStats)
            {
                if (stat.varTag == TargetVariable)
                {
                    foreach (var modifier in _modifiers)
                    {
                        stat.RemoveModifier(modifier);
                    }
                }
            }
            _foundStatOwner = null;
        }
        
        
    }
}