using System;
using System.Collections.Generic;

using Sirenix.Utilities;

using UnityEngine;
using UnityEngine.Events;

public class ValueChangedListener<TTarget, TParam, TValue>
{
    private Dictionary<object, Tuple<object, object, UnityAction<object, object, TValue>>> onChangeActionDict;

    public void AddListenerDict(object target, object param, UnityAction<object, object, TValue> action)
    {
        var tuple = Tuple.Create(target, param, action);


        if (onChangeActionDict == null)
            onChangeActionDict = new Dictionary<object, Tuple<object, object, UnityAction<object, object, TValue>>>();
        if (onChangeActionDict.ContainsKey(target))
        {
            // Debug.Log("Already AddListener" + key);
            return;
        }

        onChangeActionDict[target] = tuple;
    }

    private List<object> keys = new List<object>();

    public void OnValueChange(TValue value)
    {
        if (onChangeActionDict == null)
        {
            return;
        }

        //避免Dictionary變動 先把key 都拿出來
        keys.Clear();

        var iterator = onChangeActionDict.GFIterator();
        while (iterator.MoveNext())
        {
            keys.Add(iterator.Current.Key);
        }

        foreach (var key in keys)
        {
            if (onChangeActionDict.TryGetValue(key, out var value1))
            {
                var action = value1.Item3;
                Debug.Log("FlagField Invoke:" + action);
                action?.Invoke(value1.Item1, value1.Item2, value);
            }
            else
            {
                Debug.LogError("WTF?");
            }
        }
    }
}