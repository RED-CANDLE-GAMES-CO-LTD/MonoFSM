using _1_MonoFSM_Core.Runtime._1_Conditions;
using MonoFSM.EditorExtension;

namespace MonoFSM_Core.Runtime.StateBehaviour
{
    //怪怪的？如果只有一個進入條件？
    public class CanEnterNode : CompositeCondition, IHierarchyValueInfo //抽成一種ConditionNode? Decision Node? 和CompositionCondition根本一樣？
    {

        // [Tooltip("選擇條件組合的邏輯操作：AND (所有條件都必須滿足) 或 OR (至少一個條件滿足)")] [LabelText("操作類型")]
        // public CompositeOperationType _operationType = CompositeOperationType.And;
        //
        // [CompRef] [AutoChildren(DepthOneOnly = true)]
        // private AbstractConditionBehaviour[] _conditions;
        //
        // public bool IsValid
        // {
        //     get
        //     {
        //         if (_conditions == null || _conditions.Length == 0)
        //             return true;
        //
        //         switch (_operationType)
        //         {
        //             case CompositeOperationType.And:
        //                 return _conditions.IsAllValid();
        //
        //             case CompositeOperationType.Or:
        //                 return _conditions.IsAnyValid();
        //
        //             default:
        //                 return _conditions.IsAllValid();
        //         }
        //     }
        // }
        //
        // public string ValueInfo => IsValid.ToString();
        // public bool IsDrawingValueInfo => Application.isPlaying && isActiveAndEnabled;
    }
}
