using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayerDesignPoint : MonoBehaviour
{
    public string iconName;
    public bool showGizmoText = true;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draws the Light bulb icon at position of the object.
        // Because we draw it inside OnDrawGizmos the icon is also pickable
        // in the scene view.


        Gizmos.color = Color.white;
        // var start = shootSpawner.defaultSpawnPoint.position;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 16;
        if (showGizmoText)
            UnityEditor.Handles.Label(transform.position + Vector3.down, name, style);
        Gizmos.DrawIcon(transform.position, iconName + ".png", false);
    }
#endif
}
