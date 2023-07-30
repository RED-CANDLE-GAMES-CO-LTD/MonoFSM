using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    public class AnimatorReferencePlayAction : AnimatorPlayAction

    {
        [PropertyOrder(-1)] public InstanceReference animatorReference;

        private void OnValidate()
        {
            animator = animatorReference.prefab.GetComponent<Animator>();
        }

        protected override void Awake()
        {
            base.Awake();
            animator = animatorReference.instance.GetComponent<Animator>();
        }
    }
}