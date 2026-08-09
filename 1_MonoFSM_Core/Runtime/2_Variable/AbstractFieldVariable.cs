using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MonoDebugSetting;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.RCGMakerFSMCore.Tracking;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

interface IStringTokenVar
{
    string ValueInfo { get; }
}

[Searchable]
[DisallowMultipleComponent]
public abstract class AbstractFieldVariable<TScriptableData, TField, TType>
    : TypedMonoVariable<TType>,
        ISettable<TType>,
        IGameStateOwner,
        IDefaultSerializable,
        IReferenceTarget,
        ISceneStart
    where TScriptableData : AbstractScriptableData<TField, TType>
    where TField : FlagField<TType>, new()
// where TType : IEquatable<TType>
{
    public override bool IsDrawingValueInfo => true;
    // 遞迴檢測相關變數
    [System.NonSerialized]
    private int _recursionDepth = 0;
    private const int MAX_RECURSION_DEPTH = 50;

    public TType GetValue()
    {
        return CurrentValue;
    }

    //遞迴檢測交給 CurrentValue（這裡唯一的呼叫），這層不再包 try/finally 以便 inline
    public override T1 GetValue<T1>()
    {
        //typeof 比較 JIT 時會被摺疊掉
        if (typeof(TType) != typeof(T1))
            return default;
        var value = CurrentValue;
        return Unsafe.As<TType, T1>(ref value);
    }

    public override void SetRaw<T1>(T1 value, Object byWho)
    {
        Profiler.BeginSample("SetRaw");
        this.Log("SetRaw Value:", value, ", Type:", typeof(T1), this);
        var tValue = Unsafe.As<T1, TType>(ref value);
        SetValueInternal(tValue, byWho);
        Profiler.EndSample();
    }

    public override void SetValueFromVar(AbstractMonoVariable source, Object byWho)
    {
        SetValueInternal(source.Get<TType>(), byWho);
    }

    //還要看條件嗎？ conditional value switch
    //想要直接選一個field就拿他的值，應該抽出去做成一個新東西不要放在GenericVariable裡面
    //VariableFloat應該獨立寫？這樣就一定可以有一個最好的abstract class
    public void SetValue(TType value, Object byWho = null, string reason = null)
    {
        SetValueInternal(value, byWho, reason);
    }

    //--- 網路覆寫通道 ---
    //Getter 型 Var（有 valueSource）的 CurrentValue 是每次現算的，SetValue 寫進 Field 也讀不回來，
    //所以 proxy 端收到權威值時改走這條，讓 GetCurrentValueCore 直接回覆寫值。
    [ShowInDebugMode] [NonSerialized] private bool _isNetOverridden;

    [ShowInDebugMode] [NonSerialized] private TType _netValue;

    [ShowInDebugMode] private bool IsNetOverridden => _isNetOverridden;

    /// <summary>
    ///     NetworkedVarSync 的 proxy 端寫入口。有 valueSource 就走覆寫，
    ///     沒有的話行為與一般 SetValue 完全相同（不影響既有 Var）。
    /// </summary>
    public void SetValueFromNetwork(TType value, Object byWho)
    {
        if (!HasValueSource)
        {
            //fromNetwork：權威端寫出前已經 clamp 過了，非 SA 端再 clamp 一次只會製造分歧。
            //bound 常綁在 VarStat 的 FinalValue 上，而 StatModifier 的來源不見得都有同步
            //（例：Max Stamina 扣身上負重 / 搬運質量），兩端 bound 不同就會把權威值壓成別的數字，
            //表現為 UI 抖動。這條也是唯一能繞過網路權威 gate 的入口。
            SetValueInternal(value, byWho, "Network", true);
            return;
        }

        _isNetOverridden = true;
        _netValue = value;
    }

    public override void ClearNetworkOverride()
    {
        _isNetOverridden = false;
        _netValue = default;
    }

    public override void CommitValue()
    {
        this.Log("CommitValue", this);
        // Profiler.BeginSample("Field.CommitValue");
        var (last, current) = Field.CommitValue();
        // Profiler.EndSample();
        this.Log("CommitValue Commited", current, "Last Value", last, this);
        // Profiler.BeginSample("ValueCommited");
        ValueCommited(last, current);
        // Profiler.EndSample();
    }

    //可以用abstract比較好？但目前只用到VarFloat
    protected virtual void ValueCommited(TType lastValue, TType currentValue) { }

    /// <summary>
    /// 每次 SetValue 成功寫入後立即呼叫，oldValue 是寫入前的值，newValue 是寫入後的值。
    /// 與 ValueCommited 不同：這裡是每次 SetValue 都會觸發，不是等到 CommitValue。
    /// </summary>
    protected virtual void OnValueSet(TType oldValue, TType newValue) { }

    // public override void SetValue(object value, MonoBehaviour byWho)
    // {
    //     SetValueInternal((TType)value, byWho);
    // }

    [CompRef]
    [Auto]
    private IVarValueSettingProcessor<TType> _beforeSetProcessor;

    private bool PrefabKindMatchTagCheck()
    {
#if UNITY_EDITOR
        if (myPrefabKind == PrefabKind.NonPrefabInstance) //場景上的非prefab給過
            return true;
        var tag = GetComponent<GameStateRequireAtPrefabKind>();

        if (tag == null)
            return false; //[]: 該給過嗎？ 不該，要不然prefab會很吵
        if ((tag.prefabKind & myPrefabKind) != 0)
            return true;
#endif
        return false; //不是那個環境就不用顯示了
    }

    private bool IsCheckingPrefabKind => GetComponent<GameStateRequireAtPrefabKind>() != null;

    private void GenData()
    {
#if UNITY_EDITOR
        //get type of scriptableData field using reflection
        var type = GetType().GetField("scriptableData").FieldType;
        _bindData = type.CreateGameStateSO(this) as TScriptableData;
        this.SetDirty();
        Debug.Log("自動生成flag修正" + _bindData, _bindData);
#endif
        //FIXME: 用validator檢查，然後自動Fix?
        //[]:已經在Auto那邊用OnBeforeSerialize全部做掉了
    }

    [HideIf(nameof(HasProxySource))]
    [TabGroup("GameState")]
    [LabelText("自動生成")]
    [ShowInInspector]
    private bool IsAutoGen => GetComponent<AutoGenGameState>() != null; //TODO: IsAutoGen?
#if UNITY_EDITOR
    private bool IsAutoGenButNotYet() => IsAutoGen && _bindData == null;

    private bool IsGameStateRequiredButMissing()
        //FIXME: default不需要存檔，標記需要存檔的流程是什麼？
        =>
        PrefabKindMatchTagCheck() && _bindData == null;

    private bool IsSuggestingAutoGen() => !IsAutoGen && _bindData == null;

    private bool IsSuggestingDesignTag()
    {
        return gameObject.IsInPrefab() || myPrefabKind == PrefabKind.NonPrefabInstance;
    }
#endif

    //TODO: 可以直接弄到drawer上？

    [TabGroup("GameState")]
    [HideInInlineEditors]
    [EnableIf("IsSuggestingDesignTag")]
    //[]: 已經裝了的話要藏嗎？ 還是應該要透明
    [HideIf("@" + nameof(HasProxySource) + " || " + nameof(IsAutoGen))]
    [Button("[Prefab設計]Add AutoGen GameState")]
    private void AddTag() => this.TryGetCompOrAdd<AutoGenGameState>();


    [TabGroup("GameState")]
    //[]: 已經裝了的話要藏嗎？
    [HideIf("@" + nameof(HasProxySource) + " || " + nameof(IsCheckingPrefabKind))]
    [EnableIf("IsSuggestingDesignTag")]
    [Button("[Prefab設計]Add GameState Require Tag")]
    private void AddRequireInPrefab() => this.TryGetCompOrAdd<GameStateRequireAtPrefabKind>();

    //  MustGenScriptableDataTag mustGenTag; //提醒一定要gen flag
#if UNITY_EDITOR


    //lazy get prefabKind
    private PrefabKind _myPrefabKind;

    [ShowInInspector]
    private PrefabKind myPrefabKind => OdinPrefabUtility.GetPrefabKind(this);
    //FIXME: 這個可以cache嗎...
#endif

    // [ShowInDebugMode]
    // private bool HasLocalField => _bindData != null || HasValueSource;

    // [MCPExtractable]
    [PropertyOrder(-1)]
    [FormerlySerializedAs("localField")]
    [TabGroup("Value")]
    [InlineField]
    // [HideIf(nameof(HasLocalField))]
    [HideIf(nameof(HasProxySource))]
    public TField _localField; // = new();

    //HasValueSource 已上移到 AbstractMonoVariable（行為相同：ValueResolver.HasValueProvider(_valueSources)）

    //這個值會被蓋掉???

    // [TabGroup("Value")]
    public TField Field => BindData != null ? BindData.field : _localField;

    //給非Auto的人看的，要綁，Auto自己就會生，就結束了

    public virtual void EnterSceneStart()
    {
        RegisterValueChange();
        //EnterSceneAwake?
        Field.Init(TestMode.Production, this);
        // Debug.Log("[Variable] EnterSceneStart Init Value:" + CurrentValue, this);
    }

    // public override void AddListener<T>(UnityAction<T> action)
    // {
    //     if (action == null) return;
    //     // this.Log("[Variable] AddListener", action);
    //     if (action is UnityAction<TType> actionT)
    //         Field.AddListener(actionT, this);
    //     else
    //         Debug.LogError("AddListener Type Error", this);
    // }


    protected virtual void RegisterValueChange()
    {
        Field.AddListener(
            (value) =>
            {
                OnValueChanged();
            },
            this
        );
    }

    public override void OnValueChanged()
    {
        if (!Application.isPlaying)
            return;

        // 處理 _dataChangedListeners
        if (_dataChangedListeners != null)
            foreach (var item in _dataChangedListeners)
                item.OnVarChanged(this);

        // 觸發帶參數的 EventHandle，傳入當前值
        _valueChangedHandler?.EventHandle<TType>(CurrentValue);
    }

    [FormerlySerializedAs("scriptableData")]
    //FIXME: 這個錯了...要有特定設計tag，才是在prefab上不要gen
    // [EnableIn(PrefabKind.InstanceInScene | PrefabKind.NonPrefabInstance)] //scriptable binding, 只想要在景裡編輯
    [TabGroup("GameState")]
    [Header("存檔")]
    [GameState]
    [HideIf(nameof(HasProxySource))]
    [InlineEditor]
    [EnableIf(nameof(PrefabKindMatchTagCheck))]
#if  UNITY_EDITOR
    [InfoBox("SaveID不一致, 清掉重綁", InfoMessageType.Error, nameof(IsGameStateSaveIDNotMatch))]
    [InfoBox("GameState的類型不對", InfoMessageType.Error, nameof(IsGameStateTypeNotMatch))]
    [InfoBox("需要綁GameState!", InfoMessageType.Error, nameof(IsGameStateRequiredButMissing))]
    [InlineButton(nameof(GenData), "Auto Gen Fix", ShowIf = nameof(IsGenDataRequired))]
#endif
    // [ValidateInput("AutoGenCheck", "自動生成檢查失敗")]
    public TScriptableData _bindData;

#if UNITY_EDITOR
    private bool IsGameStateSaveIDNotMatch() //需檢查情境：複製時，造成綁到同一個gameState ref, 檢查saveID
    {
        if (!IsAutoGen)
            return false;
        var autoComp = GetComponent<AutoGenGameState>();
        if (autoComp == null || _bindData == null)
            return false;
        return autoComp.SaveID != _bindData.GetSaveID;
        // Debug.LogError("SaveID不一致", this);
    }

    // <summary> 用來檢查是否有auto gen, 但是type不對 </summary>
    private bool IsGameStateTypeNotMatch()
    {
        if (_bindData == null)
            return false;

        var autoComp = GetComponent<AutoGenGameState>();
        if (autoComp != null)
        {
            //有auto gen, 但是type不對
            if (_bindData.gameStateType != GameFlagBase.GameStateType.AutoUnique)
                return true;
        }
        else
        {
            if (_bindData.gameStateType != GameFlagBase.GameStateType.Manual)
                return true;
        }

        return false;
    }
#endif

    public virtual TScriptableData BindData => _bindData; //FIXME:

    //不同type不同類型的modifier
    [PreviewInInspector]
    [Component]
    [AutoChildren]
    protected AbstractVariableModifier<TType>[] _modifiers; //bound modifier?

    // [TabGroup("Data")]
    // [PreviewInInspector]
    public virtual TType FinalValue => CurrentValue;

    // [TabGroup("Value")]
    [ShowInDebugMode]
    public virtual TType LastValue => Field.LastValue; //FIXME: 這裡沒有過到modifier

    // [MCPExtractable]
    public TType Value
    {
        get => CurrentValue;
        // set //給reflection用的
        // // this.Log("[Variable] Set", value);
        // {
        //     if (!Application.isPlaying)
        //         EditorValue = value;
        //     else
        //         SetValueExecution(value);
        // }
    }

    public TType EditorValue
    {
        get => Field.CurrentValue;
        set
        {
            // Field.ProductionValue = value;
            // Field.DevValue = value;
            _localField.ProductionValue = value;
            _localField.DevValue = value;
            Debug.Log("Set EditorValue" + value, this);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }

    public bool IsNull => _isNull;

    [SerializeField]
    private bool _isNull = false; //預設是ProductionValue
//可以用 Vector3?

    public override string StringValue => CurrentValue.ToString();
    public override string ValueInfo => CurrentValue.ToString();
    [ShowInPlayMode]
    public virtual TType CurrentValue //FIXME: 改成Value?
    {
        get
        {
            //hmm
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return EditorValue;
#endif

            //遞迴檢測只在 Editor 做：try/finally 會讓這個 getter 無法被 inline，
            //而這條 chain 是每幀被讀好幾次的熱路徑
#if UNITY_EDITOR
            _recursionDepth++;
            if (_recursionDepth > MAX_RECURSION_DEPTH)
            {
                Debug.LogError(
                    $"[遞迴檢測] CurrentValue 遞迴深度超過 {MAX_RECURSION_DEPTH}！可能發生循環引用。Variable: {name}, ValueSource: {valueSource}, VarRef: {varRef}",
                    this
                );
                Debug.Break();
                _recursionDepth = 0;
                return default;
            }

            try
            {
                return GetCurrentValueCore();
            }
            finally
            {
                _recursionDepth--;
            }
#else
            return GetCurrentValueCore();
#endif
        }
    }

    private TType GetCurrentValueCore()
    {
        if (_isNetOverridden) //proxy 端收到的權威值，優先於本地現算
            return _netValue;
        if (valueSource != null) //用外部source getter, 這樣原本一坨都不需要了吧？
            return valueSource.Get<TType>();
        if (varRef != null)
            return varRef.Get<TType>();

        if (HasProxySource) //有proxy卻拿不到，不給
            return default;
        var tempValue = _localField.CurrentValue;

        //FIXME: 這裡就有proxy? 而且還是直接reference...
        if (BindData != null)
            tempValue = BindData.CurrentValue;

        // this.Log("[Variable] Get", tempValue);
        return tempValue;
    }

    // private MonoBehaviour lastValueSetter;

    // SetValue???

    /// <summary>
    ///     少數真的需要在非 SA 端本地預測的地方用（明確 opt-in），一般寫入請用 SetValue。
    /// </summary>
    public void SetValueLocalPredicted(TType value, Object byWho = null)
    {
        SetValueInternal(value, byWho, "LocalPredicted", false, true);
    }

    /// <param name="fromNetwork">
    ///     這是網路權威值寫入：略過 _modifiers（bound clamp 等），也不受權威 gate 限制。
    ///     只由 SetValueFromNetwork 傳 true。
    /// </param>
    /// <param name="ignoreAuthorityGate">
    ///     明確 opt-in 的本地預測寫入，見 SetValueLocalPredicted。
    /// </param>
    protected void SetValueInternal(TType value, Object byWho, string reason = null,
        bool fromNetwork = false, bool ignoreAuthorityGate = false)
    {
        //已被 NetworkedVarSync 認領的 Var，權威在 StateAuthority 端；
        //非 SA 端的本地寫入會跟每 tick 讀回的權威值打架，直接擋掉
        if (!fromNetwork && !ignoreAuthorityGate && IsLocalWriteBlockedByNetwork)
        {
            RecordNetworkBlocked(byWho);
            return;
        }

        Profiler.BeginSample("FieldVariable SetValueInternal");

        // 如果有 ParentVarEntity，代理 SetValue 到 parent entity 的 Variable
        if (varRef != null)
        {
            //代理型 Var 不會被掛上 NetworkedVarSync（同步的是被指向的實體 Var），
            //gate 與 fromNetwork 都交給對面那個 Var 自己判斷
            varRef.SetRaw(value, byWho);
            Profiler.EndSample();
            return;
        }

        var (result, tempValue) = SetValueExecution(value, byWho as MonoBehaviour, fromNetwork);
        if (result)
            RecordSetbyWhoDebug(byWho, tempValue, reason);

        Profiler.EndSample();
    }

    //FIXME: protected?
    private (bool, TType) SetValueExecution(TType value, MonoBehaviour byWho,
        bool skipModifiers = false)
    {
        // if (_beforeSetProcessor != null)
        _beforeSetProcessor?.BeforeSetValueCallback(value); //練線處理？
        // lastValueSetter = byWho;

        //CurrentValue 是多層 getter，這裡只讀一次，給 modifier / 比較 / oldValue 共用
        var currentValue = CurrentValue;
        var tempValue = value;
        //先檢查會被修改

        Profiler.BeginSample("BeforeSetValueModifyCheck", this);
        if (!skipModifiers && _modifiers != null)
            foreach (var modifier in _modifiers)
                tempValue = modifier.BeforeSetValueModifyCheck(tempValue, currentValue);
        Profiler.EndSample();
        //after?
        // Debug.Log("[Variable] Set" + value + "tempValue:" + tempValue + ", Value:" + CurrentValue, byWho);
        if (EqualityComparer<TType>.Default.Equals(tempValue, currentValue))
            return (false, tempValue); //沒有變化就不需要處理

        if (valueSource is IValueSettable<TType> settableSource)
        {
            settableSource.SetValue(tempValue, byWho, null);
            return (true, tempValue);
        }

        // Profiler.BeginSample("Field SetCurrentValue");
        var oldValue = currentValue;
        Field.SetCurrentValue(tempValue, byWho);
        _isNull = false;
        // Profiler.EndSample();

        OnValueSet(oldValue, tempValue);

        //什麼時候需要track? isTracking?
        // Profiler.BeginSample("TrackValue");
        TrackValue(tempValue, byWho);
        // Profiler.EndSample();

        return (true, tempValue);
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

    private void TrackValue(TType value, MonoBehaviour byWho)
    {
        if (!RuntimeDebugSetting.isTracking)
            return;
        var trackValue = UserDataTracker.BorrowTrackableValue;
        if (trackValue == null)
            return;
        // trackValue.SetProperty("Data", FinalData ? FinalData.name : "null");
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

    //     public void Validate(SelfValidationResult result)
    //     {
    // #if UNITY_EDITOR
    //         if (IsAutoGen)
    //         {
    //             //不在景裏，不需要
    //             if ((OdinPrefabUtility.GetPrefabKind(this) & PrefabKind.InstanceInScene) == 0) return;
    //             if (IsAutoGenButNotYet()) result.AddError("需要GameState Not Gen").WithFix(GenData);
    //         }
    //
    //         if (IsGameStateSaveIDNotMatch()) result.AddError("SaveID不一致, 清掉重綁").WithFix(GenData);
    // #endif
    //     }

#if UNITY_EDITOR
    private bool IsGenDataRequired()
    {
        if (IsAutoGen)
        {
            //不在景裏，不需要
            if ((OdinPrefabUtility.GetPrefabKind(this) & PrefabKind.InstanceInScene) == 0)
                return false;
            if (IsAutoGenButNotYet())
                return true;
        }

        return IsGameStateSaveIDNotMatch();
    }
#endif

    public override Type ValueType => typeof(TType);

    // public override object objectValue => CurrentValue;

    public string Serialize()
    {
        return GetType().Name + ":" + _localField.ProductionValue;
    }

    public void Deserialize(string data)
    {
        throw new NotImplementedException();
    }

    public override void ResetStateRestore(bool IsHardReset) //應該放下去，然後這裡override實作？
    {
        //FIXME: if not init, restore? 應該要弄個ISceneAwake?
        // _localField.Init(TestMode.Production, this);
        // Field.ResetToDefault();
        //網路覆寫值是 reset 前的殘留，不清掉的話 Getter 型 Var 在 client 端 reset 後讀到的還是舊網路值
        ClearNetworkOverride();
        Field.Init(TestMode.Production, this);
    }

    public override void ClearValue()
    {
        Field.ClearValue();
        RecordSetbyWhoDebug(this, default(TType), "ClearValue");
        _isNull = true;
    }

    public override void OnBeforePrefabSave()
    {
        base.OnBeforePrefabSave();
        if (HasParentVar) //local的沒差
            return;
        if (_varTag == null) //nested的可以不用有？
        {
            if (RuntimeDebugSetting.IsDebugMode)
                Debug.LogWarning("No VarTag: " + this, this);
        }
        else if (name != _varTag.name)
        {
            name = _varTag.name;
        }

    }
}

public interface ISettable //FIXME: 有點蠢
{
    //如果有 proxy value就return? 要用一個bool?
    void CommitValue();

    //FIXME: 用T?
    // void SetValue<T>(T value, MonoBehaviour byWho = null);
    // void SetValue(object value, MonoBehaviour byWho = null);
}

//這個有意義嗎？
public interface ISettable<in T> : ISettable
{
    void SetValue(T value, Object byWho = null, string message = "");
}
