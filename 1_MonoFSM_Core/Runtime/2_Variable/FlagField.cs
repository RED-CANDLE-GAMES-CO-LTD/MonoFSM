using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

using Sirenix.OdinInspector;
using Sirenix.Utilities;
using MonoFSM.Core.Attributes;

[Serializable]
public class FlagFieldString : FlagField<string>
{
    protected override bool IsCurrentValueEquals(string value) 
        => _currentValue == value;
}

[Serializable]
public class FlagFieldEnum<T> : FlagField<T> where T : struct, IConvertible, IComparable
{
    protected override bool IsCurrentValueEquals(T value) 
        => _currentValue.Equals(value);
}

[Serializable]
public class FlagFieldInt : FlagField<int>
{
    protected override bool IsCurrentValueEquals(int value) 
        => _currentValue == value;
}

[Serializable]
public class FlagFieldLong : FlagField<long>
{
    protected override bool IsCurrentValueEquals(long value) 
        => _currentValue == value;
}


[Serializable]
public class FlagFieldFloat : FlagField<float>
{
    public static bool operator ==(FlagFieldFloat j, float k) 
        => j.CurrentValue == k;

    public static bool operator !=(FlagFieldFloat j, float k) 
        => j.CurrentValue != k;

    protected override bool IsCurrentValueEquals(float value) 
        => _currentValue == value;
}


[Serializable]
public class ValueChangedListener<T>
{
    public void Clear()
    {
        onChangeActionDict?.Clear();
        tempKeys?.Clear();
        toRemove?.Clear();
    }

    //FIXME: 也可以直接塞介面？就不是彈性的隨便塞Action監聽
    private Dictionary<int, Tuple<Object, UnityAction<T>>> onChangeActionDict;

    [PreviewInInspector] private List<Object> ownersInDict 
        => onChangeActionDict?.Values.Select(x => x.Item1).ToList();

    [PreviewInInspector] private List<int> tempKeys = new();

    public void OnChange(T value, bool clearAll)
    {
        if (onChangeActionDict == null) return;

        CleanNullListener();

        //避免Dictionary變動 先把key 都拿出來
        tempKeys.Clear();

        var iterator = onChangeActionDict.GFIterator();
        while (iterator.MoveNext()) tempKeys.Add(iterator.Current.Key);

        foreach (var key in tempKeys) //這個keys怎麼可能變動？在別的地方add listener?
            if (onChangeActionDict.TryGetValue(key, out var value1))
            {
                var action = value1.Item2;
                //FIXME: 這個invoke可能會造成這個field又change?很糟糕要怎麼避免
                action.Invoke(value);
            }
            else
            {
                Debug.LogError("WTF?");
            }

        if (clearAll)
            onChangeActionDict.Clear();
    }

    //重複註冊就留著
    public void AddListenerDict(UnityAction<T> action, Object target)
    {
        var tuple = Tuple.Create(target, action);
        var key = tuple.GetHashCode();

        if (onChangeActionDict == null)
            onChangeActionDict = new Dictionary<int, Tuple<Object, UnityAction<T>>>();
        if (onChangeActionDict.ContainsKey(key))
            // Debug.Log("Already AddListener" + key);
            return;

        CleanNullListener();
        onChangeActionDict[key] = tuple;
    }

    private List<int> toRemove; //這個new list呢？

    private void CleanNullListener()
    {
        if (toRemove == null)
            toRemove = new List<int>();
        else
            toRemove.Clear();

        var iterator = onChangeActionDict.GFIterator();
        while (iterator.MoveNext())
        {
            var action = iterator.Current.Value;
            if (action.Item1 == null)
            {
                toRemove.Add(iterator.Current.Key);
                continue;
            }
            else if (action.Item1.Equals(null))
            {
                toRemove.Add(iterator.Current.Key);
                continue;
            }
        }


        for (var i = 0; i < toRemove.Count; i++)
            //Debug.Log("Remove" + toRemove[i]);
            onChangeActionDict.Remove(toRemove[i]);
    }

    // public static bool IsNullOrDestroyed(this System.Object obj)
    // {
    //     if (object.ReferenceEquals(obj, null)) return true;
    //     if (obj is UnityEngine.Object) return (obj as UnityEngine.Object) == null;
    //     return false;
    // }
    public bool RemoveListenerDict(UnityAction<T> action, Object target)
    {
        if (onChangeActionDict == null) return true;

        // var key = action.GetHashCode();
        var key = Tuple.Create(target, action).GetHashCode();
        if (!onChangeActionDict.ContainsKey(key)) return false;

        onChangeActionDict.Remove(key);
        return true;
    }
}

// [Serializable]
public class FlagFieldModifier<T>
{
    public T OverrideValue; //FIXME: 這個是用來override的值嗎？ 啥？
    public IStatModifierOwner source;
    [PreviewInInspector] public Object sourceObj => source as Object;
}

[Serializable]
public class FlagFieldBool : FlagField<bool>
{
    public bool IsJustBecameTrue => _lastValue == false && _currentValue == true;

    public bool Value
    {
        get => CurrentValue;

        set => SetCurrentValue(value);
    }

    // public override bool Equals(object obj)
    // {
    //     if (ReferenceEquals(null, obj)) return false;
    //     if (ReferenceEquals(this, obj)) return true;
    //     if (obj.GetType() != GetType()) return false;
    //     return Equals((FlagFieldBool)obj);
    // }

    public FlagFieldBool() : base()
    {
    }

    public FlagFieldBool(bool defaultValue)
    {
        ProductionValue = defaultValue;
        DevValue = defaultValue;
        // PlayTestValue = defaultValue;
    }

    // public static bool operator ==(FlagFieldBool j, bool k) 
    //     => j.CurrentValue == k;
    //
    // public static bool operator !=(FlagFieldBool j, bool k) 
    //     => j.CurrentValue != k;
    //
    // protected override bool IsCurrentValueEquals(bool value) 
    //     => _currentValue == value;
}

[Serializable]
public abstract class FlagFieldBase
{
    public abstract void ResetToDefault();
}

public interface IVariableField
{
    public void AddListener(UnityAction action, Object owner);
    public void RemoveListener(UnityAction action, Object owner);
}

[Serializable]
public class
    FlagField<T> : FlagFieldBase,
    IVariableField // where T : IComparable, IComparable<bool>, IConvertible, IEquatable<bool>
{
    [ShowInInspector] [ReadOnly]
    // private FlagFieldModifier<T> _modifier;
    private List<FlagFieldModifier<T>> _modifiers = new(); //FIXME: 這啥？

    public FlagField()
    {
        ProductionValue = default;
    }

    public FlagField(T defaultValue)
    {
        ProductionValue = defaultValue;
    }
    // [Header("Game Setting")]
    // [JsonIgnore]
    // [SerializeField]


    //FIXME: 分Production和Dev好像怪怪的...九日是為了打勾某些能力，這個可以拿掉了？或是應該用別種方式側(環境)
    //FIXME: Config, Stat不需要DevValue

    //FIXME: Nested好像很不好assign...
    // [MCPExtractable]
    // public T DefaultValue => ProductionValue;

    //editor time value changed?


    [GUIColor(0.3f, 0.7f, 0.7f, 1f)]
    [FormerlySerializedAs("DefaultValue")] public T ProductionValue;

    // public T PlayTestValue;
    [HideInInspector]
    [FormerlySerializedAs("TestValue")]
    // [JsonIgnore]
    public T DevValue;

    // [Header("Current State")]

    // [OnChangedCallAttribute("SetCurrentValue")]


    // public bool isDirty = false;
    // private List<FlagFieldModifier<T>> modifiers = new();


    //暫時變更值，可以看出來是誰變更的
    public void AddModifier(FlagFieldModifier<T> modifier)
    {
        //先清在加
        //投票機制是 只取第一個人的意見...，一人只有一票

        //FIXME: gc...
        _modifiers.RemoveAll(x => x.source == modifier.source);
        _modifiers.Add(modifier);
        //理論上加了modifier就要重新計算一次，
        OnChangeInvoke(CurrentValue);
    }

    public void RemoveModifier(IStatModifierOwner modifierOwner)
    {
        _modifiers.RemoveAll(x => x.source == modifierOwner);
        // if (_modifiers.Contains(modifier)) _modifiers.Remove(modifier);
        // _modifier = null;
    }

    // private T OverrideValue => modifiers.Count > 0 ? modifiers[0].OverrideValue : default;


    [PreviewInInspector] protected T _currentValue; //真正拿來存的值

    [PropertyOrder(-1)]
    [GUIColor(0, 1, 0.5f, 1)]
    [ShowInPlayMode]
    public virtual T CurrentValue
    {
        get
        {
            if (Application.isPlaying == false)
                return ProductionValue;
            //強迫蓋值？
            return _modifiers.Count > 0 ? _modifiers[^1].OverrideValue : _currentValue;
        }
        //有modifier的話...
        set => SetCurrentValue(value); //從inspector來的就是null?不是很好可以塞一個dummy給他嗎
        // SetCurrentValue(value);
        //有事件而且值不同
        //   Debug.Log("FlagField Set CurrentValue" + value);
    }


    public T SaveValue => _currentValue;

    protected T _lastValue;
    [ShowInPlayMode] public T LastValue => _lastValue;

    public void RevertToLastValue() //FIXME: 什麼時候需要revert?
    {
        CurrentValue = LastValue;
    }

    public (T lastValue, T currentValue) CommitValue() //state update之後，要commit
    {
        if (owner is MonoBehaviour mono)
            mono.Log("FlagField CommitValue: ", CurrentValue);
        // owner.Log("FlagField CommitValue: ", CurrentValue);
        var old = _lastValue;
        _lastValue = CurrentValue;
        return (old, _lastValue);
    }

    [ShowInDebugMode] private ValueChangedListener<T> listener; //好像可以把監聽對象丟出來看？
    // [ShowInDebugMode] private ValueChangedListener<T> listenerOnce = new();
    [ShowInDebugMode] private UnityAction _onChangeAction;

    // public void AddListener<TTarget, TParam>(TTarget target, TParam param, UnityAction<TTarget, TParam, T> callback)
    //     where TTarget : Object
    // {
    //     if (listenerDict == null)
    //         listenerDict = new ValueChangedListener<object, object, T>();
    //     listenerDict.AddListenerDict(target, param, callback as UnityAction<object, object, T>);
    // }

    /// <summary>
    /// <seealso cref="GenericMonoVariable.EnterSceneStart"/> variable會做監聽
    /// </summary>
    /// <param name="action"></param>
    /// <param name="owner"></param>
    public void AddListener(UnityAction action, Object owner)
    {
        _onChangeAction += action;
    }

    public void RemoveListener(UnityAction action, Object owner)
    {
        if (_onChangeAction == null) return;
        _onChangeAction -= action;
    }

    /// fixme: 有可能做non gc 版本嗎？ 上面的看起來失敗了..
    /// <summary>
    /// 
    /// </summary>
    /// <param name="action"></param>
    /// <param name="owner"></param>
    public void AddListener(UnityAction<T> action, Object owner)
    {
        if (owner == null)
        {
            // var mono = action.Target as MonoBehaviour;
            // if (mono == null)
            // {
            Debug.LogError("PLZ FIX ME, Assign Owner for function block!!" + action.Target);
            return;
            // }
            // owner = mono;
        }

        if (listener == null) listener = new ValueChangedListener<T>();

        if (owner is Component comp) comp.Log("FlagField Add Listener", comp);

        listener.AddListenerDict(action, owner);
    }
    // public void AddListener(UnityAction<T> action, ScriptableObject owner)
    // {
    //     if (owner == null)
    //     {
    //         // var mono = action.Target as MonoBehaviour;
    //         // if (mono == null)
    //         // {
    //         Debug.LogError("PLZ FIX ME, Assign Owner for function block!!" + action.Target);
    //         return;
    //         // }
    //         // owner = mono;
    //     }
    //
    //
    //     if (listener == null)
    //     {
    //         listener = new ValueChangedListener<T>();
    //     }
    //     listener.AddListenerDict(action, owner as object);
    // }

    //once是不是不太好？

    // public void AddListenerOnce(UnityAction<T> action, Object owner)
    // {
    //     // if (listenerOnce == null) listenerOnce = new ValueChangedListener<T>();
    //     //
    //     // listenerOnce.AddListenerDict(action, owner);
    // }

    public void RemoveListener(UnityAction<T> action, Object owner)
    {
        var result = false;
        if (listener != null)
            result |= listener.RemoveListenerDict(action, owner);
        // if (listenerOnce != null)
        //     result |= listenerOnce.RemoveListenerDict(action, owner);
        if (result == false)
            Debug.LogWarning("Remove Not Exist Listener");
        // else
        //     Debug.Log("Remove Listener" + action.Method);
    }


    //[]: debug mode才顯示？ conditional inspector property

    // [ShowInPlayMode(DebugModeOnly = true)] 

    // [ShowIf("@DebugSetting.IsDebugMode")] [ShowInInspector]

    //會被清掉...
    [ShowInDebugMode] public bool _isShowDebugLog = false;

    protected virtual bool IsCurrentValueEquals(T value)
    {
        return _currentValue.Equals(value);
    }


    private Object _lastByWho;

    [ShowInDebugMode] public Object LastByWho => _lastByWho;

    //NOTE: public是為了，propertyDrawer
    public void SetCurrentValue(T value, Object byWho = null) //FIXME: 可能會memory leak
    {
// #if UNITY_EDITOR
//         if (DebugSetting.IsDebugMode && _isShowDebugLog)
//             Debug.Log("[FlagField] Before Set lastValue:" + _currentValue + "set with:" + value, owner);
// #endif

        Profiler.BeginSample("IsCurrentValueEquals");
        if (IsCurrentValueEquals(value))
        {
            Profiler.EndSample();
            return;
        }

        Profiler.EndSample();
#if UNITY_EDITOR
        //想要看誰改的，build不要看會memory leak
        _lastByWho = byWho;
#endif
        _lastValue = _currentValue;
        _currentValue = value;
        // Log("SetCurrentValue" + value);
        // if (DebugSetting.IsDebugMode && _isShowDebugLog)
        //     Debug.Log("[FlagField] After CurrentValue" + value);
        OnChangeInvoke(value);
    }
    //need UI update...
    // public bool InvokeSetEventValueNotChanged


    private void OnChangeInvoke(T value)
    {
        listener?.OnChange(value, false);
        // listenerOnce.OnChange(value, true);
        _onChangeAction?.Invoke();
        // listenerDict?.OnValueChange(value);
    }


    public void Init(TestMode mode, Object _owner) //這已經是reset了..
    {
        owner = _owner;
        _modifiers.Clear();

        _currentValue = mode switch
        {
            TestMode.EditorDevelopment => DevValue,
            TestMode.Production => ProductionValue,
            _ => _currentValue
        };
        _lastValue = _currentValue;
        lastMode = mode;
        //ClearListener();
        // Debug.Log("Listener Clear", owner);
        //FIXME: 綁定清掉，這樣listener也要重綁耶
        // listenerOnce.Clear();
        
        if (_owner is Component comp)
            comp.Log("FlagField Init", comp);
        
        ResetToDefault();
    }

    // void ClearListener()
    // {
    //     listener?.Clear(); //綁定不清會怎麼樣嗎？
    //     _onChangeAction = null;
    // }
    
    //TODO: 換scene清？也不對，有些不清

    [ShowInDebugMode]
    private Object owner;

    private void Log(object msg)
    {
        if (_isShowDebugLog)
        {
            if (owner)
                Debug.Log(msg + " " + owner.GetInstanceID(), owner);
            else
                Debug.Log(msg);
        }
    }

    private TestMode lastMode = TestMode.EditorDevelopment;
    //FIXME: local field...不會有一般的init途徑，怎麼辦？


    public override void ResetToDefault()
    {
        // Debug.Log("FlagField: ResetToDefault" + owner, owner);
        //[]: 要先init才能ResetToDefault
        if (owner == null)
            Debug.LogError("PLZ FIX ME, Assign Owner for function block!!" + owner);


        //[]: 有singleton就不用lastMode了吧
        //FIXME: 這個整個都住解掉了？
        // if (lastMode != TestMode.Undefined)
        // {
        //     // Debug.Log("FlagField: ResetToDefault" + lastMode);
        //     Init(lastMode, owner);
        //     
        //     // Debug.Log("FlagField: CurrentValue" + CurrentValue);
        // }
        // else
        CurrentValue = ProductionValue;
        //沒有register耶？
    }


}

// public class OnChangedCallAttribute : PropertyAttribute
// {
//     public string methodName;
//     public OnChangedCallAttribute(string methodNameNoArguments)
//     {
//         methodName = methodNameNoArguments;
//     }
// }