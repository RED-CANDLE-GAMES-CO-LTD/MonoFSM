using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;
using UnityEngine.Events;

using MonoFSM.Core.Attributes;

[Serializable]
public class CharacterStat //這個改名會爛掉嗎?
{
    public float BaseValue;
    protected bool isDirty = true;
    protected float lastBaseValue;
    protected float _value;


    [ShowInPlayMode]
    public virtual float Value
    {
        get
        {
            if (isDirty || lastBaseValue != BaseValue)
            {
                //條件一變，值就變？dirty也是一路問，問每個statmodifier
                CalValues();
                listener?.OnChange(_value,false);
            }

            return _value;
        }
    }

    private void CalValues()
    {
        lastBaseValue = BaseValue;
        _value = CalculateFinalValue();
        _permanentValue = CalculateFinalValueWithoutTemporary();
        isDirty = false;
    }

    private float _permanentValue;

    [ShowInPlayMode]
    public float PermanentValue
    {
        //calculate value without temporary modifier
        get
        {
            if (isDirty || lastBaseValue != BaseValue) CalValues();
            return _permanentValue;
        }
    }

    ValueChangedListener<float> listener;

    [PreviewInInspector] [NonSerialized]
    public List<StatModifier> statModifiers;
    //protected readonly
    public readonly ReadOnlyCollection<StatModifier> StatModifiers;

    public CharacterStat()
    {
        statModifiers = new List<StatModifier>();
        StatModifiers = statModifiers.AsReadOnly();
    }

    public CharacterStat(float baseValue) : this()
    {
        BaseValue = baseValue;
    }
    
    public void AddListener(UnityAction<float> action, MonoBehaviour owner)
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
            listener = new ValueChangedListener<float>();
        }
        listener.AddListenerDict(action, owner);
    }

    //remove
    public void RemoveListener(UnityAction<float> action, MonoBehaviour owner)
    {
        if (listener == null)
        {
            Debug.LogError("No Listener to Remove");
            return;
        }

        listener.RemoveListenerDict(action, owner);
    }
    
    public virtual void AddModifier(StatModifier mod)
    {
        // Debug.Log("Add Stat modifier" + this);
        if (!statModifiers.Contains(mod))
        {
            isDirty = true;
            statModifiers.Add(mod);
            var value = Value; //modifier改變時，更新一下值
            // Debug.Log("Character Stat Add Modifier" + mod.Value + mod.Type + ",result:" + value);
        }
        else
        {
            // statModifiers.Remove(mod);
            isDirty = true;
            var value = Value; //modifier改變時，更新一下值
            Debug.Log("Character Stat Already Has Modifier" + mod.Value + mod._statModType);
        }

    }

    public virtual bool RemoveModifier(StatModifier mod)
    {
        if (statModifiers.Remove(mod))
        {
            isDirty = true;
            var value = Value; //modifier改變，更新一下值
            return true;
        }
        return false;
    }
    public void Clear()
    {
        statModifiers.Clear();
        _value = BaseValue;
        isDirty = true;
        listener = null;
    }

    public virtual bool RemoveAllModifiersFromSource(IStatModifierOwner source)
    {
        //check remove from back to front of statModifiers
        for (var i = statModifiers.Count - 1; i >= 0; i--)
        {
            if (statModifiers[i].Source == (ScriptableObject)source)
            {
                statModifiers.RemoveAt(i);
                isDirty = true;
            }
        }

        return isDirty;
    }

    private Comparison<StatModifier> _modifierOrder = (a, b) => a.Order < b.Order ? -1 : a.Order > b.Order ? 1 : 0;
    // protected virtual int CompareModifierOrder(StatModifier a, StatModifier b)
    // {
    //     if (a.Order < b.Order)
    //         return -1;
    //     else if (a.Order > b.Order)
    //         return 1;
    //     return 0; //if (a.Order == b.Order)
    // }

    // Calculate FinalValue For PermanentModifiers Only


    private float CalValueAfterModifier(List<StatModifier> statModifiers)
    {
        var finalValue = BaseValue;
        float sumPercentAdd = 0;
        for (var i = 0; i < statModifiers.Count; i++)
        {
            var mod = statModifiers[i];

            if (mod._statModType == StatModType.Flat)
            {
                finalValue += mod.Value;
            }
            else if (mod._statModType == StatModType.PercentAdd)
            {
                sumPercentAdd += mod.Value;

                if (i + 1 >= statModifiers.Count || statModifiers[i + 1]._statModType != StatModType.PercentAdd)
                {
                    finalValue *= 1 + sumPercentAdd;
                    sumPercentAdd = 0;
                }
            }
            else if (mod._statModType == StatModType.PercentMult)
            {
                //TODO: 直接乘比較好懂???
                // finalValue *= mod.Value;
                finalValue *= mod.Value;
            }
        }

        // Workaround for float calculation errors, like displaying 12.00001 instead of 12
        return (float)Math.Round(finalValue, 4);
    }
    


    protected virtual float CalculateFinalValue()
    {
        // Debug.Log("Cal Value:" + BaseValue + "," + statModifiers.Count);
        statModifiers.Sort(_modifierOrder);
        return CalValueAfterModifier(statModifiers);
    }

    [PreviewInInspector] [NonSerialized] private List<StatModifier> _staticStatModifiers = new();

    public virtual float CalculateFinalValueWithoutTemporary()
    {
        _staticStatModifiers = statModifiers.FindAll(x => x.DurationType != StatModDurationType.Temporary);
        _staticStatModifiers.Sort(_modifierOrder);
        return CalValueAfterModifier(_staticStatModifiers);
    }
}

