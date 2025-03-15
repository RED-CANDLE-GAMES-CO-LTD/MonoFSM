using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GuidComponent))]
public class GuidComponentDrawer : Editor
{
    private GuidComponent guidComp;

    public override void OnInspectorGUI()
    {
        if (guidComp == null)
        {
            guidComp = (GuidComponent)target;
        }

        //make the label selectable
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("GUID:", guidComp.GetGuid().ToString());

        EditorGUI.EndDisabledGroup();
        //copy button
        if (GUILayout.Button("Copy"))
        {
            EditorGUIUtility.systemCopyBuffer = guidComp.GetGuid().ToString();
        }
        
        // Draw label
        // EditorGUILayout.LabelField("Guid:", guidComp.GetGuid().ToString());
        
        
    }
}