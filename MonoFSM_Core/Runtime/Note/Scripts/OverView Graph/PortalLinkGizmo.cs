using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


public class PortalLinkGizmo : MonoBehaviour
{
#if UNITY_EDITOR
    public Transform a;
    public Transform b;
    public Transform anchor;
    public enum LinkDir
    {
        Vert,
        Hori
    }
    public LinkDir linkDir = LinkDir.Vert;
    private void OnValidate()
    {
        //TODO: 怎麼做比較好...會有個討厭的parent
        //重新算位置？

    }
    const float size = 4;
    private void OnDrawGizmos()
    {

        if (linkDir == LinkDir.Vert)
        {
            var aPoint = new Vector3(a.position.x, anchor.position.y);
            var bPoint = new Vector3(b.position.x, anchor.position.y);

            Handles.DrawDottedLine(a.position, aPoint, size);
            Handles.DrawDottedLine(aPoint, bPoint, size);
            Handles.DrawDottedLine(bPoint, b.position, size);
        }
        else if (linkDir == LinkDir.Hori)
        {
            var aPoint = new Vector3(anchor.position.x, a.position.y);
            var bPoint = new Vector3(anchor.position.x, b.position.y);

            Handles.DrawDottedLine(a.position, aPoint, size);
            Handles.DrawDottedLine(aPoint, bPoint, size);
            Handles.DrawDottedLine(bPoint, b.position, size);
        }

        // if (other)
        //     Handles.DrawLine(transform.position, other.transform.position);
    }
#endif
}

