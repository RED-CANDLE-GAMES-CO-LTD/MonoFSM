using System;
using System.Collections.Generic;
// using Mono.CSharp;
using RCGMaker.Core.Attributes;
using RCGSetting;
// using RCGSetting;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

// using Newtonsoft.Json;

[Serializable]
public class FlagFieldString : FlagField<string>
{
    protected override bool IsCurrentValueEquals(string value)
    {
        return _currentValue == value;
    }
}
[Serializable]
public class FlagFieldEnum<T> : FlagField<T> where T : struct, IConvertible, IComparable
{
    protected override bool IsCurrentValueEquals(T value)
    {
        return _currentValue.Equals(value);
    }
}

[Serializable]
public class FlagFieldInt : FlagField<int>
{
    // public static bool operator ==(FlagFieldInt j, int k)
    // {
    //     return j.CurrentValue == k;
    // }
    // public static bool operator !=(FlagFieldInt j, int k)
    // {
    //     return j.CurrentValue != k;
    // }

    protected override bool IsCurrentValueEquals(int value)
    {
        return _currentValue == value;
    }
}

[Serializable]
public class FlagFieldLong : FlagField<long>
{
    protected override bool IsCurrentValueEquals(long value)
    {
        return _currentValue == value;
    }
}


[Serializable]
public class FlagFieldFloat : FlagField<float>
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool operator ==(FlagFieldFloat j, float k)
    {
        return j.CurrentValue == k;
    }
    public static bool operator !=(FlagFieldFloat j, float k)
    {
        return j.CurrentValue != k;
    }

    protected override bool IsCurrentValueEquals(float value)
    {
        return _currentValue == value;
    }
}



public class ValueChangedListener<T>
{
    public void Clear()
    {
        onChangeActionDict?.Clear();
        keys?.Clear();
        toRemove?.Clear();
    }
    private Dictionary<int, System.Tuple<object, UnityAction<T>>> onChangeActionDict;

    private List<int> keys = new List<int>();
    public void OnChange(T value, bool clearAll)
    {
        if (onChangeActionDict == null)
        {
            return;
        }
        CleanNullListener();

        //避免Dictionary變動 先把key 都拿出來
        keys.Clear();

        var iterator = onChangeActionDict.GFIterator();
        while (iterator.MoveNext())
        {
            keys.Add(iterator.Current.Key);
        }

        // keys.AddRange(onChangeActionDict.Keys);

        foreach (var key in keys)
        {
            if (onChangeActionDict.TryGetValue(key, out var value1))
            {
                var action = value1.Item2;
                //  Debug.Log("FlagField Invoke" + action);
                action.Invoke(value);
            }
            else
            {
                Debug.LogError("WTF?");
            }

        }
        if (clearAll)
            onChangeActionDict.Clear();
    }
    public void AddListenerDict(UnityAction<T> action, object target)
    {
        var tuple = Tuple.Create(target, action);
        var key = tuple.GetHashCode();

        if (onChangeActionDict == null)
            onChangeActionDict = new Dictionary<int, System.Tuple<object, UnityAction<T>>>();
        if (onChangeActionDict.ContainsKey(key))
        {
            // Debug.Log("Already AddListener" + key);
            return;
        }
        CleanNullListener();
        onChangeActionDict[key] = tuple;
    }
    List<int> toRemove; //這個new list呢？
    void CleanNullListener()
    {
        if (toRemove == null)
        {
            toRemove = new List<int>();
        }
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
        {
            //Debug.Log("Remove" + toRemove[i]);
            onChangeActionDict.Remove(toRemove[i]);
        }
    }
    // public static bool IsNullOrDestroyed(this System.Object obj)
    // {
    //     if (object.ReferenceEquals(obj, null)) return true;
    //     if (obj is UnityEngine.Object) return (obj as UnityEngine.Object) == null;
    //     return false;
    // }
    public bool RemoveListenerDict(UnityAction<T> action, Object target)
    {
        if (onChangeActionDict == null)
        {
            return true;
        }
        // var key = action.GetHashCode();
        var key = Tuple.Create(target, action).GetHashCode();
        if (!onChangeActionDict.ContainsKey(key))
        {
            return false;
        }
        onChangeActionDict.Remove(key);
        return true;
    }
}

// [Serializable]
public class FlagFieldModifier<T>
{
    public T OverrideValue;
    public IStatModifierOwner source;
    [PreviewInInspector] public Object sourceObj => source as Object;
}
[Serializable]
public class FlagFieldBool : FlagField<bool>
{
    public bool IsJustBecameTrue => _lastValue == false && _currentValue == true;
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FlagFieldBool)obj);
    }
    
    public FlagFieldBool() : base()
    {

    }
    public FlagFieldBool(bool defaultValue)
    {
        ProductionValue = defaultValue;
        DevValue = defaultValue;
        // PlayTestValue = defaultValue;
    }
    public static bool operator ==(FlagFieldBool j, bool k)
    {
        return j.CurrentValue == k;
    }
    public static bool operator !=(FlagFieldBool j, bool k)
    {
        return j.CurrentValue != k;
    }

    protected override bool IsCurrentValueEquals(bool value)
    {
        return _currentValue == value;
    }
}

[Serializable]
public abstract class FlagFieldBase
{
    public abstract void ResetToDefault();
}

[Serializable]
public class
    FlagField<T> : FlagFieldBase // where T : IComparable, IComparable<bool>, IConvertible, IEquatable<bool>
{
    [ShowInInspector] [ReadOnly]
    // private FlagFieldModifier<T> _modifier;
    private List<FlagFieldModifier<T>> _modifiers = new();
    public FlagField()
    {
        ProductionValue = default(T);
    }
    public FlagField(T defaultValue)
    {
        ProductionValue = defaultValue;
    }
    // [Header("Game Setting")]
    // [JsonIgnore]
    // [SerializeField]

    [FormerlySerializedAs("DefaultValue")]
    public T ProductionValue;

    // public T PlayTestValue;


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
    
    [GUIColor(0, 1, 0.5f, 1)]
    [ShowInInspector]
    public virtual T CurrentValue
    {
        get => _modifiers.Count > 0 ? _modifiers[^1].OverrideValue : _currentValue; //有modifier的話...
        set => SetCurrentValue(value); //從inspector來的就是null?不是很好可以塞一個dummy給他嗎
            // SetCurrentValue(value);
            //有事件而且值不同
            //   Debug.Log("FlagField Set CurrentValue" + value);
    }

    
    public T SaveValue => _currentValue;

    protected T _lastValue;
    [ShowInPlayMode] public T LastValue => _lastValue;

    public void RevertToLastValue()
    {
        CurrentValue = LastValue;
    }

    private ValueChangedListener<T> listener = new(); //好像可以把監聽對象丟出來看？
    private ValueChangedListener<T> listenerOnce = new();
    // private ValueChangedListener<object, object, T> listenerDict;

    // public void AddListener<TTarget, TParam>(TTarget target, TParam param, UnityAction<TTarget, TParam, T> callback)
    //     where TTarget : Object
    // {
    //     if (listenerDict == null)
    //         listenerDict = new ValueChangedListener<object, object, T>();
    //     listenerDict.AddListenerDict(target, param, callback as UnityAction<object, object, T>);
    // }

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


        if (listener == null)
        {
            listener = new ValueChangedListener<T>();
        }

        // Debug.Log("FlagField Add Listener",owner);
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

    public void AddListenerOnce(UnityAction<T> action, Object owner)
    {
        if (listenerOnce == null)
        {
            listenerOnce = new ValueChangedListener<T>();
        }
        listenerOnce.AddListenerDict(action, owner as object);
    }

    public void RemoveListener(UnityAction<T> action, Object owner)
    {
        var result = false;
        if (listener != null)
            result |= listener.RemoveListenerDict(action, owner);
        if (listenerOnce != null)
            result |= listenerOnce.RemoveListenerDict(action, owner);
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

    [ShowInInspector] public Object LastByWho => _lastByWho;
    //NOTE: public是為了，propertyDrawer
    public void SetCurrentValue(T value, Object byWho = null)
    {
#if UNITY_EDITOR
        if (DebugSetting.IsDebugMode && _isShowDebugLog)
            Debug.Log("[FlagField] Before Set lastValue:" + _currentValue + "set with:" + value, owner);
#endif

        if (IsCurrentValueEquals(value))
            return;
        _lastByWho = byWho;
        _lastValue = _currentValue;
        _currentValue = value;

        // if (DebugSetting.IsDebugMode && _isShowDebugLog)
        //     Debug.Log("[FlagField] After CurrentValue" + value);
        OnChangeInvoke(value);
    }
    //need UI update...
    // public bool InvokeSetEventValueNotChanged
    

    private void OnChangeInvoke(T value)
    {
        listener.OnChange(value, false);
        listenerOnce.OnChange(value, true);
        // listenerDict?.OnValueChange(value);
    }


    public void Init(TestMode mode, Object _owner)
    {
        owner = _owner;
        _modifiers.Clear();

        _currentValue = mode switch
        {
            TestMode.EditorDevelopment => DevValue,
            TestMode.Build => ProductionValue,
            _ => _currentValue
        };
        
        lastMode = mode;
        listener.Clear();
        listenerOnce.Clear();
    }

    private Object owner;

    private void Log(object msg)
    {
        // if (_isShowDebugLog)
        Debug.Log(msg + " " + owner.GetInstanceID(), owner);
    }

    private TestMode lastMode = TestMode.EditorDevelopment;
    //FIXME: local field...不會有一般的init途徑，怎麼辦？


    public override void ResetToDefault()
    {
        //[]: 要先init才能ResetToDefault
        if (owner == null)
            Debug.LogError("PLZ FIX ME, Assign Owner for function block!!" + owner);
        // listener = null;
        // listenerOnce = null;

        //[]: 有singleton就不用lastMode了吧
        if (lastMode != TestMode.Undefined)
        {
            // Debug.Log("FlagField: ResetToDefault" + lastMode);
            Init(lastMode, owner);
            // Debug.Log("FlagField: CurrentValue" + CurrentValue);
        }
        else
            CurrentValue = ProductionValue;
        
    }

    //Field init的時候，就會清了才對
    // public void Clear()
    // {
    //     //FIXME: 是不是應該要清掉listener?
    // }

}

// public class OnChangedCallAttribute : PropertyAttribute
// {
//     public string methodName;
//     public OnChangedCallAttribute(string methodNameNoArguments)
//     {
//         methodName = methodNameNoArguments;
//     }
// }