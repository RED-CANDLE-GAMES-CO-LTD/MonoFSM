using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Sirenix.OdinInspector;
using MonoFSM.Core.Attributes;
using RCGExtension;
using MonoDebugSetting;
using MonoFSM.Variable;
using MonoFSM.Foundation;


//還是Condition要用Is開頭？
public abstract class AbstractConditionBehaviour : AbstractDescriptionBehaviour, IBoolProvider, IOverrideHierarchyIcon,
    IHierarchyValueInfo
{
#if UNITY_EDITOR
    [ExcludeFromCodeCoverage]
    public string IconName { get; }
    [ExcludeFromCodeCoverage]
    public bool IsDrawingIcon => true;
    [ExcludeFromCodeCoverage]
    public Texture2D CustomIcon =>
        UnityEditor.EditorGUIUtility.ObjectContent(null, typeof(AbstractConditionBehaviour)).image as Texture2D;
    //UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.rcgmaker.fsm/RCGMakerFSMCore/Runtime/2_Variable/VarFloatIcon.png");
#endif
//     [Button]
//     [ShowIf("IsShowRenameButton")]
//     protected void RenameOfGameObject()
//     {
//         try
//         {
//             var text = "[Condition] " + Description;
//             if (FinalResultInverted)
//                 text = "[Condition] Not " + Description;
//             gameObject.name = text;
// #if UNITY_EDITOR
//             UnityEditor.EditorUtility.SetDirty(gameObject);
// #endif
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError(e,this);
//         }
//     }
    protected override string DescriptionTag => "Condition";

    protected override string DescriptionPreprocess(string description)
    {
        var text = base.DescriptionPreprocess(description);
        return FinalResultInverted ? " Not " + text : text;
    }

    // protected override string Description => 


    // protected virtual bool IsShowRenameButton => Description != "";
    //
    // //FIXME: AI 可以解釋性？
    // //FIXME: 整合 Description, interface?
    // protected virtual string Description => this.GetType().Name;


    //FIXME: 可是 _parentTransition等著被call
    // public Action OnConditionChanged; //要用這個？還是用polling就好了
    //直接用interface往上叫好像不錯？
    private bool _isConditionChanged = false;

    //用類似statData 檢查dirty來決定要不要重新檢查condition
    public bool IsDirty => _isConditionChanged;

    public virtual bool IsInvertResultOptionAvailable => true;

    [ShowIf(nameof(IsInvertResultOptionAvailable))]
    public bool FinalResultInverted = false;

    protected abstract bool IsValid { get; }

    [ShowInPlayMode]
    public bool FinalResult
    {
        get
        {
            if (Application.isPlaying == false)
                return false;
#if UNITY_EDITOR

            //Debug用，暫時強迫覆蓋值 (ex: 裝備可以在路上換)
            if (_debugConditionResultOverrider != null && IsDebugMode)
                return _debugConditionResultOverrider.OverrideResultValue;
#endif
            //之前都沒有...
            // if (isActiveAndEnabled == false)
            //     return false;
            //FIXME: 關著表示不判...

            if (FinalResultInverted)
                return !IsValid;

            return IsValid;
        }
    }


#if UNITY_EDITOR
    [ShowIf("IsDebugMode")] [PropertyOrder(1)] [TabGroup("Debug")] [Component] [AutoChildren(false)]
    protected DebugConditionResultOverrider _debugConditionResultOverrider;

    [ShowIf("IsDebugMode")]
    [ShowInInspector]
    [TabGroup("Debug")]
    public bool OverrideValue =>
        _debugConditionResultOverrider != null && _debugConditionResultOverrider.OverrideResultValue;

    private static bool IsDebugMode => DebugSetting.IsDebugMode;
#endif

    //For Cheat Code
    public virtual void CheatComplete()
    {
        Debug.LogError("This Condition Can't ForceSetValid");
    }

    public bool IsTrue => FinalResult;
    public string ValueInfo => FinalResult.ToString();
    public bool IsDrawingValueInfo => Application.isPlaying && isActiveAndEnabled;
}