using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class CharacterStat
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
                CalValues();
               if(listener != null)listener.OnChange(_value,false);
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

    [ReadOnly]
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
    public virtual void AddModifier(StatModifier mod)
    {
        // Debug.Log("Stat modifier" + this);
        if (statModifiers.Contains(mod) == false)
        {
            isDirty = true;
            statModifiers.Add(mod);
            var v = Value;
            // Debug.Log("Character Stat Add Modifier" + mod.Value + mod.Type + "result:" + v);
        }

    }

    public virtual bool RemoveModifier(StatModifier mod)
    {
        if (statModifiers.Remove(mod))
        {
            isDirty = true;
            return true;
        }
        return false;
    }
    public void Clear()
    {
        statModifiers.Clear();
        _value = BaseValue;
        isDirty = true;
    }
    public virtual bool RemoveAllModifiersFromSource(object source)
    {
        int numRemovals = statModifiers.RemoveAll(mod => mod.Source == source);

        if (numRemovals > 0)
        {
            isDirty = true;
            return true;
        }
        return false;
    }

    protected virtual int CompareModifierOrder(StatModifier a, StatModifier b)
    {
        if (a.Order < b.Order)
            return -1;
        else if (a.Order > b.Order)
            return 1;
        return 0; //if (a.Order == b.Order)
    }

    // Calculate FinalValue For PermanentModifiers Only


    private float CalValueAfterModifier(List<StatModifier> statModifiers)
    {
        var finalValue = BaseValue;
        float sumPercentAdd = 0;
        for (var i = 0; i < statModifiers.Count; i++)
        {
            var mod = statModifiers[i];

            if (mod.Type == StatModType.Flat)
            {
                finalValue += mod.Value;
            }
            else if (mod.Type == StatModType.PercentAdd)
            {
                sumPercentAdd += mod.Value;

                if (i + 1 >= statModifiers.Count || statModifiers[i + 1].Type != StatModType.PercentAdd)
                {
                    finalValue *= 1 + sumPercentAdd;
                    sumPercentAdd = 0;
                }
            }
            else if (mod.Type == StatModType.PercentMult)
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
        statModifiers.Sort(CompareModifierOrder);
        return CalValueAfterModifier(statModifiers);
    }

    public virtual float CalculateFinalValueWithoutTemporary()
    {
        var modifiers = statModifiers.FindAll(x => x.Duration != StatModDuration.Temporary);
        modifiers.Sort(CompareModifierOrder);
        return CalValueAfterModifier(modifiers);
    }
}

