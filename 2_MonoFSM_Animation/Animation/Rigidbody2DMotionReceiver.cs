using MonoFSM.EditorExtension;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Rigidbody2DMotionReceiver : MonoBehaviour, IRootMotionReceiver, IOverrideHierarchyIcon, IDrawHierarchyBackGround, IHierarchyValueInfo
{
    [Auto][Required][ShowInInspector]
    private Rigidbody2D rb2D;
    private Vector2 pendingPosition;
    private float pendingRotation;

    [Auto] private AnimatorRootMotionRelay _relay;

    private void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
    }

    public void OnProcessRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        // 累積位移和旋轉，在 FixedUpdate 中套用
        pendingPosition += new Vector2(deltaPosition.x, deltaPosition.y);
        pendingRotation += deltaRotation.eulerAngles.z;
    }

    private void FixedUpdate()
    {
        if (pendingPosition != Vector2.zero || pendingRotation != 0f)
        {
            rb2D.MovePosition(rb2D.position + pendingPosition);
            rb2D.MoveRotation(rb2D.rotation + pendingRotation);

            pendingPosition = Vector2.zero;
            pendingRotation = 0f;
        }
    }

#if UNITY_EDITOR
    // IOverrideHierarchyIcon 實作
    public string IconName => "Rigidbody2D Icon";
    public bool IsDrawingIcon => _relay == null; // 只有獨立存在時顯示圖示
    public Texture2D CustomIcon => null;
    public bool IsPosAtHead => false; // 圖示在右邊

    // IDrawHierarchyBackGround 實作
    public Color BackgroundColor => new Color(0.8f, 0.7f, 0.2f, 0.15f); // 淡黃色
    public bool IsDrawGUIHierarchyBackground => _relay == null; // 只有獨立存在時顯示背景

    // IHierarchyValueInfo 實作 - 顯示接收狀態
    public string ValueInfo => "Rigidbody2D ↰";
    public bool IsDrawingValueInfo => _relay == null; // 只有獨立存在時顯示文字
#endif
}
