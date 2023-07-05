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
    void ExitLevelAndDestroy();
}

public interface ILevelAwake
{
    void EnterLevelAwake();
}

public interface ILevelStart
{
    void EnterLevelStart();
}

// public interface IResetPriority
// {
//     int GetPriority();
// }

