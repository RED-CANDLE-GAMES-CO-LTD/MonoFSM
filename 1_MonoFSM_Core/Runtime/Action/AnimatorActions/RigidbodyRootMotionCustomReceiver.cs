using MonoFSM.Core.Simulate;
using MonoFSM.EditorExtension;
using Sirenix.OdinInspector;
using UnityEngine;

// [RequireComponent(typeof(Rigidbody))]
public class RigidbodyRootMotionCustomReceiver
    : MonoBehaviour,
        IRootMotionReceiver,
        IUpdateSimulate,
        IOverrideHierarchyIcon,
        IDrawHierarchyBackGround,
        IHierarchyValueInfo,
        IAfterSimulate
{
    [Required]
    [ShowInInspector]
    [SerializeField]
    [AutoParent]
    private Rigidbody rb;

    private Vector3 pendingPosition;
    private Quaternion pendingRotation = Quaternion.identity;

    [Auto] private AnimatorRootMotionRelay _relay;

    public void OnProcessRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        // 累積位移和旋轉，在 FixedUpdate 中套用
        pendingPosition += deltaPosition;
        pendingRotation = deltaRotation * pendingRotation;
    }

    //FIXME: 需要 IUpdateSimulate
    // private void FixedUpdate()
    // {
    //
    // }

#if UNITY_EDITOR
    // IOverrideHierarchyIcon 實作
    public string IconName => "Rigidbody Icon";
    public bool IsDrawingIcon => _relay == null; // 只有獨立存在時顯示圖示
    public Texture2D CustomIcon => null;
    public bool IsPosAtHead => false; // 圖示在右邊

    // IDrawHierarchyBackGround 實作
    public Color BackgroundColor => new(0.8f, 0.7f, 0.2f, 0.15f); // 淡黃色
    public bool IsDrawGUIHierarchyBackground => _relay == null; // 只有獨立存在時顯示背景

    // IHierarchyValueInfo 實作 - 顯示接收狀態
    public string ValueInfo => "Rigidbody ↰";
    public bool IsDrawingValueInfo => _relay == null; // 只有獨立存在時顯示文字
#endif

    public void Simulate(float deltaTime)
    {

    }

    public void AfterSimulate(float deltaTime)
    {
        if (pendingPosition != Vector3.zero || pendingRotation != Quaternion.identity)
        {
            // Debug.Log(
            //     $"Applying pending root motion to Rigidbody: ΔPos={pendingPosition}, ΔRot={pendingRotation.eulerAngles}",
            //     this);
            rb.MovePosition(rb.position + pendingPosition);
            rb.MoveRotation(rb.rotation * pendingRotation);

            pendingPosition = Vector3.zero;
            pendingRotation = Quaternion.identity;
        }
    }
}
