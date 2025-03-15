using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RCGMakerFSM.RCGMakerFSMCore.Tracking;
#if MIXPANEL
using mixpanel;
#endif
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.FSM._2_Variable.VariableBinder;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

//現在根本還沒做監聽，是用condition做polling
[Searchable]
public abstract class GenericMonoVariable<TScriptableData, TField, TType> : AbstractMonoVariable, ISettable<TType>,
    ISelfValidator,
    IGameStateOwner, IDefaultSerializable, ILevelResetPrepare
    where TScriptableData : AbstractScriptableData<TField, TType>
    where TField : FlagField<TType>, new()
    where TType : IEquatable<TType>
{
    //想要直接選一個field就拿他的值，應該抽出去做成一個新東西不要放在GenericVariable裡面
    //VariableFloat應該獨立寫？這樣就一定可以有一個最好的abstract class
    public void CommitValue()
    {
        Field.CommitValue();
    }

    public void SetValue(object value, MonoBehaviour byWho = null)
    {
        SetValue((TType)value, byWho);
    }

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

    private bool PrefabKindMatchTagCheck()
    {
#if UNITY_EDITOR
        if (myPrefabKind == PrefabKind.NonPrefabInstance) //場景上的非prefab給過
            return true;


        var tag = GetComponent<GameStateRequireAtPrefabKind>();

        if (tag == null) return false; //[]: 該給過嗎？ 不該，要不然prefab會很吵
        if ((tag.prefabKind & myPrefabKind) != 0) return true;
#endif
        return false; //不是那個環境就不用顯示了
    }

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

    [TabGroup("GameState")]
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
    [TabGroup("GameState")]
    [HideInInlineEditors]
    [EnableIf("IsSuggestingDesignTag")]
    [HideIf("IsAutoGen")] //[]: 已經裝了的話要藏嗎？ 還是應該要透明
    [Button("[Prefab設計]Add AutoGen GameState")]
    private void AddTag()
    {
        this.TryGetCompOrAdd<AutoGenGameState>();
    }

    [TabGroup("GameState")]
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

    [TabGroup("Data")] [InlineField] [HideIf(nameof(scriptableData))] [SerializeField]
    protected TField localField; // = new();

    //這個值會被蓋掉???

    [TabGroup("Data")] public TField Field => ScriptableData ? ScriptableData.field : localField;
    //給非Auto的人看的，要綁，Auto自己就會生，就結束了

    [InfoBox("需要綁GameState!", InfoMessageType.Error, "IsGameStateRequiredButMissing")]
    //FIXME: 這個錯了...要有特定設計tag，才是在prefab上不要gen
    // [EnableIn(PrefabKind.InstanceInScene | PrefabKind.NonPrefabInstance)] //scriptable binding, 只想要在景裡編輯
    [TabGroup("Data")]
    [Header("存檔")]
    [GameState]
    [InlineEditor()]
    [EnableIf(nameof(PrefabKindMatchTagCheck))]
    [InfoBox("SaveID不一致, 清掉重綁", InfoMessageType.Error, "IsGameStateSaveIDNotMatch")]
    [InfoBox("GameState的類型不對", InfoMessageType.Error, "IsGameStateTypeNotMatch")]
    // [ValidateInput("AutoGenCheck", "自動生成檢查失敗")]
    public TScriptableData scriptableData;


    //<summary> 用來檢查auto gen時, 但是saveID不對 </summary>
#if UNITY_EDITOR
    private bool IsGameStateSaveIDNotMatch() //需檢查情境：複製時，造成綁到同一個gameState ref, 檢查saveID
    {
        if (!IsAutoGen) return false;
        var autoComp = GetComponent<AutoGenGameState>();
        if (autoComp == null || scriptableData == null) return false;
        return autoComp.SaveID != scriptableData.GetSaveID;
        // Debug.LogError("SaveID不一致", this);
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


    public virtual TScriptableData ScriptableData => scriptableData; //FIXME:


    [PreviewInInspector] [Component] [AutoChildren]
    private AbstractVariableModifier<TType>[] modifiers;
//會有external modifier...

    [TabGroup("Data"), PreviewInInspector] public virtual TType FinalValue => CurrentValue;
    [TabGroup("Data"), PreviewInInspector] public virtual TType LastValue => Field.LastValue; //FIXME: 這裡沒有過到modifier

    public TType Value => CurrentValue;

    [ShowInPlayMode]
    public TType CurrentValue //FIXME: 改成Value?
    {
        get
        {
            Profiler.BeginSample("Variable GetValue");
            var tempValue = localField.CurrentValue;

            //FIXME: 這裡就有proxy? 而且還是直接reference...
            // if (VariableSource != null)
            // {
            //     var v = VariableSource as GenericMonoVariable<TScriptableData, TField, TType>;
            //     tempValue = v.CurrentValue;
            // }
            if (ScriptableData != null)
            {
                tempValue = ScriptableData.CurrentValue;
            }

            Profiler.EndSample();
            Profiler.BeginSample("AfterGetValueModifyCheck");
            //FIXME: 這個是不是有點貴？有需要在這層做嗎？應該在set時就做掉了？不需要ㄅ
            if (modifiers != null)
                foreach (var modifier in modifiers)
                    tempValue = modifier.AfterGetValueModifyCheck(tempValue);
            Profiler.EndSample();
            // this.Log("[Variable] Get", tempValue);
            return tempValue;
        }

        set //FIXME: 拿掉，用SetValue(
        {
            var tempValue = value;
            //先檢查會被修改

            if (modifiers != null)
                foreach (var modifier in modifiers)
                    tempValue = modifier.BeforeSetValueModifyCheck(tempValue);
            // this.Log("[Variable] Set", value); 
            if (ScriptableData == null)
            {
                if (localField.CurrentValue.Equals(tempValue)) return;
                // if (localField == null)
                //     localField = default(TField);
                localField.CurrentValue = tempValue;
            }

            else
            {
                if (ScriptableData.CurrentValue.Equals(tempValue)) return;
                if (FinalData == null) return;
#if MIXPANEL
                _trackValue.OnRecycle();
                _trackValue["Data"] = FinalData ? FinalData.name : "null";
                _trackValue["value"] = tempValue switch
                {
                    bool valueBool => valueBool,
                    int valueInt => valueInt,
                    float valueFloat => valueFloat,
                    _ => _trackValue["value"]
                };
                this.Track("Variable Changed", _trackValue);
#endif
                // Debug.Log("Set Value" + tempValue);

                ScriptableData.CurrentValue = tempValue;
            }
        }
    }

    // private MonoBehaviour lastValueSetter;

    HashSet<MonoBehaviour> byWhoHashSet = new();
    [PreviewInInspector] public List<MonoBehaviour> byWhoList => byWhoHashSet.ToList();

    protected override void SetValueInternal<T>(T value, Object byWho = null)
    {
        SetValue(value, byWho as MonoBehaviour);
    }

    public void SetValue(TType value, MonoBehaviour byWho = null)
    {
        // lastValueSetter = byWho;
        var tempValue = value;
        //先檢查會被修改

        if (modifiers != null)
            foreach (var modifier in modifiers)
                tempValue = modifier.BeforeSetValueModifyCheck(tempValue);
        //after?
        // Debug.Log("[Variable] Set" + value + "tempValue:" + tempValue + ", Value:" + CurrentValue, byWho);
        if (tempValue.Equals(CurrentValue)) return;
        byWho.Log("[Variable] Set", value);
        byWhoHashSet.Add(byWho);

        Field.SetCurrentValue(tempValue, byWho);

        if (FinalData == null) return;

        TrackValue(tempValue, byWho);
// #if MIXPANEL
//         _trackValue.OnRecycle();
//         _trackValue["Data"] = FinalData ? FinalData.name : "null";
//         _trackValue["byWho"] = byWho ? byWho.name : "null";
//         _trackValue["value"] = tempValue switch
//         {
//             bool valueBool => valueBool,
//             int valueInt => valueInt,
//             float valueFloat => valueFloat,
//             _ => _trackValue["value"]
//         };
//         this.Log("Set Value byWho", tempValue, "byWho", byWho);
//      
//         this.Track("Variable Changed", _trackValue);
// #endif
    }

    void TrackValue(TType value, MonoBehaviour byWho)
    {
        var trackValue = UserDataTracker.BorrowTrackableValue;
        if (trackValue == null) return;
        trackValue.SetProperty("Data", FinalData ? FinalData.name : "null");
        trackValue.SetProperty("byWho", byWho ? byWho.name : "null");
        trackValue.SetProperty("value", value);
        //FIXME: 還是這裡應該用trackValue.Track(...?)既然都包了
        UserDataTracker.Track("Variable Changed", trackValue);
    }
#if MIXPANEL
    private readonly Value _trackValue = new();
#endif
    //FIXME: 還需要這個嗎？
    // [AutoParent()] private IGameEntity gameEntity;
    //
    // [ShowInPlayMode]
    // private string GameStateID => gameEntity != null
    //     ? $"{gameObject.scene.name}_{gameEntity.name}_{gameObject.name}"
    //     : $"{gameObject.scene.name}_{gameObject.name}";

    //為了讀檔後才能設定？reset又要重置參數...


    // void IResetter.EnterLevelReset()
    // {
    //     // this.Log("[VariableType] Before local Reset" + localField.CurrentValue, gameObject);
    //     //Scene裡的物件沒有要存檔的必要，重置
    //     if (TestModeGameFlag.Instance)
    //         localField.Init(TestModeGameFlag.Instance.mode, this);
    //     else
    //     {
    //         localField.Init(TestMode.EditorDevelopment, this);
    //     }
    //     localField.ResetToDefault();
    //     this.Log("[VariableType] After local Reset" , localField.CurrentValue, gameObject);
    // }

    public void ExitLevelAndDestroy()
    {
        return;
    }

    public int GetPriority()
    {
        return -1;
    }

    //FIXME 不該用這個？
    // [HideInInlineEditors] public UnityEvent<TType> OnValueChanged = new();

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

    // public override GameFlagBase FinalData => ScriptableData ? ScriptableData : Sampledata;
    // [TabGroup("再說")] public GameFlagBase mainData;

    // [TabGroup("再說")] [ShowIf(nameof(mainData))] [ValueDropdown(nameof(GetAllFlagField))]
    // public string fieldOfMainData;
    //
    // public TField fieldOfMainDataValue => mainData.FindField<TType>(fieldOfMainData) as TField;
    //
    // private IEnumerable<string> GetAllFlagField()
    // {
    //     if (mainData == null) yield break;
    //     var fields = mainData.GetAllFlagFieldNames<TField>();
    //     foreach (var field in fields)
    //         yield return field;
    // }

    public override Type FinalDataType => typeof(TScriptableData);
    public override Type ValueType => typeof(TType);
    public override object objectValue => CurrentValue;


    public string Serialize()
    {
        return GetType().Name + ":" + localField.ProductionValue;
    }

    public void Deserialize(string data)
    {
        throw new NotImplementedException();
    }

    public void LevelResetPrepareRuntimeData()
    {
        localField.Init(TestMode.EditorDevelopment, this);
    }
}

public interface ISettable //FIXME: 有點蠢
{
    void CommitValue();

    //FIXME: 用T?
    void SetValue(object value, MonoBehaviour byWho = null);
}

public interface ISettable<in T> : ISettable
{
    void SetValue(T value, MonoBehaviour byWho = null);
}

public abstract class AbstractMonoVariable : MonoBehaviour, IGuidEntity, IName, IValueOfKey<VariableTag>
{
    public UnityAction OnValueChangedRaw; //任何數值改變就通知

    [Button]
    void UpdateTag()
    {
        varTag._variableType.SetType(GetType());
        varTag._valueFilterType.SetType(ValueType);
        // Debug.Log("Tag Changed");
        //variable folder refresh
        var variableFolder = GetComponentInParent<RCGVariableFolder>();
        if (variableFolder)
            variableFolder.Refresh();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(varTag);
#endif
    }

    [OnValueChanged(nameof(UpdateTag))]
    [Header("變數名稱")]
    [PropertyOrder(-1)]
    [Required]
    [SOConfig("VariableType", nameof(CreateTagPostProcess))]
    public VariableTag varTag; //直接看當下是什麼就可以

    protected void CreateTagPostProcess()
    {
        //FIXME: 從Drawer call 失敗了，感覺varTag還沒做好...
        // varTag._variableType.SetType(GetType());
        // varTag._valueFilterType.SetType(ValueType);
        // Debug.Log("CreateTagPostProcess" + varTag._variableType.RestrictType + varTag._valueFilterType.RestrictType,
        //     varTag);
    }

    // public abstract void CommitValue();
    // public abstract void SetValue(object value, MonoBehaviour byWho = null); //一開始就預設要可以Set了
    public abstract GameFlagBase FinalData { get; } //這是啥？
    public abstract Type FinalDataType { get; }
    public abstract Type ValueType { get; }

    public abstract object objectValue { get; }

    public virtual T GetValue<T>()
    {
        var value = objectValue;
        if (value == null)
            return default;
        try
        {
            return (T)value;
        }
        catch (Exception e)
        {
            Debug.LogError($"Cannot cast {value} to {typeof(T)}", this);
            return default;
        }
    }

    protected abstract void SetValueInternal<T>(T value, Object byWho = null);

    public void SetValue<T>(T value, MonoBehaviour byWho = null)
    {
        SetValueInternal(value, byWho);
        OnValueChangedRaw?.Invoke(); //通知有人改變了
        //FIXME: 如果還有什麼需要處理的？
    }

    public object GetProperty(string knownFieldName)
    {
        return GetPropertyCache(knownFieldName)?.Invoke(this);
    }

    public Dictionary<string, Func<AbstractMonoVariable, object>> propertyCache = new();

    //GameFlagDescriptable有一樣的東西喔
    public Func<AbstractMonoVariable, object> GetPropertyCache(
        string propertyName)
    {
        if (propertyCache.TryGetValue(propertyName, out var info))
            return info;


        var propertyInfo = GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        // Debug.Log($"Property {propertyName} found in {sourceObject.GetType()}", sourceObject);

        if (propertyInfo == null)
        {
            propertyCache[propertyName] = null;
            //FIXME: 可能因為unknownData所以有可能會找不到 有點危險？
            // Debug.LogError($"Property {propertyName} not found in {GetType()}");
            return null;
        }

        var getMethod = propertyInfo.GetGetMethod();
        if (getMethod == null)
        {
            Debug.LogError($"Property {propertyName} does not have a getter in {GetType()}"
            );
            return null;
        }

        Func<AbstractMonoVariable, object>
            getMyProperty = (source) => getMethod.Invoke(source, null);
        propertyCache[propertyName] = getMyProperty;
        return getMyProperty;
    }

#if UNITY_EDITOR
    [Header("GameState 功能說明")] [TextArea(1, 4)]
    public string description;
#endif

    // [HideInInlineEditors] [Header("Flag Setting")]
    // public FlagTypeScriptable typeScriptable;
    protected virtual void Awake()
    {
    }

    //FIXME: virtual variable?
    // [FormerlySerializedAs("VariableSource")]
    // [ShowIf("VariableSource")] 
    // [InlineEditor] public AbstractMonoVariable VariableSource; //用別人的值 //FIXME: 什麼時候會用到這個？

    [ReadOnly] public List<AbstractVariableConsumer> consumers; //有誰有用我，binder綁一下


    //FIXME: 這個是錯的，要改成用scriptableData的 (flagFlied的？
    // public UnityEvent ValueChangedEvent => valueChangedEvent;

    // [HideInInlineEditors] public UnityEvent valueChangedEvent;
    public string Name => gameObject.name;
    public VariableTag Key => varTag;

    public VariableTag[] GetKeys()
    {
        return new[] { varTag };
    }
}