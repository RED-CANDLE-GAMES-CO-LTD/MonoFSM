
using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace MonoFSM.Editor.DesignTool
{
    // [RequireComponent(typeof(AreaMarker))]
public class GamePlayTag : AbstractMapTag, IGizmoColorProvider
{

#if UNITY_EDITOR

     Color IGizmoColorProvider.GizmoColor => GetTagColor();
     #else
          Color IGizmoColorProvider.GizmoColor => Color.white;
     #endif

    [Required] [GUIColor("GetTagColor")] [HideLabel]
    public GamePlayTypeSO tagType;

    [HideInInspector] public GamePlayTypeSO _lastTagType;

    //選了tagType會決定可以填哪些attributes
    [ListDrawerSettings(IsReadOnly = true)] [LabelText("標籤屬性")]
    public List<AttributeEntry> attributes;
#if UNITY_EDITOR


   
    private Color GetTagColor()
    {
        Sirenix.Utilities.Editor.GUIHelper.RequestRepaint();
        return tagType ? tagType.color : Color.white;
    }
    // [InlineEditor]
   
    [Button("重整標籤屬性")]
    void FetchTagAttributes()
    {
        //TODO: 少了attribute要新增？還要對應以前有的不要刪掉
        var oldList = attributes.ConvertAll(t => t);
        Rebuild(tagType);
        foreach (var attribute in attributes)
        {
            for (var i = 0; i < oldList.Count; i++)
            {
                // Debug.Log("Old Attri:" + oldList[i].attribute);
                if (attribute.type == oldList[i].type)
                {
                    attribute.attribute = oldList[i].attribute;
                }
            }
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        if (_lastTagType != tagType)
        {
            Rebuild(tagType);
            _lastTagType = tagType;
        }
        //FIXME: 怎麼更新？
        // GetComponent<AreaMarker>().OnDrawGizmos();
    }
    void Rebuild(GamePlayTypeSO tagSchema)
    {
        if (tagSchema == null)
            return;
        attributes.Clear();
        foreach (var attribute in tagSchema.requiredAttributes)
        {
            var entry = new AttributeEntry();
            entry.type = attribute;
            attributes.Add(entry);
        }
    }

    //不太好做multi...可以分開？ composition就好，或是真的分開tag
    [Button("Open Issue")]
    void OpenIssue()
    {
        
        var issue = this.AddChildrenComponent<Issue>("issue");
        issue.author = IssuePrefs.accountName;
    }
#endif
}


    public class AbstractMapTag : MonoBehaviour, IEditorOnly
{

    // [Header("改title會顯示在Scene中")]
    [TextArea]
    public string title = "GamePlay";
    [TextArea(5,100)]
    public string description = "";

    public int fontSize = 16;
    public bool IsCustomColor;
    [ShowIf("IsCustomColor")] public Color customColor = Color.white;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        //TODO: 之後想要怎麼做？
        //羊尾習慣在hierarchy上看到title？
        //hierarchy上 brief...?
        //comment1, comment2...?
        gameObject.name = title;
    }
    
    

    protected virtual Color GizmoColor
    {
        get
        {
            if (IsCustomColor)
                return customColor;
            return Color.white;
        }
    }

    protected virtual void OnDrawGizmos()
    {
        // var color = Gizmos.color = GizmoColor;
        // GUIStyle style = new GUIStyle(GUI.skin.label);
        // style.normal.textColor = color;
        // // UnityEditor.Handles.color = color;
        // // UnityEditor.Handles.DrawWireDisc(origin.position, new Vector3(0, 0, 1), radious);
        // style.fontSize = fontSize;
        // UnityEditor.Handles.Label(transform.position + offset + Vector3.right * 10, name, style);
        // Gizmos.DrawSphere(transform.position, 5);
    }
    protected void DrawLabel(string text, Vector3 offset = default(Vector3))
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = GizmoColor;
        style.fontSize = fontSize;
        UnityEditor.Handles.Label(transform.position + offset + Vector3.right * 10, text, style);
    }
#endif
}

}
