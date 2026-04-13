using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MonoDebugSetting;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.EditorExtension;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;

[Serializable]
public class ConditionGroup //AndGroup? //封裝的蠻好的...? 但是auto可能會遇到問題...
{
    public bool IsValid => _conditions.IsAllValid();

    [CompRef]
    [AutoChildren(DepthOneOnly = true, _isSelfInclude = false)]
    // [SerializeField]
    private AbstractConditionBehaviour[] _conditions; //&&

    //這裡直接做 And OR 更方便？default And, 會沒注意到嗎，好像會耶
}

//還是Condition要用Is開頭？
public abstract class AbstractConditionBehaviour
    : AbstractDescriptionBehaviour,
        IBoolProvider,
        IOverrideHierarchyIcon,
        // IValueProvider<bool>, //FIXME: 不該作為ValueProvider? 要的話另外轉換好了？
        IValueProvider<bool>, IValueProvider<float>
{
#if UNITY_EDITOR
    [ExcludeFromCodeCoverage]
    public string IconName => Selection.activeGameObject == gameObject ? "d__Help@2x" : "_Help@2x"; //UnityEditor.EditorGUIUtility.ObjectContent(null, typeof(AbstractConditionBehaviour)).image.name;

    [ExcludeFromCodeCoverage]
    public bool IsDrawingIcon => true;

    [ExcludeFromCodeCoverage]
    public Texture2D CustomIcon => null;
    // UnityEditor.EditorGUIUtility.ObjectContent(null, typeof(AbstractConditionBehaviour)).image as Texture2D;
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
    protected override string DescriptionTag => FinalResultInverted ? "Not" : "If";

    //FIXME: formatName會把這個尬爛...
    // public override string Description =>
    //     FinalResultInverted ? "\"Not\" " + base.Description : base.Description;

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
    [Tooltip(
        "If true, the final result will be inverted. For example, if the condition is fulfilled when pressing a button normally, setting this to true will make it fulfilled when the button is not pressed."
    )]
    public bool FinalResultInverted = false;

    protected abstract bool IsValid { get; }
    [ShowInPlayMode] private bool _cachedFinalResult;
#if UNITY_EDITOR
    [Serializable]
    private struct ConditionResultRecord
    {
        public float _time;
        public bool _result;
        public override string ToString() => $"[{_time:F2}] {_result}";
    }

    [ShowInDebugMode] [SerializeField]
    private List<ConditionResultRecord> _resultHistory = new List<ConditionResultRecord>(10);

    [Conditional("UNITY_EDITOR")]
    private void RecordResult(bool result)
    {
        _cachedFinalResult = result;
        if (_resultHistory.Count >= 10)
            _resultHistory.RemoveAt(0);
        _resultHistory.Add(new ConditionResultRecord { _time = Time.time, _result = result });
    }
#endif
    public bool FinalResult
    {
        get
        {
            if (Application.isPlaying == false)
                return false;
#if UNITY_EDITOR

            //Debug用，暫時強迫覆蓋值 (ex: 裝備可以在路上換)
            if (_debugConditionResultOverrider != null && IsDebugMode)
            {
                var overrideResult = _debugConditionResultOverrider.OverrideResultValue;
                RecordResult(overrideResult);
                return overrideResult;
            }
#endif
            //之前都沒有...
            // if (isActiveAndEnabled == false)
            //     return false;
            //FIXME: 關著表示不判...

            var finalResult = FinalResultInverted ? !IsValid : IsValid;
#if UNITY_EDITOR
            RecordResult(finalResult);
#endif
            return finalResult;
        }
    }

#if UNITY_EDITOR
    [ShowIf("IsDebugMode")]
    [PropertyOrder(1)]
    [TabGroup("Debug")]
    [Component]
    [AutoChildren(false)]
    protected DebugConditionResultOverrider _debugConditionResultOverrider;

    private float _value;

    [ShowIf("IsDebugMode")]
    [ShowInInspector]
    [TabGroup("Debug")]
    public bool OverrideValue =>
        _debugConditionResultOverrider != null
        && _debugConditionResultOverrider.OverrideResultValue;

    private static bool IsDebugMode => RuntimeDebugSetting.IsDebugMode;
#endif

    //For Cheat Code
    public virtual void CheatComplete()
    {
        Debug.LogError("This Condition Can't ForceSetValid");
    }

    public bool IsTrue => FinalResult;
    public bool Value => FinalResult;

    //interface & implementation的關係，所以我也可以說安裝一個schema, 然後下面再補variable....可能自動補掉就好了？(有就自動撈)
    public override string ValueInfo => FinalResult.ToString();
    public override bool IsDrawingValueInfo => Application.isPlaying;

    float IValueProvider<float>.Value => IsTrue ? 1f : 0f;

    public T1 Get<T1>()
    {
        if (typeof(T1) == typeof(bool))
            return (T1)(object)FinalResult;
        if (typeof(T1) == typeof(float))
            return (T1)(object)(FinalResult ? 1f : 0f);
        throw new InvalidCastException($"Cannot cast bool to {typeof(T1)}");
    }

    public Type ValueType => typeof(bool);
}
