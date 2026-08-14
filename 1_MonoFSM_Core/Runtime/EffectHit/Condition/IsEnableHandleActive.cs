using MonoFSM.Core.Module;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
    public class IsEnableHandleActive : AbstractConditionBehaviour
    {
        public override string Description => $"{enableHandle?.name} is active and enabled";

        //tag mapping find...?
        // [DropDownRef]
        [ShowInInspector]
        public EnableHandle enableHandle => _overrideEnableHandle
            ? _overrideEnableHandle
            : _enableHandleVar?.Value as EnableHandle;

        [DropDownRef] [SerializeField] EnableHandle _overrideEnableHandle;

        [HideIf(nameof(_overrideEnableHandle))]
        public VarComp _enableHandleVar;
        //wrap?

        //不夠好用，還是要用類別來mapping
        //用effect type可能也不夠耶, general tag?
        protected override bool IsValid => enableHandle?.isActiveAndEnabled ?? false;
        // _enableHandle != null && _enableHandle.isActiveAndEnabled;
    }
}
