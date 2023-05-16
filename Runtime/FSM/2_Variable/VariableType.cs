using System;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Events;

[Searchable]
public class VariableType<TScriptableData, TField, TType> : AbstractVariable, IResetter, ISelfValidator,
    IGameStateOwner
    where TScriptableData : AbstractScriptableData<TField, TType> where TField : FlagField<TType>, new()
{
    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        // if (OdinPrefabUtility.GetPrefabKind(this) == PrefabKind.PrefabInstance)
//             //檢查有沒有綁定data
//             if (EditorUtility.IsPersistent(this)) return;
//             if (MustGenButNotYet())
//                 Debug.LogError("Instance需要生flag data", this);

//         //好像也不用傳了？
//        

        // GenData();
#endif
    }

    private bool AutoGenCheck()
    {
        #if UNITY_EDITOR
        if (PrefabKindMatchTagCheck() && IsAutoGen)
        {
            if (scriptableData == null)
            {
                Debug.Log("Empty, About to Auto Gen" + myPrefabKind, this);
                GenData();
                if (scriptableData)
                    return true;
            }
            else if (IsGameStateSaveIDNotMatch())
            {
                Debug.Log("SaveID NotMatch, About to Auto Gen" + myPrefabKind, this);
                GenData();
                if (scriptableData)
                    return true;
            }
        }
        #endif

        return true;
    }
#if UNITY_EDITOR
    private bool PrefabKindMatchTagCheck()
    {
        if (myPrefabKind == PrefabKind.NonPrefabInstance) //場景上的非prefab給過
            return true;
        
        
        var tag = GetComponent<GameStateRequireAtPrefabKind>();

        if (tag == null) return false; //[]: 該給過嗎？ 不該，要不然prefab會很吵
        if ((tag.prefabKind & myPrefabKind) != 0) return true;
        return false; //不是那個環境就不用顯示了
    }
#endif
    private bool IsCheckingPrefabKind => GetComponent<GameStateRequireAtPrefabKind>() != null;

    // [BoxGroup("GameState")]
    // [EnableIf("PrefabKindMatchTagCheck")]
    // // [DisableIf("@!IsAutoGenButNotYet()")] //FIXME: 用validate檢查
    // [Button("Auto Gen Fix")]
    // [EditorOnly]
    private void GenData()
    {
#if UNITY_EDITOR
        //get type of scriptableData field using reflection
        var type = GetType().GetField("scriptableData").FieldType;
        scriptableData =
            type.CreateGameStateSO(this) as TScriptableData;
        this.SetDirty();
        Debug.Log("自動生成flag修正" + scriptableData, scriptableData);
#endif
        //FIXME: 用validator檢查，然後自動Fix?
        //[]:已經在Auto那邊用OnBeforeSerialize全部做掉了
    }

    [BoxGroup("GameState")]
    [LabelText("自動生成")]
    [ShowInInspector]
    private bool IsAutoGen //TODO: IsAutoGen?
    {
        get
        {
            if (GetComponent<AutoGenGameState>() != null)
                return true;
            return false;
        }
    }

#if UNITY_EDITOR
    private bool IsAutoGenButNotYet()
    {
        if (!IsAutoGen) return false;
        return scriptableData == null;
    }

    private bool IsGameStateRequiredButMissing()
    {
        if (PrefabKindMatchTagCheck() && scriptableData == null)
            return true;
        return false;
    }

    private bool IsSuggestingAutoGen()
    {
       
        if (IsAutoGen) return false;
        return scriptableData == null;
    }


    private bool IsSuggestingDesignTag()
    {
        return gameObject.IsInPrefab() || myPrefabKind == PrefabKind.NonPrefabInstance;
    }
#endif

    //TODO: 可以直接弄到drawer上？
    [BoxGroup("GameState")]
    [HideInInlineEditors]
    [EnableIf("IsSuggestingDesignTag")]
    [HideIf("IsAutoGen")] //[]: 已經裝了的話要藏嗎？ 還是應該要透明
    [Button("[Prefab設計]Add AutoGen GameState")]
    private void AddTag()
    {
        this.TryGetCompOrAdd<AutoGenGameState>();
    }

    [BoxGroup("GameState")]
    [HideIf("IsCheckingPrefabKind")] //[]: 已經裝了的話要藏嗎？
    [EnableIf("IsSuggestingDesignTag")]
    [Button("[Prefab設計]Add GameState Require Tag")]
    private void AddRequireInPrefab()
    {
        this.TryGetCompOrAdd<GameStateRequireAtPrefabKind>();
    }

    //  MustGenScriptableDataTag mustGenTag; //提醒一定要gen flag
#if UNITY_EDITOR


    //lazy get prefabKind
    private PrefabKind _myPrefabKind;

    [ShowInInspector] private PrefabKind myPrefabKind => OdinPrefabUtility.GetPrefabKind(this);
    // private PrefabKind myPrefabKind => _myPrefabKind == PrefabKind.None
    //     ? _myPrefabKind = OdinPrefabUtility.GetPrefabKind(this)
    //     : _myPrefabKind;

    //FIXME: 這個可以cache嗎...
#endif

    // [ShowDrawerChain]
    [BoxGroup("GameState")]

    //給非Auto的人看的，要綁，Auto自己就會生，就結束了
    [InfoBox("需要綁GameState!", InfoMessageType.Error, "IsGameStateRequiredButMissing")]
    // [HideIf("VariableSource")]

    //FIXME: 這個錯了...要有特定設計tag，才是在prefab上不要gen
    // [EnableIn(PrefabKind.InstanceInScene | PrefabKind.NonPrefabInstance)] //scriptable binding, 只想要在景裡編輯
    [Header("存檔")]
    [GameState]
    [InlineEditor()]
    [EnableIf("PrefabKindMatchTagCheck")]
    // [DisableIf("IsAutoGen")]
    //FIXME: IsSceneAutoGen, PrefabMustGen?
    //TODO: 這個可以自動拿掉然後修起來嗎？
    [InfoBox("SaveID不一致, 清掉重綁", InfoMessageType.Error, "IsGameStateSaveIDNotMatch")]
    [InfoBox("GameState的類型不對", InfoMessageType.Error, "IsGameStateTypeNotMatch")]
    // [ValidateInput("AutoGenCheck", "自動生成檢查失敗")]
    public TScriptableData scriptableData;


    //<summary> 用來檢查auto gen時, 但是saveID不對 </summary>
#if UNITY_EDITOR
    private bool IsGameStateSaveIDNotMatch() //需檢查情境：複製時，造成綁到同一個gameState ref, 檢查saveID
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
#endif


    // <summary> 用來檢查是否有auto gen, 但是type不對 </summary>
    private bool IsGameStateTypeNotMatch()
    {
        if (scriptableData == null) return false;

        var autoComp = GetComponent<AutoGenGameState>();
        if (autoComp != null)
        {
            //有auto gen, 但是type不對
            if (scriptableData.gameStateType != GameFlagBase.GameStateType.AutoUnique) return true;
        }
        else
        {
            if (scriptableData.gameStateType != GameFlagBase.GameStateType.Manual)
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

    [ShowInPlayMode]
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

    [ShowInPlayMode]
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
        // this.Log("[VariableType] Before local Reset" + localField.CurrentValue, gameObject);
        localField.Reset();
        // this.Log("[VariableType] After local Reset" + localField.CurrentValue, gameObject);
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

    public void Validate(SelfValidationResult result)
    {
#if UNITY_EDITOR

        if (IsAutoGen)
        {
            //不在景裏，不需要
            if ((OdinPrefabUtility.GetPrefabKind(this) & PrefabKind.InstanceInScene) == 0) return;
            if (IsAutoGenButNotYet()) result.AddError("需要GameState Not Gen").WithFix(GenData);
        }
        
        if (IsGameStateSaveIDNotMatch()) result.AddError("SaveID不一致, 清掉重綁").WithFix(GenData);
#endif
    }

    // public void OnBeforeSerialize() //存檔的時候就會自動生，複製、instance的時候也會自動生
    // {
    //     // throw new System.NotImplementedException();
    //     if (EditorUtility.IsPersistent(this)) return;
    //     AutoGenCheck();
    // }
    //
    // public void OnAfterDeserialize()
    // {
    //     // throw new System.NotImplementedException();
    // }
}

public abstract class AbstractVariable : AbstractFlag
{
    protected virtual void Awake()
    {
    }

    [ShowIf("VariableSource")] [InlineEditor]
    public AbstractVariable VariableSource; //用別人的值 //FIXME: 什麼時候會用到這個？

    [ReadOnly] public List<AbstractVariableConsumer> consumers; //有誰有用我，binder綁一下
}