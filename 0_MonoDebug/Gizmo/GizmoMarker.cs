using MonoDebugSetting;
using Sirenix.OdinInspector;
using UnityEngine;

//FIXME: Debug Gizmo?
public class GizmoMarker : MonoBehaviour, IEditorOnly //,IDrawHierarchyBackGround
{
#if UNITY_EDITOR
    public enum GizmoShapeType
    {
        Solid,
        Wire,
        BoxCollider,
        HandleDot,
        HandleSphere,
    }

    //目前這個 Gizmo 到底畫不畫得出來（不含距離判斷）
    private bool IsGizmoVisibleNow =>
        (RuntimeDebugSetting.IsDebugMode || _isAlwaysVisible)
        && gizmoType != GizmoShapeType.HandleDot
        && gizmoType != GizmoShapeType.HandleSphere;

    private string GizmoVisibleInfo
    {
        get
        {
            if (gizmoType is GizmoShapeType.HandleDot or GizmoShapeType.HandleSphere)
                return $"不顯示：{gizmoType} 由 Handle 繪製，OnDrawGizmos 直接 return";
            if (_isAlwaysVisible)
                return "顯示中：AlwaysVisible 開啟（不受 Debug Mode 與距離限制）";
            if (RuntimeDebugSetting.IsDebugMode)
                return "顯示中：Debug Mode 開啟（距離 SceneView 相機 100 以內才畫）";
            return "不顯示：Debug Mode 關閉，且 AlwaysVisible 未開";
        }
    }

    [PropertyOrder(-1)]
    [InfoBox(
        "$" + nameof(GizmoVisibleInfo),
        InfoMessageType.Info,
        VisibleIf = nameof(IsGizmoVisibleNow)
    )]
    [InfoBox(
        "$" + nameof(GizmoVisibleInfo),
        InfoMessageType.Warning,
        VisibleIf = "@!" + nameof(IsGizmoVisibleNow)
    )]
    public bool _isAlwaysVisible = false;

    // public bool useHandle = false;
    public GizmoShapeType gizmoType = GizmoShapeType.Solid;
    public Color color = Color.yellow;
    public float size = 1;

    // public bool IsForceShow = false;
    private void OnValidate()
    {
        if (boxCollider2D == null)
            boxCollider2D = GetComponent<BoxCollider2D>();
    }

    [SerializeField]
    private BoxCollider2D boxCollider2D;

    [SerializeField]
    [Auto]
    private BoxCollider _boxCollider;

    // public bool disable = false;


    private void OnDrawGizmos()
    {
        if (RuntimeDebugSetting.IsDebugMode == false && _isAlwaysVisible == false)
            return;
        if (gizmoType == GizmoShapeType.HandleDot || gizmoType == GizmoShapeType.HandleSphere)
            return;

        // 距離判斷：超過 20 單位不畫 Gizmo
        // Debug.Log("Distance too far, not drawing Gizmo: ");
        var sceneView = UnityEditor.SceneView.lastActiveSceneView;
        if (!_isAlwaysVisible && sceneView != null && sceneView.camera != null)
        {
            var dist = Vector3.Distance(sceneView.camera.transform.position, transform.position);
            if (dist > 100f) // 你可以調整這個距離
                // Debug.Log("Distance too far, not drawing Gizmo: " + dist);
                return;
        }

        // Draw a yellow sphere at the transform's position
        Gizmos.color = color;
        // transform.position = Handles.PositionHandle(transform.position, transform.rotation);
        // Handles.DrawSolidDisc(transform.position, Vector3.forward, size);
        // Handles.DrawSphere
        // var size = new Vector2(transform.lossyScale.x * boxCollider2D.size.x,
        //     transform.lossyScale.y * boxCollider2D.size.y);
        if (gizmoType == GizmoShapeType.BoxCollider)
        {
            if (boxCollider2D)
                Gizmos.DrawWireCube(
                    transform.position
                        + new Vector3(
                            transform.lossyScale.x * boxCollider2D.offset.x,
                            transform.lossyScale.y * boxCollider2D.offset.y
                        ),
                    boxCollider2D.size * transform.lossyScale
                );
            if (_boxCollider)
            {
                var cubeTransform = Matrix4x4.TRS(
                    _boxCollider.transform.position
                        + Vector3.Scale(_boxCollider.center, _boxCollider.transform.lossyScale),
                    _boxCollider.transform.rotation,
                    Vector3.Scale(_boxCollider.size, _boxCollider.transform.lossyScale)
                );
                Gizmos.matrix = cubeTransform;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
                Gizmos.matrix = Matrix4x4.identity;
            }
            return;
        }

        if (gizmoType == GizmoShapeType.Solid || gizmoType == GizmoShapeType.Wire)
        {
            var sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                var maxScale = Mathf.Max(
                    transform.lossyScale.x,
                    transform.lossyScale.y,
                    transform.lossyScale.z
                );
                var radius = sphereCollider.radius * maxScale;
                if (gizmoType == GizmoShapeType.Solid)
                    Gizmos.DrawSphere(transform.position, radius);
                else
                    Gizmos.DrawWireSphere(transform.position, radius);
            }
            else
            {
                if (gizmoType == GizmoShapeType.Solid)
                    Gizmos.DrawSphere(transform.position, size);
                else
                    Gizmos.DrawWireSphere(transform.position, size);
            }
        }
    }
    // FIXME:
    //  this.DrawText(transform.position, name);
#endif

    public Color BackgroundColor
    {
        get
        {
#if UNITY_EDITOR
            return new Color(color.r, color.g, color.b, 0.2f);
#else
            return Color.clear;
#endif
        }
    }

    public bool IsDrawGUIHierarchyBackground => false;
    //spline bound?
}
