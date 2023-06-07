using System;
using System.Collections.Generic;
using Mono.CSharp;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
// using Newtonsoft.Json;

[Serializable]
public class FlagFieldString : FlagField<string>
{
}
[Serializable]
public class FlagFieldEnum<T> : FlagField<T> where T : struct, IConvertible, IComparable
{

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

}

public class ValueChangedListener<T>
{
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
        keys.AddRange(onChangeActionDict.Keys);

        foreach (var key in keys)
        {
            if (onChangeActionDict.ContainsKey(key))
            {
                var action = onChangeActionDict[key].Item2;
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
        foreach (var key in onChangeActionDict.Keys)
        {
            var action = onChangeActionDict[key];
            // Debug.Log("[FlagField] Object Target" + action);
            // Debug.Log("[FlagField] Object Target" + action.Item1);

            //  Equals(null)

            // if (IsNullOrDestroyed(action.Item1))
            // {
            //     toRemove.Add(key);
            //     continue;
            // }
            if (action.Item1 == null)
            {
                // Debug.Log("Flag == null");
                toRemove.Add(key);
                continue;
            }
            else if (action.Item1.Equals(null))
            {
                // Debug.Log("Flag Equals(null)");
                toRemove.Add(key);
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
    public bool RemoveListenerDict(UnityAction<T> action, MonoBehaviour target)
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

public class FlagFieldModifier<T>
{
    public T OverrideValue;
    public IStatModifierOwner source;
}
[Serializable]
public class FlagFieldBool : FlagField<bool>
{
    
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
        PlayTestValue = defaultValue;
    }
    public static bool operator ==(FlagFieldBool j, bool k)
    {
        return j.CurrentValue == k;
    }
    public static bool operator !=(FlagFieldBool j, bool k)
    {
        return j.CurrentValue != k;
    }
}
public abstract class FlagFieldBase
{

}
public class FlagField<T> : FlagFieldBase
{
    private FlagFieldModifier<T> _modifier;
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

    public T PlayTestValue;


    [FormerlySerializedAs("TestValue")]
    // [JsonIgnore]
    public T DevValue;

    // [Header("Current State")]
    
    // [OnChangedCallAttribute("SetCurrentValue")]
    
    protected T _currentValue;

    // public bool isDirty = false;
    // private List<FlagFieldModifier<T>> modifiers = new();

    public void AddModifier(FlagFieldModifier<T> modifier)
    {
        // if (!modifiers.Contains(modifier)) modifiers.Add(modifier);
        _modifier = modifier;
    }

    public void RemoveModifier(FlagFieldModifier<T> modifier)
    {
        // if (modifiers.Contains(modifier)) modifiers.Remove(modifier);
        _modifier = null;
    }
    

    [GUIColor(0, 1, 0.5f, 1)]
    [ShowInPlayMode]
    public virtual T CurrentValue
    {
        get => _modifier != null ? _modifier.OverrideValue : _currentValue; //有modifier的話...
        set => SetCurrentValue(value);
            // SetCurrentValue(value);
            //有事件而且值不同
            //   Debug.Log("FlagField Set CurrentValue" + value);
    }

    protected T _lastValue;
    [ShowInPlayMode] public T LastValue => _lastValue;

    public void RevertToLastValue()
    {
        CurrentValue = LastValue;
    }
    public ValueChangedListener<T> listener =  new ();
    public ValueChangedListener<T> listenerOnce = new();
    public void AddListener(UnityAction<T> action, MonoBehaviour owner)
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
    public void AddListener(UnityAction<T> action, ScriptableObject owner)
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
        listener.AddListenerDict(action, owner as object);
    }
    public void AddListenerOnce(UnityAction<T> action, MonoBehaviour owner)
    {
        if (listenerOnce == null)
        {
            listenerOnce = new ValueChangedListener<T>();
        }
        listenerOnce.AddListenerDict(action, owner);
    }
    public void AddListenerOnce(UnityAction<T> action, ScriptableObject owner)
    {
        if (listenerOnce == null)
        {
            listenerOnce = new ValueChangedListener<T>();
        }
        listenerOnce.AddListenerDict(action, owner as object);
    }
    public void RemoveListener(UnityAction<T> action, MonoBehaviour owner)
    {
        var result = false;
        if (listener != null)
            result |= listener.RemoveListenerDict(action, owner);
        if (listenerOnce != null)
            result |= listenerOnce.RemoveListenerDict(action, owner);
        if (result == false)
            Debug.LogWarning("Remove Not Exist Listener");
        else
            Debug.Log("Remove Listener" + action.Method);

    }

    //NOTE: public是為了，propertyDrawer
    protected void SetCurrentValue(T value)
    {
        // Debug.Log("FlagField Before SetCurrentValue" + value);
        if (value.Equals(_currentValue))
            return;
        _lastValue = _currentValue;
        _currentValue = value;
        // Debug.Log("FlagField SetCurrentValue OnChanged" + value);
        listener?.OnChange(value, false);
        listenerOnce?.OnChange(value, true);

        // isDirty = true;

    }



    public void Init(TestMode mode)
    {
        // isDirty = false;
        _currentValue = mode switch
        {
            TestMode.EditorDevelopment => DevValue,
            TestMode.Production => ProductionValue,
            // TestMode.BetaTest => PlayTestValue,
            _ => _currentValue
        };
        lastMode = mode;

        
    }

    private TestMode lastMode = TestMode.EditorDevelopment;
    //FIXME: local field...不會有一般的init途徑，怎麼辦？
    


    public void Reset()
    {
        // listener = null;
        // listenerOnce = null;
        if (lastMode != TestMode.Undefined)
        {
            Init(lastMode);
        }
        else
            CurrentValue = ProductionValue;

        // Debug.Break();
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