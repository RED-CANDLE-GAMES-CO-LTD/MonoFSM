using UnityEngine;

public static class ConditionHelper
{
    /// <summary>
    /// 每個frame跑condition會很貴嗎？可以cache?
    /// </summary>
    /// <param name="conditions"></param>
    /// <param name="owner"></param>
    /// <returns></returns>
    public static bool IsAllValid(this AbstractConditionBehaviour[] conditions, Object owner = null)
    {
        if (conditions == null || conditions.Length == 0)
            return true;
        foreach (var condition in conditions)
        {
            if (condition == null)
                continue;
            if (condition.gameObject.activeSelf == false) //只看自己，可能是parent有人關
                continue;
            if (condition == owner)
            {
                Debug.LogError(
                    "[ConditionHelper] Condition cannot reference itself!, will stackoverflow",
                    condition
                );
                continue;
            }
            if (condition.FinalResult == false)
                return false;
            // Debug.Log($"[ConditionHelper] {condition.name} is valid", condition.gameObject);
        }

        return true;
    }

    /// <summary>
    /// IsAllValid 的除錯變體：照 IsAllValid 同一套規則（null / activeSelf == false / 指到自己 都跳過），
    /// 回傳第一顆 FinalResult == false 的 index；全部成立回 -1。
    /// 給 FsmTrace 在 Enabled 時算「是哪一顆擋掉的」，平常的 IsAllValid 不受影響。
    /// </summary>
    public static int FirstFailedIndex(this AbstractConditionBehaviour[] conditions, Object owner = null)
    {
        if (conditions == null)
            return -1;
        for (var i = 0; i < conditions.Length; i++)
            if (EvaluateAt(conditions, i, owner) == ConditionEvalResult.False)
                return i;
        return -1;
    }

    /// <summary>
    /// IsAllValid 的逐顆版本：單獨評估第 index 顆，回傳它在 IsAllValid 裡會被怎麼看待。
    /// 快照（FsmTrace.CaptureSnapshot）用它把每顆 condition 的結果列出來；注意 IsAllValid 本身會在第一顆
    /// False 就短路，後面的在實際判定裡根本沒被問。
    /// </summary>
    public static ConditionEvalResult EvaluateAt(this AbstractConditionBehaviour[] conditions, int index,
        Object owner = null)
    {
        if (conditions == null || index < 0 || index >= conditions.Length)
            return ConditionEvalResult.SkippedNull;
        var condition = conditions[index];
        if (condition == null)
            return ConditionEvalResult.SkippedNull;
        if (condition.gameObject.activeSelf == false)
            return ConditionEvalResult.SkippedInactive;
        if (condition == owner)
            return ConditionEvalResult.SkippedSelfRef;
        return condition.FinalResult ? ConditionEvalResult.True : ConditionEvalResult.False;
    }

    public static bool IsAnyValid(this AbstractConditionBehaviour[] conditions, Object owner = null)
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        foreach (var condition in conditions)
        {
            if (condition == null)
                continue;
            if (condition == owner)
            {
                Debug.LogError(
                    "[ConditionHelper] Condition cannot reference itself!, will stackoverflow",
                    condition
                );
                continue;
            }
            if (!condition.gameObject.activeSelf) //只看自己，可能是parent有人關
                continue;
            if (condition.FinalResult)
                return true;
            // Debug.Log($"[ConditionHelper] {condition.name} is valid", condition.gameObject);
        }

        return false;
    }
}

/// <summary>
/// ConditionHelper.EvaluateAt 的結果：一顆 condition 在 IsAllValid 裡會被怎麼看待。
/// Skipped* 在 IsAllValid 裡等同「不計」（continue），不會讓整組變 false。
/// </summary>
public enum ConditionEvalResult : byte
{
    True,
    False,
    SkippedNull,
    SkippedInactive,
    SkippedSelfRef,
}
