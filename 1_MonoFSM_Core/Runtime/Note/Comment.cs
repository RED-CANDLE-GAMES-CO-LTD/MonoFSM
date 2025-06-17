using System.Collections;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Editor.DesignTool
{
    public class Comment : AbstractMapTag, IEditorOnly
    {
        [Header("描述---")] [EnumToggleButtons] public IssueType type = IssueType.Question; //好像不用？應該看有沒有Resolve就好，可以上色

        // [TextArea]
        // public string message;
        public string author;
#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            var color = Gizmos.color = GizmoColor;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            // UnityEditor.Handles.color = color;
            // UnityEditor.Handles.DrawWireDisc(origin.position, new Vector3(0, 0, 1), radious);
            style.fontSize = fontSize;
            UnityEditor.Handles.Label(transform.position + Vector3.right * 10, name, style);
            // Gizmos.DrawSphere(transform.position, 5);
        }
#endif
    }

}
