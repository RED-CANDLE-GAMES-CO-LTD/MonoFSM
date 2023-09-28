using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    public class AnimatorReferencePlayAction : AnimatorPlayAction,IResetter

    {
        [PropertyOrder(-1)] public InstanceReference animatorReference;

        private void OnValidate()
        {
            animator = animatorReference.prefab.GetComponent<Animator>();
        }

        public void EnterLevelReset()
        {
            if( animatorReference.instance!=null)
             animator = animatorReference.instance.GetComponent<Animator>();
        }

        public void ExitLevelAndDestroy()
        {

        }
    }
}