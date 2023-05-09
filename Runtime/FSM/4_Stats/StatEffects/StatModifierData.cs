using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class StatModifierEntry //有點醜ㄇ，PlayerStatModifier比較醜？
{
    public float value = 1f;
    public StatModType modType = StatModType.Flat;
    public StatData statData;
    public StatModDuration Duration;
    protected StatModifier modifier;
    public CharacterStat bindStat => statData.stat;

    public void Apply(ScriptableObject source)
    {
        modifier = new StatModifier(value, modType, 0, source)
        {
            Duration = Duration
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


[CreateAssetMenu(fileName = "StatModifierData", menuName = "ScriptableObjects/StatModifierData", order = 1)]
public class StatModifierData : ScriptableObject
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
}
