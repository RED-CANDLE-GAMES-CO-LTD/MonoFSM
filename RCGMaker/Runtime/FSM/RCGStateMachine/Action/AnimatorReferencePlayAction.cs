using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    //直接對InstanceReference的instance做操作
    public class AnimatorReferencePlayAction : AnimatorPlayAction,IResetter
    {
        [ShowInInspector] public GameObject instance => animatorReference.instance;

        [InlineEditor]
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