using MonoFSM.Core.Simulate;

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


public interface ILevelConfig //應該不需要了？
{
    void SetLevelConfig();
}

public interface ISceneAwakeReverse
{
    void EnterSceneAwakeReverse();
}

/// <summary>
/// 摸別人，進入場景後一次性
/// </summary>
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