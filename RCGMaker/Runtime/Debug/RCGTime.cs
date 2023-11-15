using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public struct UniTaskWrapper : IDisposable
{
    public UniTaskWrapper(UniTask task, CancellationTokenSource tokenSource)
    {
        Task = task;
        _tokenSource = tokenSource;
        // DisposeLater().Forget();
    }

    // private async UniTaskVoid DisposeLater()
    // {
    //     await Task;
    //     _tokenSource?.Dispose();
    // }

    public UniTask Task { get; }

    private readonly CancellationTokenSource _tokenSource;
    
    public void Cancel()
    {
        if (_tokenSource == null) return;
        if (_tokenSource.IsCancellationRequested) return;
        try
        {
            _tokenSource?.Cancel();
        }
        catch (ObjectDisposedException e)
        {
            // Console.WriteLine(e);
            // throw;
        }

        // _tokenSource?.Dispose();
        
    }

    public void Dispose() //用 using(){}, 會自動呼叫
    {
        
        _tokenSource?.Dispose();
    }
}
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

    public static UniTask UnscaledDelay(this MonoBehaviour mb, float second)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(second), SelfTimeScale ? DelayType.DeltaTime : DelayType.UnscaledDeltaTime);
    }

    public static UniTask Delay(this MonoBehaviour mb, float second)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(second), DelayType.DeltaTime,
            cancellationToken: mb.GetCancellationTokenOnDestroy());
    }
    
    public static UniTask Delay(this MonoBehaviour mb, float second,CancellationToken cancelToken)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(second), DelayType.DeltaTime,
            cancellationToken: cancelToken);
    }


    public static UniTaskWrapper DelayTask(this MonoBehaviour mb, float second)
    {
        var tokenSource = new CancellationTokenSource();
        var task = UniTask.Delay(TimeSpan.FromSeconds(second), DelayType.DeltaTime,
            cancellationToken: tokenSource.Token);
        var wrapper = new UniTaskWrapper(task, tokenSource);
        return wrapper;
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