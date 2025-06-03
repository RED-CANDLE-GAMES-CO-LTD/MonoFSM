using UnityEngine;

public class GizmoRectPainter : MonoBehaviour
{
    public Rect rect;
    public Color color;

    private void OnDrawGizmos() 
        => DrawRect(rect, color);

    private static void DrawRect(Rect rect, Color c)
    {
        Gizmos.color = c;
        Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMin, 0), new Vector3(rect.xMax, rect.yMin, 0));
        Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMin, 0), new Vector3(rect.xMin, rect.yMax, 0));
        Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMax, 0), new Vector3(rect.xMax, rect.yMin, 0));
        Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMax, 0), new Vector3(rect.xMin, rect.yMax, 0));
    }
}