using System;
using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
[System.Serializable]
public class StatModifierEntry //有點醜ㄇ，PlayerStatModifier比較醜？
{
    [PropertyOrder(-1)]
    [PreviewInInspector]
    private float PreviewValue
    {
        get
        {
            if (TargetStat == null)
            {
                return -1;
            }

            //Preview用的所以只看BaseValue
            var baseValue = TargetStat.BaseValue;
            if (modType == StatModType.Flat)
            {
                return baseValue + value;
            }
            else if (modType == StatModType.PercentAdd)
            {
                return baseValue * (1 + value);
            }
            else if (modType == StatModType.PercentMult)
            {
                return baseValue * value;
            }

            return baseValue;
        }
    }
    public float value = 1f;
    public StatModType modType = StatModType.Flat;

    [InlineEditor()]
    public StatData statData;
    public StatModDurationType DurationType;

    // [ShowInInspector]
    [NonSerialized]
    protected StatModifier modifier;
    [TextArea] public string note;
    public CharacterStat TargetStat => statData ? statData.Stat : null;

    public void Apply(IStatModifierOwner source)
    {
        if (modifier == null)
        {
            // Debug.Log("[Apply StatModifierEntry]: " + source, source as ScriptableObject);
            modifier = new StatModifier(value, modType, source)
            {
                DurationType = DurationType
            };
        }
        else
        {
            modifier.Value = value;
            modifier.Type = modType;
            modifier.Source = source as ScriptableObject;
            modifier.DurationType = DurationType;
        }

        // Debug.Log("[Apply StatModifierEntry]: " + this, source as ScriptableObject);
        // statData.flagStat.AddModifier(modifier);
        TargetStat.AddModifier(modifier);
    }
    public void Remove()
    {
        // statData.flagStat.RemoveModifier(modifier);
        TargetStat.RemoveModifier(modifier);
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
