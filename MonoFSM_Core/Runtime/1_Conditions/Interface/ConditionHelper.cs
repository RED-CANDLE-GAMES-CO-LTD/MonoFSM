public static class ConditionHelper
{
    public static bool IsAllValid(this AbstractConditionComp[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return true;
        foreach (var condition in conditions)
        {
            if (condition == null)
                continue;
            if (condition.gameObject.activeSelf == false) //只看自己，可能是parent有人關
                continue;
            if (condition.FinalResult == false) return false;
        }

        return true;
    }
}