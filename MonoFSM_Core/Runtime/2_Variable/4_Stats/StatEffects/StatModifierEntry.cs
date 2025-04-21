using System;
using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

//fixme: 介面看不太懂，要重新設計一下...
[Serializable]
public class StatModifierEntry //有點醜ㄇ，PlayerStatModifier比較醜？
{
    [PropertyOrder(-1)]
    [PreviewInInspector]
    private float PreviewValue
    {
        get
        {
            if (TargetStat == null) return -1;

            //Preview用的所以只看BaseValue
            var baseValue = TargetStat.BaseValue;
            if (modType == StatModType.Flat) return baseValue + Value;

            if (modType == StatModType.PercentAdd) return baseValue * (1 + Value);

            if (modType == StatModType.PercentMult) return baseValue * Value;

            return baseValue;
        }
    }

    private float Value => ValueSource ? ValueSource.Value : value; //可以吃ScriptableDataFloat的value

    [HideIf("@ValueSource != null")] [Title("數值(簡易")]
    public float value;

    [InlineEditor] [Title("外部數值來源")] public GameDataFloat ValueSource;
    public StatModType modType = StatModType.Flat;

    [InlineEditor] public StatData statData;
    public StatModDurationType DurationType;

    [Header("額外要乘的值，可能因為數量")] //還是把數量當作一個modifier的providing value就好...有點難
    [NonSerialized]
    [PreviewInInspector]
    public float AdditionalMultiplier = 1f;

    // [ShowInInspector]
    [NonSerialized] protected StatModifier modifier;

    [TextArea] public string note;
    public CharacterStat TargetStat => statData ? statData.Stat : null;

    public void Apply(IStatModifierOwner source)
    {
        if (modifier == null)
        {
            // Debug.Log("[Apply StatModifierEntry]: new " + source, source as ScriptableObject);
            modifier = new StatModifier(Value * AdditionalMultiplier, modType, source)
            {
                DurationType = DurationType
            };
            //如果有ValueSource，就監聽他來更新
            if (ValueSource != null)
                // Debug.Log("[StatModifierEntry]: ValueSource " + ValueSource, source as ScriptableObject);
                ValueSource.field.AddListener(OnValueChange, source as ScriptableObject);
        }
        else
        {
            // Debug.Log("[Apply StatModifierEntry]: exist " + this, source as ScriptableObject);
            modifier.Value = Value * AdditionalMultiplier;
            modifier.Type = modType;
            modifier.Source = source as ScriptableObject;
            modifier.DurationType = DurationType;
        }

        // Debug.Log("[Apply StatModifierEntry]: " + this, source as ScriptableObject);
        // statData.flagStat.AddModifier(modifier);
        TargetStat.AddModifier(modifier);
    }

    private void OnValueChange(float arg0)
    {
        if (modifier != null)
        {
            modifier.Value = Value * AdditionalMultiplier;
            TargetStat.AddModifier(modifier);
            // Debug.Log("[StatModifierEntry] ValueSource OnValueChange" + arg0);
        }
    }

    //自己監聽？
    public void Remove(IStatModifierOwner source)
    {
        // statData.flagStat.RemoveModifier(modifier);
        TargetStat.RemoveModifier(modifier);

        if (ValueSource != null)
            // Debug.Log("[StatModifierEntry]: ValueSource " + ValueSource, source as ScriptableObject);
            ValueSource.field.RemoveListener(OnValueChange, source as ScriptableObject);

        modifier = null;
    }

    public void Clear()
    {
        if (modifier == null) return;
        if (ValueSource != null)
            if (modifier.Source != null)
                ValueSource.field.RemoveListener(OnValueChange, modifier.Source);

        modifier = null;
    }
}
//
// //[]: 誰在用這個？
// [CreateAssetMenu(fileName = "StatModifierData", menuName = "ScriptableObjects/StatModifierData", order = 1)]
// public class StatModifierData : ScriptableObject, IStatModifierOwner
// {
//
//     public float value = 1f;
//     // public ActorStatType statType;
//     public StatModType modType = StatModType.Flat;
//     public StatData statData;
//
//     private void Awake()
//     {
//         modifier = new StatModifier(value, modType, 0, this);
//     }
//
//     protected StatModifier modifier;
//     public CharacterStat bindStat => statData.stat;
//     // public CharacterStat bindStat
//     // {
//     //     get
//     //     {
//     //         return GameCore.Instance.player.statManager.FindStat(statType);
//     //     }
//     // }
//
//     public void Apply()
//     {
//         // if (bindStat == null)
//         // {
//         //     BindStat(player);
//         // }
//         // Debug.Log("add effect to stat" + +modifier.Value);
//         bindStat.AddModifier(modifier);
//     }
//     public void Remove()
//     {
//         bindStat.RemoveModifier(modifier);
//     }
//     // public virtual void PlayAnimation()
//     // {
//
//     // }
//     public bool IsActivated => true;
// }