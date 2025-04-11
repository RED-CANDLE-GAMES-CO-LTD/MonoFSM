using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityToolbarExtender;

public class AnimationClipSearchWindow : OdinEditorWindow, IToolbarWindow
{
    [MenuItem("Window/赤燭RCG/AnimationClipSearchWindow")]
    public static void ShowCustomWindow()
    {
        GetWindow<AnimationClipSearchWindow>().Show();
    }


    // private void CreateGUI()
    // {
    //     OnSelectionChange();
    //     // FindAllGUIDComponent();
    // }

    // public GuidComponent[] guidComponents;

    // void FindAllGUIDComponent()
    // {
    //     guidComponents = FindObjectsOfType<GuidComponent>();
    // }

    public static void EditAnimation(AnimationClip clip)
    {
        var animationWindow = GetWindow<AnimationWindow>(false);
        var sceneView = GetWindow<SceneView>();
        sceneView.Focus();
        animationWindow.Focus();
        animationWindow.animationClip = clip;
        _currentEditingClip = clip;
        // GetWindow<AnimationClipSearchWindow>().dropDownClip = clip;
        animationWindow.previewing = true;
        Debug.Log("EditAnimation Focus");
    }

    static UnityEngine.Object GetOpenAnimationWindow()
    {
        UnityEngine.Object[] objectsOfTypeAll = Resources.FindObjectsOfTypeAll(typeof (AnimationWindow));
        AnimationWindow instance;
        if (objectsOfTypeAll.Length != 0)
        {
            instance = (AnimationWindow) objectsOfTypeAll[0];
            return instance;
        }

        return null; //GetWindow<AnimationWindow>(false, "Animation", false);
    }
    // static UnityEngine.Object GetOpenAnimationWindow()
    // {
    //     UnityEngine.Object[] openAnimationWindows = Resources.FindObjectsOfTypeAll(GetAnimationWindowType());
    //     if (openAnimationWindows.Length > 0)
    //     {
    //         return openAnimationWindows[0];
    //     }
    //
    //     return null;
    // }

    private static Type animationWindowType = null;

    private static Type GetAnimationWindowType()
    {
        if (animationWindowType == null)
        {
            animationWindowType = System.Type.GetType("UnityEditor.AnimationWindow,UnityEditor");
        }

        return animationWindowType;
    }

    static AnimationClip GetAnimationWindowCurrentClip()
    {
        UnityEngine.Object w = GetOpenAnimationWindow();
        if (w != null)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            FieldInfo animEditor = GetAnimationWindowType().GetField("m_AnimEditor", flags);

            Type animEditorType = animEditor.FieldType;
            System.Object animEditorObject = animEditor.GetValue(w);
            FieldInfo animWindowState = animEditorType.GetField("m_State", flags);
            Type windowStateType = animWindowState.FieldType;

            PropertyInfo propInfo = windowStateType.GetProperty("activeAnimationClip");
            AnimationClip clip = (AnimationClip)propInfo.GetValue(animWindowState.GetValue(animEditorObject));
            // Debug.Log("CurrentClip" + clip, clip);

            return clip;
        }

        return null;
    }

    // private void OnBecameVisible()
    // {
    //     currentEditingClip = GetAnimationWindowCurrentClip();
    // }
    //
    // private void OnFocus()
    // {
    //     //點進來可以硬拿一下現在的clip，還行
    //     Debug.Log("OnFocus");
    //     currentEditingClip = GetAnimationWindowCurrentClip();
    // }

    // private void OnInspectorUpdate()
    // {
    //     //這個也太髒...
    //     
    //     if(mouseOverWindow)
    //         currentEditingClip = GetAnimationWindowCurrentClip();
    // }

    // [TabGroup("Animator Controller")] public AnimatorOverrideController currentAnimatorController;

    // private void SelectAnimatorController(AnimatorOverrideController controller)
    // {
    //     currentAnimatorController = controller;
    //     // if (currentAnimatorController)
    //     // {
    //     //     layers = currentAnimatorController.layers;
    //     //     if (layers.Length > index)
    //     //         states = layers[index].stateMachine.states;
    //     // }
    // }


    private static AnimationClip _currentEditingClip;

    //TODO: 直接set值會recursive嗎？
    // [OnValueChanged("SelectClipDropDown", InvokeOnUndoRedo = false)]
    // [ValueDropdown("GetListOfAnimationClip", OnlyChangeValueOnConfirm = true, IsUniqueList = true)]
    [ReadOnly] [ShowInInspector] public AnimationClip dropDownClip => _currentEditingClip;

    // private void SelectClipDropDown()
    // {
    //     EditAnimation(dropDownClip);
    // }

    // private IEnumerable<AnimationClip> GetListOfAnimationClip()
    private IList<ValueDropdownItem<AnimationClip>> GetListOfAnimationClip()
    {
        // ValueDropdownList<AnimationClip> myVectorValues = new ValueDropdownList<AnimationClip>();
        if (CurrentRuntimeAnimatorController)
            return Array.ConvertAll(CurrentRuntimeAnimatorController.animationClips,
                value => new ValueDropdownItem<AnimationClip>(value.name, value)).OrderBy(x => x.Text).ToList();
        //TODO: 拿overrider的
        // if(lastRuntimeAnimatorController)
        //     return lastRuntimeAnimatorController.animationClips.OrderBy(x=>x.name);
        return null;
    }


    [Serializable]
    // [InlineProperty(LabelWidth = 1)]
    public class AnimationClipEntry
    {
        public override string ToString()
        {
            return ClipName();
        }

        [ReadOnly] [HorizontalGroup("row")] [HideLabel] [GUIColor("GetClipBGColor")]
        public AnimationClip clip;

        [HideInInspector] public AnimationClip baseClip;

        // [ReadOnly]
        [HorizontalGroup("row")]
        // [LabelText("Override")]
        // [LabelWidth(32)]
        // [DoubleClickLabel("Edit")]
        [LabelText("$ClipName", SdfIconType.Pencil)]
        [GUIColor("GetClipBGColor")]
        [PropertyTooltip("@baseClip != null ? baseClip.name : clip.name")]
        //不一定有baseClip, 不一定是override controller
        public bool isOverrided = false;

        private Color GetClipBGColor()
        {
            if (_currentEditingClip == clip)
                //(0.1f, 0.5f, 0.2f, 0.3f)
                return _highlightColor;
            return Color.white;
        }

        private static Color _highlightColor = new(0.15f, 0.4f, 0.25f, 1f);

        private string ClipName()
        {
            return clip.name;
        }

        [HorizontalGroup("row",width:32)]
        [Button]
        private void Edit()
        {
            if (!IsCurrentSelectUnderAnimator && AvailableAnimator)
                Selection.activeGameObject = AvailableAnimator.gameObject;
            EditAnimation(clip);
        }
    }

    // [TableList]
    [Searchable(Recursive = false, FilterOptions = SearchFilterOptions.ValueToString)]
    [ListDrawerSettings(IsReadOnly = true)]
    [LabelText(":")]
    public AnimationClipEntry[] clipEntries;

    public GameObject lastSelectedGameObject;

    // [Searchable]
    // public AnimationClip[] clips;
    //TODO: dropdown?
    [ReadOnly] [ShowInInspector] [LabelText("Animator Controller")]
    public RuntimeAnimatorController CurrentRuntimeAnimatorController;

    private void FindAllClipOfAnimator(Animator animator)
    {
        // if (lastRuntimeAnimatorController == animator.runtimeAnimatorController && clipEntries.Length !=0)
        //     return;
        CurrentRuntimeAnimatorController = animator.runtimeAnimatorController;


        if (animator.runtimeAnimatorController == null)
            return;

        //有裝OverrideController
        if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
        {
            var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(list);
            //TODO: override controller可能會有重複的clip

            clipEntries = list.Where(entry => entry.Value != null)
                .Select(entry => new AnimationClipEntry
                    { baseClip = entry.Key, clip = entry.Value, isOverrided = true })
                .OrderBy(x => x.clip.name).ToArray();

            return;
        }

        //TODO: Projectile 全部都沒有override，誤以為沒有動畫？
        //要怎麼顯示比較好...打勾勾說要不要只顯示override？

        var clips = animator.runtimeAnimatorController.animationClips;
        clipEntries = Array.ConvertAll(clips, clip => new AnimationClipEntry { clip = clip }).OrderBy(x => x.clip.name)
            .ToArray();
    }

    private static bool IsCurrentSelectUnderAnimator;
    private static Animator AvailableAnimator;
//test

    private void OnSelectionChange()
    {
        if (!Selection.activeGameObject) return;
        IsCurrentSelectUnderAnimator = false;
        //在Animator的tree上
        //點到會有Animator的Prefab的State之類的，但不在Animator下
        // var hasAnimProvider = FindAnimatorProvider();
        // if (hasAnimProvider) return;
        //一般找animator in parent
        // Debug.Log("OnSelectionChange: " + Selection.activeGameObject);
        var anim = Selection.activeGameObject.GetComponentInParent<Animator>();
        if (!anim) return;
        

        IsCurrentSelectUnderAnimator = true;
        AvailableAnimator = anim;
        FindAllClipOfAnimator(anim);
    }

    // [Button("ForceFind")]
    private bool FindAnimatorProvider()
    {
        _currentEditingClip = GetAnimationWindowCurrentClip();
        var animProvider = Selection.activeGameObject.GetComponentInParent<IAnimatorProvider>();
        if (animProvider != null && animProvider.ChildAnimator != null)
        {
            AvailableAnimator = animProvider.ChildAnimator;
            FindAllClipOfAnimator(animProvider.ChildAnimator);
            return true;
        }

        AvailableAnimator = null;
        clipEntries = null;
        return false;
    }
}