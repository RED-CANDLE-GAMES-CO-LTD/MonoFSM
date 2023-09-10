using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// link:
/// </summary>
public static class RCGTime
{
    public static void SetTimeScaleUnsafe(float value)
    {
        timeScale = value;
    }

    private static float timeScale
    {
        get
        {
            return _timeScale;
        }
        set
        {
            if (SelfTimeScale)
            {
                _timeScale = value;
            }
            else
            {
                Time.timeScale = _timeScale = value;
                // Debug.Log("TimeScale:"+value);
            }
        }
    }

    public static UniTask UnScaledDelay(float second)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(second), SelfTimeScale ? DelayType.DeltaTime : DelayType.UnscaledDeltaTime);
    }

    public static UniTask Delay(float second)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(second), DelayType.DeltaTime);
    }

    public static UniTask DelayFrame(this MonoBehaviour mb, int frameCount)
    {
        return UniTask.DelayFrame(frameCount, cancellationToken: mb.GetCancellationTokenOnDestroy());
    }
    

    private static float _timeScale = 1f;

    public static bool SelfTimeScale = false;

    public static bool IsIndependentUpdate => !SelfTimeScale;
    
    

    public static float deltaTime
    {
        get
        {
            if (SelfTimeScale)
            {
                return Time.deltaTime * timeScale * GlobalSimulationSpeed;
            }
            else
            {
                return Time.deltaTime  * GlobalSimulationSpeed;
            }
        }
    }

    // public static float deltaTime => 0.02f * timeScale;
    public static float unscaledDeltaTime  {
        get
        {
            if (SelfTimeScale)
            {
                return  Time.deltaTime; //
            }
            else
            {
                return Time.unscaledDeltaTime;
            }
        }
    }
        
        

    public static bool IsPaused => timeScale == 0f;
    public static float TimeScale => timeScale * Time.timeScale;

    public static PlayerLoopTiming UpdateTiming =>
        PlayerLoopTiming.LastUpdate; //UniTask default會比script update還早，要用LastPostLateUpdate回放指令才會對

    public static float GlobalSimulationSpeed = 1;

    public static void ResetRCGTime()
    {
        timeScale = 1.0f;
        GlobalSimulationSpeed = 1.0f;
    }

}