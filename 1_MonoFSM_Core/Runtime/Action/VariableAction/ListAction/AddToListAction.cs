using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSM.VarRefOld;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction.ListAction
{
    //ListManipulationAction?
    public class AddToListAction : AbstractStateAction
    {
        //用source & target?

        [Required] [CompRef] [AutoChildren] private SourceValueRef _sourceValueRef;
        [Required] [SerializeField] private AbstractVarList _targetList; //FIXME: 這個型別可以抽象掉嗎？

        protected override void OnActionExecuteImplement()
        {
            //型別爆掉自己負責？object一定可以...?
            //FIXME 先用object吧？
            _targetList.Add(_sourceValueRef.GetValue<Object>());
        }
    }
}