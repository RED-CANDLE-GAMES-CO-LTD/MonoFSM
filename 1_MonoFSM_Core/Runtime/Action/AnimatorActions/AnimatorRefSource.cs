using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.AnimatorActions
{
    public class AnimatorRefSource : AbstractValueSource<Animator>
    {
        
        [SerializeField]
        [DropDownRef]
        // [TypeFilter()]
        private VarComp _animatorRef;

        [Required]
        public override Animator Value => _animatorRef?.Value as Animator;

        protected override bool HasError()
        {
            return base.HasError() || !(_animatorRef.Value is Animator);
        }
    }
}
