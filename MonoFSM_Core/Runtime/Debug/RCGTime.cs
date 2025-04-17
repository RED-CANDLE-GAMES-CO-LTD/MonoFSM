using System;
using System.Threading;
using Cysharp.Threading.Tasks;
// using DG.Tweening;
using JetBrains.Annotations;
using PrimeTween;
using UnityEngine;

public struct UniTaskWrapper : IDisposable
{
    //悲劇
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
        _timeScale = value;
        timeScale = _timeScale * GlobalSimulationSpeed;
    }

    //FIXME: 還是這個要vote才對
    public static float timeScale
    {
        get => Time.timeScale;
        set =>
            // if (SelfTimeScale)
            // {
            //     _timeScale = value;
            // }
            // else
            // {
            Time.timeScale = value;
        // Debug.Log("TimeScale:" + value);
        // }
    }

    public static Tween DelayTask<T>([NotNull] this T target, float delayTime, Action<T> action)
        where T : class
    {
        return Tween.Delay(target, delayTime, action, warnIfTargetDestroyed: false);
    }

    public static Tween DelayUITask<T>([NotNull] this T target, float delayTime, Action<T> action)
        where T : class
    {
        return Tween.Delay(target, delayTime, action, true);
    }

    public static void ExtendDelay(this Tween tween, float delay)
    {
        if (tween.isAlive)
        {
            var elapsedTime = tween.elapsedTime - delay;
            if (elapsedTime < 0) elapsedTime = 0;
            tween.elapsedTime = elapsedTime;
        }
        // PrimeTween.Tween.Delay(delay).Chain(tween); //FIXME: 不確定這個是對的嗎？
    }

    public static UniTask UnscaledDelay(this MonoBehaviour mb, float second)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(second),
            SelfTimeScale ? DelayType.DeltaTime : DelayType.UnscaledDeltaTime);
    }

    public static UniTask Delay(this MonoBehaviour mb, float second)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(second), DelayType.DeltaTime,
            cancellationToken: mb.GetCancellationTokenOnDestroy());
    }

    public static UniTask Delay(this MonoBehaviour mb, float second, CancellationToken cancelToken)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(second), DelayType.DeltaTime,
            cancellationToken: cancelToken);
    }


    // public static UniTaskWrapper DelayTask(this MonoBehaviour mb, float second,
    //     DelayType delayType = DelayType.UnscaledDeltaTime)
    // {
    //     var tokenSource = new CancellationTokenSource();
    //     var task = UniTask.Delay(TimeSpan.FromSeconds(second), delayType,
    //         cancellationToken: tokenSource.Token);
    //     var wrapper = new UniTaskWrapper(task, tokenSource);
    //     return wrapper;
    // }

    public static UniTask DelayFrame(this MonoBehaviour mb, int frameCount)
    {
        return UniTask.DelayFrame(frameCount, cancellationToken: mb.GetCancellationTokenOnDestroy());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInit()
    {
        _globalSimulationSpeed = 1;
        _timeScale = 1;
        SelfTimeScale = false;
    }

    private static float _timeScale = 1f;

    private static bool SelfTimeScale = false; //FIXME: 測試時要固定，改成true? 加速下tween也該加速


    public static bool IsUnscaledTime => !SelfTimeScale; //true

    //uncaledDeltaTime
    public static float cachedTime; //FIXME: 要有人來更新，TimePauseManager不在RCGMakerCore裡喔

    public static float deltaTime
    {
        get
        {
            if (SelfTimeScale)
                //FIXME: 寫爛了，不要再乘了？
                return Time.deltaTime; // * timeScale;
            else
                return Time.deltaTime;
        }
    }

    // public static float deltaTime => 0.02f * timeScale;
    public static float unscaledDeltaTime
    {
        get
        {
            if (SelfTimeScale) return Time.deltaTime; //

            return Time.unscaledDeltaTime;
        }
    }


    public static bool IsPaused => timeScale == 0f;
    public static float TimeScale => timeScale;

    public static void Pause()
    {
        GlobalSimulationSpeed = 0f;
    }

    public static PlayerLoopTiming UpdateTiming =>
        PlayerLoopTiming.LastUpdate; //UniTask default會比script update還早，要用LastPostLateUpdate回放指令才會對

    public static float GlobalSimulationSpeed //忘記為什麼要另外加一層這個了...
    {
        get => _globalSimulationSpeed;
        set
        {
            _globalSimulationSpeed = value;
            Time.timeScale = _globalSimulationSpeed * timeScale;
            Debug.Log("GlobalSimulationSpeed:" + _globalSimulationSpeed);
        }
    }

    private static float _globalSimulationSpeed = 1;

    public static void ResetRCGTime()
    {
        timeScale = 1.0f;
        // GlobalSimulationSpeed = 1.0f;
    }
}