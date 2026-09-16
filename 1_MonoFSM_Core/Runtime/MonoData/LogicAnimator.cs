using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 標記「這個節點是某個 MonoObj 的動畫／邏輯本體」，把節點自動命名成 `[Anim] &lt;所屬 MonoObj 的名字&gt;`。
/// 掛在 Character FSM 底下的 Context/[Anim] … 節點上（Animator、Rigidbody、移動控制都住那顆）。
/// ⚠ Description 直接 `GetComponentInParent&lt;MonoObj&gt;().name`，父鏈上沒有 MonoObj 時會丟 NRE，
/// 自動改名就靜默失效（例如對 prefab asset 而非 prefab contents 取值時）。
/// </summary>
public class LogicAnimator : AbstractDescriptionBehaviour
{
    protected override string DescriptionTag => "Anim";
    public override string Description => GetComponentInParent<MonoObj>().name;

    [PreviewInInspector] [Required] [Auto] Animator _animator;
}
