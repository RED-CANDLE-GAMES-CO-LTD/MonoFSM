using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MonoFSM.Core.Attributes;
using UnityEngine;
using Sirenix.OdinInspector;
using MonoFSM.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Networking;
using UnityEngine.Serialization;

// [RequireComponent(GizmoMarker)]
namespace MonoFSM.Editor.DesignTool
{
    public enum IssueType
    {
        Todo,
        Fixme,
        Resolved,
        Question,
    }
    public enum IssueToAssign
    {
        Bug出事了阿伯,
        Art美術,
        GamePlay遊戲設計,
        Narritive敘事,
        Implementation實作內容,
    }


    public static class IssuePrefs
    {
    #if UNITY_EDITOR
        static string ACCOUNT_NAME = "EditAccountName";
        public static string accountName
        {
            get
            {
                return UnityEditor.EditorPrefs.GetString(ACCOUNT_NAME, "無名氏");
            }
            set
            {
                UnityEditor.EditorPrefs.SetString(ACCOUNT_NAME, value);
            }
        }

        [MenuItem("GameObject/設計工具/Issue", false, 10)]
        private static void CreateIssue()
        {
            var issue = new GameObject("Issue");
            issue.AddComponent<Issue>();
            //set position to center of scene
            // issue.transform.position = SceneView.lastActiveSceneView.
            var centerPosition = SceneView.lastActiveSceneView.pivot;
            issue.transform.position = centerPosition;
        }
    #endif
    }


    public class Issue : AbstractMapTag
    {
#if UNITY_EDITOR
        [OnValueChanged(nameof(FetchInlineFavoriteComponents))] [Header("相關物件")]
        public GameObject[] RelatedComponents;

        [PreviewInInspector] [Header("相關捷徑")] private InlineFavoriteComponent[] InlineFavoriteComponents;
        private void FetchInlineFavoriteComponents()
        {
            InlineFavoriteComponents = new InlineFavoriteComponent[RelatedComponents.Length];
            for (var i = 0; i < RelatedComponents.Length; i++)
            {
                var relatedComponent = RelatedComponents[i];
                InlineFavoriteComponents[i] = relatedComponent.GetComponent<InlineFavoriteComponent>();
            }
        }
#endif
        
        [Header("描述---")]
        [EnumToggleButtons]
        public IssueType type; //好像不用？應該看有沒有Resolve就好，可以上色
        [LabelText("Issue類型")]
        public IssueToAssign issueToAssign = IssueToAssign.Implementation實作內容;
        public List<PersonSO> assignTo;

        // [HideInInspector]
        public string author = "";
        [InlineEditor] [ShowIf("comments")] [ShowInInspector]
        private List<Comment> comments;

        [TabGroup("外部連結")] public string NotionPageURL = "";

        [TabGroup("外部連結")]
        [Button("開啟Notion Issue")]
        private void OpenLinkPage()
        {
            Application.OpenURL(NotionPageURL);
        }

     
        
    #if UNITY_EDITOR
        protected override Color GizmoColor
        {
            get
            {
                if (IsCustomColor)
                {
                    return customColor;
                }
                else
                {
                    if (type == IssueType.Resolved)
                        return Color.blue;
                    else if (type == IssueType.Todo)
                        return Color.green;
                    else if (type == IssueType.Fixme)
                        return Color.red;
                    else if (type == IssueType.Question) return Color.yellow;
                }

                return base.GizmoColor;
            }
        }
        [LabelText("static使用者暱稱(自行更改)")]
        [ShowInInspector]
        public string MyName
        {
            get
            {

                return IssuePrefs.accountName;
            }
            set
            {
                // _author = value;
                IssuePrefs.accountName = value;
                
            }
        }




        [Button("解決")]
        public void Resolve()
        {
            var resolved = UnityEditor.Undo.AddComponent<IssueResolved>(gameObject);
            type = IssueType.Resolved;
            resolved.author = IssuePrefs.accountName;// UnityEditor.CloudProjectSettings.userName;
            gameObject.SetActive(false);
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        private bool IsResolved => type == IssueType.Resolved;

        [ShowIf("IsResolved")]
        [Button("重啟")]
        public void ReOpen()
        {
            type = IssueType.Fixme;
            UnityEditor.EditorUtility.SetDirty(gameObject);
            gameObject.SetActive(true);
            // resolved.author = UnityEditor.CloudProjectSettings.userName;
        }

        [Button("留言")]
        public void Comment()
        {
            var comment = gameObject.AddChildrenComponent<Comment>("comment");
            comment.author = IssuePrefs.accountName;
            comments.Add(comment);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            //issue不要改gameobject名稱
            //是因為想要把issue放在note下面嗎？
            GetComponentsInChildren<Comment>(true, comments);
        }

        private GUIStyle style; // = new(GUI.skin.label);
        protected override void OnDrawGizmos()
        {
            
            var color = Gizmos.color = GizmoColor;
            if (style == null) style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            // UnityEditor.Handles.color = color;
            // UnityEditor.Handles.DrawWireDisc(origin.position, new Vector3(0, 0, 1), radious);
            style.fontSize = fontSize;
            UnityEditor.Handles.Label(transform.position + Vector3.right * 10, name, style);
            Gizmos.DrawSphere(transform.position, 5);
        }
    #endif    
    }

}
