using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Core
{
    //直接對InstanceReference的instance做操作
    public class AnimatorReferencePlayAction : AbstractAnimatorPlayAction, IResetter
    {
        [ShowInInspector] public GameObject instance => AnimatorReferenceData?.instance;

        [FormerlySerializedAs("animatorReference")]
        [InlineEditor]
        [PropertyOrder(-1)] public InstanceReferenceData AnimatorReferenceData;

        private void OnValidate()
        {
            // animator = animatorReference.prefab.GetComponent<Animator>();
        }

        public void EnterLevelReset()
        {
            if( AnimatorReferenceData.instance!=null)
             animator = AnimatorReferenceData.instance.GetComponent<Animator>();
        }

        public void ExitLevelAndDestroy()
        {

        }
    }
}