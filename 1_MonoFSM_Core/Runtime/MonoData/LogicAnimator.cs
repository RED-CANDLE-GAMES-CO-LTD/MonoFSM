using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

public class LogicAnimator : AbstractDescriptionBehaviour
{
    protected override string DescriptionTag => "Anim";
    public override string Description => GetComponentInParent<MonoObj>().name;

    [PreviewInInspector] [Required] [Auto] Animator _animator;
}
