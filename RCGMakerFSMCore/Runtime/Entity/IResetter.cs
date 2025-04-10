using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IResetter
{
    //注意 
    //1. 關卡開始
    //2. 如果玩家跟存檔點講話
    //3. Cmd+R  
    //4. 還有從 pool出來。
    void EnterLevelReset();
    void ExitLevelAndDestroy(); //目前沒有特別意義，只有換景會call，和OnDestroy差不多
}


//1. 先回狀態
public interface IResetStateRestore //新規用這個，現在和上面都有call, exitLevelAndDestroy是為了換場景很煩可以拔掉
{
    void ResetStateRestore();
}

//2. 在跑這個
public interface IResetStart //摸別人
{
    void ResetStart();
}

/// <summary>
/// 1.LevelAwake,
/// 2.LevelAwakeReverse
/// 3.LevelStart,
/// 4.LevelStartReverse
/// </summary>
//關著也能call
public interface ILevelAwake //摸自己
{
    void EnterLevelAwake();
}

public interface ILevelConfig
{
    void SetLevelConfig();
}

public interface ISceneAwakeReverse
{
    void EnterSceneAwakeReverse();
}

public interface ISceneStart
{
    void EnterSceneStart();
}

public interface ISceneStartReverse
{
    void EnterSceneStartReverse();
}


public interface ISceneDestroy 
{
    void OnSceneDestroy();
}

public interface IClearReference //PoolObject return 會清這個
{
    void ClearReference();
}

public interface IGameDestroy
{
    void OnGameDestroy();
}
// public interface IResetPriority
// {
//     int GetPriority();
// }

