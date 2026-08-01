using MonoFSM.Core.Attributes;
using MonoFSM.Core.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.MonoData.List
{
    public class VarListCurrentIndexEqualsCondition : AbstractConditionBehaviour
    {
        public AbstractVarList _varList;

        // 沒手動指定就往上找祖先身上的 VarList（多個 UI slot 共用同一份清單用）。
        // [AutoParent] 是無條件覆寫，不能直接標在 _varList 上。
        [SerializeField] [AutoParent(false)] private AbstractVarList _parentVarList;

        [ShowInInspector]
        private AbstractVarList TargetList => _varList != null ? _varList : _parentVarList;

        public VarIntWrapper _compareIndex;

        protected override bool IsValid =>
            TargetList != null && TargetList.CurrentIndex == _compareIndex.Value;
    }
}
