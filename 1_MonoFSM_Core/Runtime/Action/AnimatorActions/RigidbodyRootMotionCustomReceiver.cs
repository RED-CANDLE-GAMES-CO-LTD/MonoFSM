using MonoFSM.Core.Simulate;
using MonoFSM.EditorExtension;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 把 Animator root motion（經 AnimatorRootMotionRelay 或 RootMotionSimulateClipMoveAction 餵進來的 delta）
/// 換成 velocity 寫進 Rigidbody，而不是 MovePosition。
/// 理由：dynamic rb 上 MovePosition 是瞬移，位移不進 linearVelocity，NetworkRigidbody 同步不到，
/// proxy 端外推會一格一格修正；kinematic rb 上則會蓋掉同一 tick 其他人（SimpleFlyingCharacter）設的目標位置。
/// 只在 StateAuthority 上寫；proxy 端的畫面交給 NetworkRigidbody 插值。
/// 與 SimpleChController 互斥：綁同一顆 _isAnimationDriven VarBool，非動畫接管時丟棄 pending，兩邊不搶同一顆 rb。
/// </summary>
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

    [AutoParent] private MonoObj _monoObj;

    /// <summary>
    /// 動畫 root motion 接管旗標。應綁 SimpleChController._isAnimationDriven 同一顆 VarBool。
    /// 為 false 時丟棄 pending 不寫 rb（交給 SimpleChController）。不綁 Var 時為常數，預設 true（沿用舊行為：永遠套用）。
    /// </summary>
    [SerializeField] private VarBoolWrapper _isAnimationDriven = new(true);

    private Vector3 pendingPosition;
    private Quaternion pendingRotation = Quaternion.identity;

    [Auto] private AnimatorRootMotionRelay _relay;

    public enum ApplyResult
    {
        Applied,
        NoPending,
        NotStateAuthority,
        NotAnimationDriven,
        ZeroDeltaTime,
    }

    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] private ApplyResult _lastResult;
    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] private Vector3 _lastPendingPosition;
    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] private Vector3 _lastAppliedVelocity;
    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] private Vector3 _lastAppliedAngularVelocity;

    public void OnProcessRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        // 累積位移和旋轉，在 AfterSimulate 中換成 velocity 套用
        pendingPosition += deltaPosition;
        pendingRotation = deltaRotation * pendingRotation;
    }

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
        _lastPendingPosition = pendingPosition;
        var hasPending = pendingPosition != Vector3.zero || pendingRotation != Quaternion.identity;

        // MonoObj.AfterSimulate 不擋 proxy，這裡自己擋：proxy 端不寫 rb，畫面交給 NetworkRigidbody 插值
        if (_monoObj != null && !_monoObj.HasStateAuthority)
        {
            _lastResult = ApplyResult.NotStateAuthority;
            ClearPending();
            return;
        }

        // 非動畫接管：rb 由 SimpleChController 驅動，這裡丟棄 pending，不然會蓋掉它的 velocity
        if (!_isAnimationDriven.Value)
        {
            _lastResult = ApplyResult.NotAnimationDriven;
            ClearPending();
            return;
        }

        if (!hasPending)
        {
            _lastResult = ApplyResult.NoPending;
            return;
        }

        if (deltaTime <= 0f)
        {
            _lastResult = ApplyResult.ZeroDeltaTime;
            ClearPending();
            return;
        }

        // 位移 → linearVelocity（NRB 會同步，proxy 才能外推）
        _lastAppliedVelocity = pendingPosition / deltaTime;
        rb.linearVelocity = _lastAppliedVelocity;

        // 旋轉 → angularVelocity
        _lastAppliedAngularVelocity = ToAngularVelocity(pendingRotation, deltaTime);
        rb.angularVelocity = _lastAppliedAngularVelocity;

        _lastResult = ApplyResult.Applied;
        ClearPending();
    }

    private void ClearPending()
    {
        pendingPosition = Vector3.zero;
        pendingRotation = Quaternion.identity;
    }

    /// <summary>把「這一 tick 的轉動量」換成角速度（rad/s），交給物理積分</summary>
    private static Vector3 ToAngularVelocity(Quaternion delta, float deltaTime)
    {
        delta.ToAngleAxis(out var angleDeg, out var axis);
        if (angleDeg > 180f) angleDeg -= 360f; // 走短邊
        if (Mathf.Abs(angleDeg) < 0.001f || deltaTime <= 0f) return Vector3.zero;
        return axis * (angleDeg * Mathf.Deg2Rad / deltaTime);
    }
}
