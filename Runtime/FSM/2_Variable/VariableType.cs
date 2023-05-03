using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[Searchable]
public class VariableType<TScriptableData, TField, TType> : AbstractVariable, IResetter
    where TScriptableData : AbstractScriptableData<TField, TType> where TField : FlagField<TType>, new()
{
    protected virtual void OnValidate()
    {
// #if UNITY_EDITOR
//         // if (OdinPrefabUtility.GetPrefabKind(this) == PrefabKind.PrefabInstance)
//             //檢查有沒有綁定data
//             if (EditorUtility.IsPersistent(this)) return;
//             if (MustGenButNotYet())
//                 Debug.LogError("Instance需要生flag data", this);

//         //好像也不用傳了？
//         // GenData();
// #endif
    }

#if UNITY_EDITOR

    [DisableIf("@!IsAutoGenButNotYet()")] //FIXME: 用validate檢查
    [Button("Auto Gen Fix")]
    private void GenData()
    {
        // scriptableData = FlagGenerator.GenerateFlagForVariable(this, scriptableData);
        Debug.Log("自動生成flag修正" + scriptableData, scriptableData);
        //FIXME: 用validator檢查，然後自動Fix?
    }
#endif
    private bool IsAutoGen //TODO: IsAutoGen?
    {
        get
        {
            if (GetComponent<AutoGenGameState>() != null)
                return true;
            return false;
        }
    }

    private bool IsAutoGenButNotYet()
    {
        if (!IsAutoGen) return false;
        return scriptableData == null;
    }

    [HideInInlineEditors]
    [DisableIf("IsAutoGen")]
    [Button("[Prefab設計]Add Auto State Save")]
    private void AddTag()
    {
        this.TryGetCompOrAdd<AutoGenGameState>();
    }

    //  MustGenScriptableDataTag mustGenTag; //提醒一定要gen flag
    
    [ShowInInspector] private PrefabKind myPrefabKind => OdinPrefabUtility.GetPrefabKind(this);


    [ShowDrawerChain]
    [InfoBox("需要生GameState!", InfoMessageType.Error, "IsAutoGenButNotYet")]
    // [HideIf("VariableSource")]
    [EnableIn(PrefabKind.InstanceInScene | PrefabKind.NonPrefabInstance)] //scriptable binding, 只想要在景裡編輯
    [Header("存擋")]
    // [FormerlySerializedAs("boolFlag")]
    // [GameFlag]
    [GameState]
    [InlineEditor()]
    // [DisableIf("IsAutoGen")]
    //FIXME: IsSceneAutoGen, PrefabMustGen?
    [InfoBox("SaveID不一致, 清掉重綁", InfoMessageType.Error, "IsGameStateSaveIDNotMatch")]
    [InfoBox("GameState的類型不對", InfoMessageType.Error, "IsGameStateTypeNotMatch")]
    public TScriptableData scriptableData;

    private bool IsGameStateSaveIDNotMatch() //複製時，造成同個gameState ref, 檢查saveID
    {
        if (IsAutoGen)
        {
            var autoComp = GetComponent<AutoGenGameState>();
            if (autoComp != null && scriptableData != null)
                if (autoComp.SaveID != scriptableData.SaveID)
                    // Debug.LogError("SaveID不一致", this);
                    return true;
        }

        return false;
    }


    private bool IsGameStateTypeNotMatch()
    {
        if (scriptableData == null) return false;

        var autoComp = GetComponent<AutoGenGameState>();
        if (autoComp != null)
        {
            //有auto gen, 但是type不對
            if (scriptableData.type != GameFlagBase.GameStateType.AutoUnique) return true;
        }
        else
        {
            if (scriptableData.type != GameFlagBase.GameStateType.Manual)
                return true;
        }

        return false;
    }
    

    [ShowInInspector] [InlineEditor] public virtual TScriptableData ScriptableData => scriptableData; //FIXME:

    
    
    // [HideInInspector]
    // public UnityEvent ValueChangedEvent;
    [HideIf("VariableSource")] [HideIf("scriptableData")] [SerializeField]
    protected TField localField; // = new();
    

    public TField Field => ScriptableData ? ScriptableData.field : localField;

    // Start is called before the first frame update
    [AutoChildren(false)] private AbstractVariableModifier<TType>[] modifiers;

    public TType Value
    {
        get
        {
            if (VariableSource != null)
            {
                var v = VariableSource as VariableType<TScriptableData, TField, TType>;
                return v.Value;
            }
            else if (ScriptableData != null)
            {
                return ScriptableData.CurrentValue;
            }
            else
            {
                // if (localField == null)
                //     localField = new TField();
                return localField.CurrentValue;
            }
        }

        set
        {
            var tempValue = value;
            //先檢查會被修改

            if (modifiers != null)
                foreach (var modifier in modifiers)
                    tempValue = modifier.BeforeSetValueModifyCheck(tempValue);
            // this.Log("[Variable] Set", value); 
            if (ScriptableData == null)
                // if (localField == null)
                //     localField = default(TField);
                localField.CurrentValue = tempValue;
            else
                ScriptableData.CurrentValue = tempValue;
        }
    }
    
    [AutoParent()] private IGameEntity gameEntity;

    [RuntimeDisplay]
    private string GameStateID => gameEntity != null
        ? $"{gameObject.scene.name}_{gameEntity.name}_{gameObject.name}"
        : $"{gameObject.scene.name}_{gameObject.name}";

   
    private void Start()
    {
        // if (ScriptableData != null)
        //     ScriptableData.field.AddListener(FlagValueChange, this);
        // else
        // {
        //     localField ??= default(TField);
        //     Debug.Log("[Variable] Init local Field"+localField,gameObject);
        //     localField?.AddListener(FlagValueChange, this);
        // }
    }

    // void FlagValueChange(TType flagValue)
    // {
    //     // if (ValueChangedEvent != null)
    //     //     ValueChangedEvent.Invoke();
    //     Debug.Log("[Variable] Changed"+name,gameObject);
    //     OnValueChanged.Invoke(flagValue);
    //     //倒著接?
    // }

    void IResetter.EnterLevelResetAndStart()
    {
        Debug.Log("[VariableType] Before local Reset" + localField.CurrentValue, gameObject);
        localField.Reset();
        Debug.Log("[VariableType] After local Reset" + localField.CurrentValue, gameObject);
    }

    public void ExitLevelAndDestroy()
    {
        return;
    }

    public int GetPriority()
    {
        return -1;
    }

    [HideInInlineEditors] public UnityEvent<TType> OnValueChanged = new();
}

public class AbstractVariable : AbstractFlag
{
    protected virtual void Awake()
    {
    }

    [ShowIf("VariableSource")] [InlineEditor]
    public AbstractVariable VariableSource; //用別人的值 //FIXME: 什麼時候會用到這個？

    [ReadOnly] public List<AbstractVariableConsumer> consumers; //有誰有用我，binder綁一下
}