using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    //condition activate?
    //FIXME: 需要對稱？
    public class ApplyModifierAction : AbstractStateAction //FIXME: 好像做成一個action比較好？
    {
        //FIXME: 沒辦法指定VariableStat..有點尷尬，要吃兩個Type?
        //用varTag, monoTag直接找到 variable
        //從VarMono, 拿到他的variable
        // public VariableProvider<float> _variableProvider;

        [SerializeReference] public IVariableProvider _variableProvider;

        [SerializeField] private VarStat _targetStat;
        // [FormerlySerializedAs("TargetVariableTypeType")]
        // [FormerlySerializedAs("targetVariableType")]
        // [InfoBox(
        //     "This action will find the VariableStatOwner in the parent of the current GameObject and add the modifiers from the ModifierInjector to the VariableStat with the same type.")]
        // [SerializeField]
        // VariableTag TargetVariable;

        // public VariableTag targetVariable => TargetVariable;

        [CompRef] [AutoChildren]
        private VariableStatModifier[] _modifiers;

        public VariableStatModifier[] Modifiers => _modifiers; //有需要陣列嗎？
        //onstateenter時，找到parent的VariableStatOwner，然後找到相同type的VariableStat，然後加上modifier
        //onstateexit時，移除modifier

        private VariableStatOwner _foundStatOwner;

        protected override void OnActionExecuteImplement()
        {
            var varStat = _variableProvider.GetVar<VarStat>();
            foreach (var modifier in _modifiers) varStat.RegisterModifier(modifier);
        }

        // protected override void OnStateExitImplement()
        // {
        //     var varStat = _variableProvider.GetVar<VarStat>();
        //     foreach (var modifier in _modifiers) varStat.RemoveModifier(modifier);
        // }
    }
}