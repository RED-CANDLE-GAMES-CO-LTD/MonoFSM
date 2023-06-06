using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
[System.Serializable]
public class StatModifierEntry //有點醜ㄇ，PlayerStatModifier比較醜？
{
    public float value = 1f;
    public StatModType modType = StatModType.Flat;

    [InlineEditor()]
    public StatData statData;
    public StatModDurationType DurationType;
    protected StatModifier modifier;
    [TextArea] public string note;
    public CharacterStat bindStat => statData.stat;

    public void Apply(IStatModifierOwner source)
    {
        if (modifier == null)
            modifier = new StatModifier(value, modType, 0, source)
            {
                DurationType = DurationType
            };

        // statData.flagStat.AddModifier(modifier);
        bindStat.AddModifier(modifier);
    }
    public void Remove()
    {
        // statData.flagStat.RemoveModifier(modifier);
        bindStat.RemoveModifier(modifier);
    }

}

//[]: 誰在用這個？
[CreateAssetMenu(fileName = "StatModifierData", menuName = "ScriptableObjects/StatModifierData", order = 1)]
public class StatModifierData : ScriptableObject, IStatModifierOwner
{

    public float value = 1f;
    // public ActorStatType statType;
    public StatModType modType = StatModType.Flat;
    public StatData statData;

    private void Awake()
    {
        modifier = new StatModifier(value, modType, 0, this);
    }

    protected StatModifier modifier;
    public CharacterStat bindStat => statData.stat;
    // public CharacterStat bindStat
    // {
    //     get
    //     {
    //         return GameCore.Instance.player.statManager.FindStat(statType);
    //     }
    // }

    public void Apply()
    {
        // if (bindStat == null)
        // {
        //     BindStat(player);
        // }
        // Debug.Log("add effect to stat" + +modifier.Value);
        bindStat.AddModifier(modifier);
    }
    public void Remove()
    {
        bindStat.RemoveModifier(modifier);
    }
    // public virtual void PlayAnimation()
    // {

    // }
    public bool IsActivated => true;
}
