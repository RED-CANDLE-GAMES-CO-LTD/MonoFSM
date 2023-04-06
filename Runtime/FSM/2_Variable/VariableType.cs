using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class VariableType<TScriptableData, TField, TType> : AbstractVariable, IResetter
    where TScriptableData : AbstractScriptableData<TField, TType> where TField : FlagField<TType>, new()
{
    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        if (!EditorUtility.IsPersistent(this))
            //檢查有沒有綁定data
            if (MustGenButNotYet())
                Debug.LogError("我需要生flag data", this);
        //TODO: 自動生？
        //好像也不用傳了？
        // GenData();
#endif
    }

#if UNITY_EDITOR
    [Button("Test Auto Gen")]
    private void GenData()
    {
        // scriptableData = FlagGenerator.GenerateFlagForVariable(this, scriptableData);
        Debug.Log("自動生成flag???" + scriptableData, scriptableData);
    }
#endif
    private bool MustGenButNotYet()
    {
        if (GetComponent<MustGenScriptableDataTag>() != null && scriptableData == null)
            return true;
        return false;
    }


    [HideInInlineEditors]
    [Button("[Prefab設計]必須存擋")]
    private void AddTag()
    {
        this.TryGetCompOrAdd<MustGenScriptableDataTag>();
    }

    //  MustGenScriptableDataTag mustGenTag; //提醒一定要gen flag
    [InfoBox("需要生Flag!", InfoMessageType.Error, "MustGenButNotYet")]
    [HideIf("VariableSource")]
    [Header("存擋")]

    // [FormerlySerializedAs("boolFlag")]
    [GameFlag]
    public TScriptableData scriptableData; //FIXME:

    [ShowInInspector] [InlineEditor] public virtual TScriptableData ScriptableData => scriptableData; //FIXME:

    // [HideInInspector]
    // public UnityEvent ValueChangedEvent;
    [HideIf("VariableSource")] [HideIf("scriptableData")] [SerializeField]
    protected TField localField = new();

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

            if (ScriptableData == null)
                // if (localField == null)
                //     localField = default(TField);
                localField.CurrentValue = tempValue;
            else
                ScriptableData.CurrentValue = tempValue;
        }
    }

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
        localField.Reset();
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
    public AbstractVariable VariableSource; //用別人的值

    [ReadOnly] public List<AbstractVariableConsumer> consumers; //有誰有用我，binder綁一下
}