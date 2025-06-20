using System;
using MonoFSM.Core.Simulate;
using MonoFSM.Core.Attributes; // For DropDownRef, PreviewInInspector
using MonoFSM.Variable; // For VarFloat, VarStat, VarBool
using UnityEngine;

// 耐力條，體幹..應該都可以用這個套？ (Stamina bar, posture... should be usable with this?)
// Refactored to handle stamina recovery with pause on external consumption.
//FIXME: timer?TickTimer 情境code...
public class StaminaTimer : MonoBehaviour, IUpdateSimulate 
{
    [Header("Stamina Properties")]
    [Tooltip(
        "The VarFloat representing the current stamina value. This variable should also provide a 'Max' value (e.g., _currentValue.Max).")]
    [DropDownRef]
    [SerializeField]
    private VarFloat _currentValue;

    [Header("Recovery Settings")]
    [Tooltip("The rate at which stamina recovers per second.")]
    [DropDownRef]
    [SerializeField]
    private VarStat _recoverRateStat;

    [Tooltip("The time in seconds to wait after consumption stops before stamina recovery begins.")]
    [DropDownRef]
    [SerializeField]
    private VarStat _waitTimeToRecover;

    // [Header("Consumption State")]
    // [Tooltip(
    //     "A boolean flag indicating if stamina is currently being consumed by an external system. Recovery pauses if true.")]
    // [DropDownRef]
    // [SerializeField]
    // private VarBool _isConsuming;

    // This 'currentTime' variable was present in your provided code.
    // Given that '_currentValue' (VarFloat) is used for stamina, the purpose of this 'float currentTime'
    // is unclear in the refactored stamina logic. It might be a leftover from a previous implementation.
    // It is not used in the current recovery logic.
    // [DropDownRef] public float currentTime;

    [Header("Internal State")]
    [PreviewInInspector]
    [Tooltip("Tracks the time elapsed while waiting to start recovery.")]
    private float pauseTimeCounter;

    public enum CountType
    {
        Increase, // Actively recovering stamina.
        Pause // Recovery is paused (due to active consumption, waiting period, or stamina being full).
    }

    [PreviewInInspector] public CountType countType = CountType.Pause; // Initial state.

    private float IncreaseSpeed => _recoverRateStat != null ? _recoverRateStat.FinalValue : 1f;
    private float WaitTimeToRecoverValue => _waitTimeToRecover != null ? _waitTimeToRecover.FinalValue : 0f;

    //FIXME:network怎麼處理？
    // private void Update() 
    // {
    //     Simulate(Time.deltaTime);
    // }

    public void Simulate(float deltaTime)
    {
        // Debug.Log(
        //     $"Simulating StaminaTimer with deltaTime: {deltaTime}, CurrentValue: {_currentValue.CurrentValue}, CountType: {countType}");
        //FIXME: 不要用varfloat, 直接知道？ value modifying order (才知道誰先誰後< 自動恢復應該最早)
        // var isConsumingNow = _isConsuming.CurrentValue;
        var currentStamina = _currentValue.CurrentValue;

        // IMPORTANT ASSUMPTION: Based on the original code's commented-out `currentTime.Max`,
        // it's assumed that your `VarFloat` type has a `Max` property (e.g., `_currentValue.Max`).
        // If `VarFloat` does not have a `.Max` property, you will need to add a
        // separate `VarFloat _maxStaminaValue` field and use `_maxStaminaValue.CurrentValue` here.
        var maxStamina = _currentValue.Max;

        // Debug.Log(
        //     $"Decreasing _currentValue.IsDecreasing{_currentValue.IsDecreasing}");
        // If stamina is being consumed, always pause recovery and reset the wait timer.
        if (_currentValue.IsDecreasing)
        {
            // Debug.Log(
            //     $"Stamina is being consumed, pausing recovery. Current Stamina: {currentStamina}, Max Stamina: {maxStamina}");
            countType = CountType.Pause;
            pauseTimeCounter = 0f;
            return; // Stop further processing for recovery this frame.
        }

        // if (!_currentValue.IsMax)
        //     Debug.Log(
        //         $"Stamina timer update: Current Stamina: {currentStamina}, Max Stamina: {maxStamina}, CountType: {countType}");
        
        switch (countType)
        {
            case CountType.Pause:
                
                // If stamina is not full, start/continue the wait timer.
                if (currentStamina < maxStamina)
                {
                    pauseTimeCounter += deltaTime;
                    
                    if (pauseTimeCounter >= WaitTimeToRecoverValue) countType = CountType.Increase;
                    // pauseTimeCounter will naturally be reset when moving to Pause state later (e.g., when full)
                    // or if consumption starts again.
                }
                else // Stamina is full (or somehow over max, clamp it)
                {
                    _currentValue.SetValue(maxStamina, this); // Ensure it's clamped to max.
                    pauseTimeCounter = 0f; // Reset timer as we are full and paused.
                }

                break;

            case CountType.Increase:
                // If stamina is not full, recover it.
                if (currentStamina < maxStamina)
                {
                    currentStamina += IncreaseSpeed * deltaTime;
                    var clamped = Mathf.Min(currentStamina, maxStamina); // Apply and clamp
                    _currentValue.SetValue(clamped, this); // Set the clamped value to the stamina variable.

                    // If stamina becomes full as a result of recovery, transition to Pause.
                    if (_currentValue.CurrentValue >= maxStamina)
                    {
                        countType = CountType.Pause;
                        pauseTimeCounter = 0f; // Reset wait timer for the next cycle.
                    }
                }
                else // Should ideally not be in Increase state if already full, but as a safeguard:
                {
                    _currentValue.SetValue(maxStamina, this); // Ensure it's clamped.
                    countType = CountType.Pause;
                    pauseTimeCounter = 0f;
                }

                break;
        }
    }

    public void AfterUpdate()
    {
    }
}