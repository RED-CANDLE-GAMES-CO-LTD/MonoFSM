using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// link:
/// </summary>
public static class RCGTime
{
    public static float timeScale = 1f;

    public static float deltaTime => Time.deltaTime * timeScale * GlobalSimulationSpeed;

    // public static float deltaTime => 0.02f * timeScale;
    public static float unscaledDeltaTime => Time.deltaTime; //Time.unscaledDeltaTime;
    public static bool IsPaused => timeScale == 0f;
    public static float TimeScale => timeScale * Time.timeScale;

    public static PlayerLoopTiming UpdateTiming =>
        PlayerLoopTiming.LastUpdate; //UniTask default會比script update還早，播放指令要用LastPostLateUpdate

    public static float GlobalSimulationSpeed = 1;
}