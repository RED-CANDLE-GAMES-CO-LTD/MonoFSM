using System;
using MonoFSM.Editor.DesignTool;
using MonoFSM.EditorExtension;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

//Just for remind

public class
    Note : AbstractDescriptionBehaviour, IEditorOnly //IOverrideHierarchyIcon
{
    public override string Description => $"[{_noteType}]";
#if UNITY_EDITOR
    //FIXME: 用vHierarchyIcon來做?
    // [EnumToggleButtons]
    //FIXME: hoverable note, 不要直接顯示
    // public NoteType type = NoteType.NOTE; //FIXME:拿掉這個好了，不需要 ，用issue來做丟接球

    // public bool IsShow = false; //default 景裡可以看到
    // [ShowIf("IsShow")]
    // public Vector3 offset;
    // [ShowIf("IsShow")]
    // public Color TextColor = Color.white;
    // [ShowIf("IsShow")]
    // public int fontSize = 24;

    protected override bool IsIgnoreRename => gameObject.GetComponentCount() > 1;

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
        FIXME,
    }

    [GUIColor("BackgroundColor")]
    [SerializeField]
    private NoteType _noteType = NoteType.NOTE;

    [Obsolete]
    [TextArea(5, 100)]
    [Title("(舊規，搬到上面去) 意圖、Prompt")]
    public string note;
    // [ColorPalette] public Color bgColor = Color.yellow; //fixme:color 應該直接照著類型，和IDE這個註解一樣


    public string IconName => "_Help";
    public bool IsDrawingIcon => false;
    // public string ValueInfo => note;
    // public bool IsDrawingValueInfo => false; //TODO:
    //
    // public Color BackgroundColor => _noteType switch
    // {
    //     NoteType.NOTE => new Color(0.8f, 0.8f, 0.8f, 0.5f),
    //     //yellow
    //     NoteType.TODO => new Color(0.7f, 1f, 0.2f, 0.5f),
    //     //橘色
    //     NoteType.FIXME => new Color(1f, 0.3f, 0.5f, 0.5f),
    //     _ => Color.clear
    // };
    //
    // public bool IsDrawGUIHierarchyBackground =>
    //     _noteType == NoteType.FIXME || _noteType == NoteType.TODO;
#endif
}

