using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core
{
    //直接對InstanceReference的instance做操作
    public class AnimatorReferencePlayAction : AbstractAnimatorPlayAction, IResetter
    {
        [ShowInPlayMode] public GameObject instance => AnimatorReferenceData?.RunTimeInstance;

        [FormerlySerializedAs("animatorReference")]
        [InlineEditor]
        [PropertyOrder(-1)] public InstanceReferenceData AnimatorReferenceData;

        private void OnValidate()
        {
            // animator = animatorReference.prefab.GetComponent<Animator>();
        }

        public void EnterLevelReset()
        {
            if (AnimatorReferenceData.RunTimeInstance != null)
                animator = AnimatorReferenceData.RunTimeInstance.GetComponent<Animator>();
        }

        public void ExitLevelAndDestroy()
        {

        }
    }
}