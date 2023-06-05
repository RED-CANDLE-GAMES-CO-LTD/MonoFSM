using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
[CreateAssetMenu(fileName = "StatData", menuName = "ScriptableObjects/StatData", order = 1)]
public class StatData : ScriptableObject
{
    //TODO: 不知道有沒有allStat裡？
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    void Reset()
    {
        // Debug.Log("StatData Reset" + name);
        stat?.Clear();
    }
    public void Clear() //重load時清除
    {
        stat?.Clear();
    }
    void OnEnable()
    {
        Reset();
    }
    private void OnDisable()
    {
        Reset();
    }
    [Header("能力值")]
    // public FlagFieldStat flagStat;
    //TODO:
    public CharacterStat stat;
    [ReadOnly]
    [ShowInInspector]
    [PropertyOrder(-1)]
    public virtual float Value => stat.Value;
    public string note;
}
