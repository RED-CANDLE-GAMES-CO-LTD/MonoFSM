using System;
using UnityEngine;
using Sirenix.OdinInspector;
using MonoFSM.Core.Attributes;
using Object = UnityEngine.Object;

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

    [NonSerialized] protected StatModifier modifier;

    [TextArea] public string note;
    public CharacterStat TargetStat => statData ? statData.Stat : null;

    public void Apply(IStatModifierOwner source)
    {
        if (modifier == null)
        {
            modifier = new StatModifier(Value * AdditionalMultiplier, modType, source)
            {
                DurationType = DurationType
            };
            // 如果有ValueSource，就監聽他來更新
            if (ValueSource != null)
            {
                // Debug.Log("[StatModifierEntry]: ValueSource " + ValueSource, source as ScriptableObject);
                ValueSource.field.AddListener(OnValueChange, source as Object);
            }
            else
            {
                // Debug.Log("[Apply StatModifierEntry]: exist " + this, source as ScriptableObject);
                modifier.Value = Value * AdditionalMultiplier;
                modifier._statModType = modType;
                modifier.SetSource(source as Object);
                modifier.DurationType = DurationType;
            }

            TargetStat.AddModifier(modifier);
        }
    }

    private void OnValueChange(float arg0)
    {
        if (modifier != null)
        {
            modifier.Value = Value * AdditionalMultiplier;
            TargetStat.AddModifier(modifier);
        }
    }

    //自己監聽？
    public void Remove(IStatModifierOwner source)
    {
        TargetStat.RemoveModifier(modifier);

        if (ValueSource != null)
        {
            // Debug.Log("[StatModifierEntry]: ValueSource " + ValueSource, source as ScriptableObject);
            ValueSource.field.RemoveListener(OnValueChange, source as ScriptableObject);

            modifier = null;
        }
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