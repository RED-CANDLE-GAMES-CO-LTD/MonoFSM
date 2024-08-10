#if UNITY_EDITOR
using UnityEditor;
#endif
using RCGMaker.Core.Attributes;
using UnityEngine;


#if UNITY_EDITOR
[CanEditMultipleObjects]
#endif
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

#endif
    
    // private void OnEnable()
    // {
    //     SceneView.duringSceneGui += OnSceneGUI;
    // }
    // private void OnSceneGUI(SceneView sceneView)
    // {
    //     transform.position = Handles.PositionHandle(transform.position, transform.rotation);
    // }
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