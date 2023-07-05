using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

//暫時override某個值
//PlayerDieAnimState, 但沒得選有點髒
//用另一個Config給值。好像還行
public class GameStateStringOverrider : GameStateOverrider<GameFlagString, FlagFieldString, string>
{
}

//共用interface,
//共用實作
public abstract class GameStateOverrider<TGameState, TFlagField, TType> : MonoBehaviour, IResetter
    where TGameState : AbstractScriptableData<TFlagField, TType> where TFlagField : FlagField<TType>
{
    [Header("把GameState的CurrentValue改成某個值")] [FormerlySerializedAs("value")]
    public TType OverrideValue;

    [InlineEditor()]
    public TGameState flag;

    

    public void EnterLevelReset()
    {
        flag.CurrentValue = OverrideValue;
    }

    public void ExitLevelAndDestroy()
    {
        flag.Reset();
    }
}