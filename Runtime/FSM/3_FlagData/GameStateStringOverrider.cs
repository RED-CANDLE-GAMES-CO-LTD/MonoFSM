using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public TGameState flag;
    public TType value;

    public void EnterLevelResetAndStart()
    {
        flag.CurrentValue = value;
    }

    public void ExitLevelAndDestroy()
    {
        flag.Reset();
    }
}