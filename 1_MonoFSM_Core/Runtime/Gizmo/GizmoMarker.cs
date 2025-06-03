using UnityEngine;

using RCGExtension;

public class GizmoMarker : MonoBehaviour, IDrawHierarchyBackGround, IEditorOnly
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

    // public bool IsForceShow = false;
    private void OnValidate()
    {
        if (boxCollider2D == null)
            boxCollider2D = GetComponent<BoxCollider2D>();
    }

    [SerializeField] private BoxCollider2D boxCollider2D;

    [SerializeField] [Auto] private BoxCollider _boxCollider;
    public bool disable = false;
    

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
        if (gizmoType == GizmoShapeType.BoxCollider)
        {
            if (boxCollider2D)
                Gizmos.DrawWireCube(
                    transform.position + new Vector3(transform.lossyScale.x * boxCollider2D.offset.x,
                        transform.lossyScale.y * boxCollider2D.offset.y), boxCollider2D.size * transform.lossyScale);
            if (_boxCollider)
            {
                var cubeTransform = Matrix4x4.TRS(
                    _boxCollider.transform.position +
                    Vector3.Scale(_boxCollider.center, _boxCollider.transform.lossyScale),
                    _boxCollider.transform.rotation,
                    Vector3.Scale(_boxCollider.size, _boxCollider.transform.lossyScale)
                );
                Gizmos.matrix = cubeTransform;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
                Gizmos.matrix = Matrix4x4.identity;
            }
            return;
        }

        if (gizmoType == GizmoShapeType.Solid)
            Gizmos.DrawSphere(transform.position, size);
        else
            Gizmos.DrawWireSphere(transform.position, size);
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

    public bool IsDrawGUIHierarchyBackground => true;

}

