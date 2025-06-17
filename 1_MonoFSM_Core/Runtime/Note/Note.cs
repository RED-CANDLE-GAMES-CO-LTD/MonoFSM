using MonoFSM.Core.Attributes;
using MonoFSM.Editor.DesignTool;
using UnityEngine;
using Sirenix.OdinInspector;


//Just for remind
public class Note : MonoBehaviour, IEditorOnly //IOverrideHierarchyIcon
{
    //FIXME: 用vHierarchyIcon來做?
    // [EnumToggleButtons]
    // public NoteType type = NoteType.NOTE; //FIXME:拿掉這個好了，不需要 ，用issue來做丟接球

    // public bool IsShow = false; //default 景裡可以看到
    // [ShowIf("IsShow")]
    // public Vector3 offset;
    // [ShowIf("IsShow")]
    // public Color TextColor = Color.white;
    // [ShowIf("IsShow")]
    // public int fontSize = 24;

    [Button("開Issue")]
    void AddIssue()
    {
        //issue想要獨立節點嗎？好像不需要，反而直接裝在有問題的東西旁邊比較好
        this.AddChildrenComponent<Issue>("issue");
    }
    public enum NoteType
    {
        NOTE,
        TODO,
        FIXME
    }
#if UNITY_EDITOR
    [SerializeField] private NoteType _noteType = NoteType.NOTE;
    [TextArea(5,100)]
    public string note;

    [ColorPalette] public Color bgColor = Color.yellow; //fixme:color 應該直接照著類型，和IDE這個註解一樣
#endif  
    public string IconName => "_Help";
    public bool IsDrawingIcon => false;
}