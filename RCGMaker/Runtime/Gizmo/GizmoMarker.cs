using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
public class GizmoMarker : MonoBehaviour
{
#if UNITY_EDITOR
    public enum GizmoShapeType
    {
        Solid,
        Wire,
        BoxCollider,
        HandleDot,
        HandleSphere
    }

    // public bool useHandle = false;
    public GizmoShapeType gizmoType = GizmoShapeType.Solid;
    public Color color = Color.yellow;
    public float size = 20;

    private void OnValidate()
    {
        if (boxCollider2D == null)
            boxCollider2D = GetComponent<BoxCollider2D>();
    }

    [SerializeField] private BoxCollider2D boxCollider2D;

    public bool disable = false;

    // public bool IsDrawForCollider = false;

    // [DrawGizmo(GizmoType.InSelectionHierarchy)]
    // private static void DrawGizmoMarker(Transform transform, GizmoType gizmoType)
    // {
    //     if (!transform.TryGetComponent<GizmoMarker>(out var gizmoMarker))
    //         return;
    //     Debug.Log("DrawGizmoMarker", transform);
    //     // Draw a yellow sphere at the transform's position
    //     Gizmos.color = gizmoMarker.color;
    //     // transform.position = Handles.PositionHandle(transform.position, transform.rotation);
    //     // Handles.DrawSolidDisc(transform.position, Vector3.forward, size);
    //     // Handles.DrawSphere
    //     // var size = new Vector2(transform.lossyScale.x * boxCollider2D.size.x,
    //     //     transform.lossyScale.y * boxCollider2D.size.y);
    //     var boxCollider2D = gizmoMarker.boxCollider2D;
    //     if (gizmoMarker.gizmoType == GizmoShapeType.BoxCollider && boxCollider2D)
    //     {
    //         Gizmos.DrawWireCube(
    //             transform.position + new Vector3(transform.lossyScale.x * boxCollider2D.offset.x,
    //                 transform.lossyScale.y * boxCollider2D.offset.y), boxCollider2D.size * transform.lossyScale);
    //         return;
    //     }
    //
    //     // else
    //     // {
    //     var size = gizmoMarker.size;
    //     if (gizmoMarker.gizmoType == GizmoShapeType.Solid)
    //         Gizmos.DrawSphere(transform.position, size);
    //     else
    //         Gizmos.DrawWireSphere(transform.position, size);
    // }

    private void OnDrawGizmos()
    {
        if (disable || gizmoType == GizmoShapeType.HandleDot || gizmoType == GizmoShapeType.HandleSphere)
            return;
        // Draw a yellow sphere at the transform's position
        Gizmos.color = color;
        // transform.position = Handles.PositionHandle(transform.position, transform.rotation);
        // Handles.DrawSolidDisc(transform.position, Vector3.forward, size);
        // Handles.DrawSphere
        // var size = new Vector2(transform.lossyScale.x * boxCollider2D.size.x,
        //     transform.lossyScale.y * boxCollider2D.size.y);
        if (gizmoType == GizmoShapeType.BoxCollider && boxCollider2D)
        {
            Gizmos.DrawWireCube(
                transform.position + new Vector3(transform.lossyScale.x * boxCollider2D.offset.x,
                    transform.lossyScale.y * boxCollider2D.offset.y), boxCollider2D.size * transform.lossyScale);
            return;
        }

        // else
        // {
        if (gizmoType == GizmoShapeType.Solid)
            Gizmos.DrawSphere(transform.position, size);
        else
            Gizmos.DrawWireSphere(transform.position, size);
    }

    // FIXME:
    //  this.DrawText(transform.position, name);
    

    
    // private void OnEnable()
    // {
    //     SceneView.duringSceneGui += OnSceneGUI;
    // }
    // private void OnSceneGUI(SceneView sceneView)
    // {
    //     transform.position = Handles.PositionHandle(transform.position, transform.rotation);
    // }
#endif
}