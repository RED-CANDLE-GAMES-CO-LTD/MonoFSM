//主要給condition用的

using System;
using Sirenix.OdinInspector;

[Serializable]
public class FlagFieldBoolEntry : FlagFieldEntry<bool>
{
    // public GameFlagBase flagBase;
    // public string fieldName;
    public bool IsResultInverted = false;

    [ShowInInspector]
    public bool isValid
    {
        get
        {
            var result = field.CurrentValue;
            if (IsResultInverted)
                return !result;
            else
                return result;
        }
    }
}
// public class FlagFieldStatConditionEntry : FlagFieldEntry<float>
// {
//     public enum Operator
//     {
//         Greater,
//         Smaller,
//         Equal,
//     }
//     public Operator op;
//     public float compareValue;
//     public bool isPercentage;
//     public bool isValid{
//         get{
//             //maxHealth, CurrentValue, compare.. so complicated...寫condition比較輕鬆?
//             Value
//             switch(op)
//             {

//             }
//             flagBase.FindField<bool>(fieldName).CurrentValue
//         }
//     }
// }
public class FlagFieldEntry<T> //沒有flagBase的話就runtime自己建立runtime variable
{
    // [Header("Flag Valid Entry")]
    [InlineEditor()]
    public GameFlagBase flagBase;
    public string fieldName;
    private FlagField<T> _runtimeField; //如果需要的話才要new

    [ShowInInspector]
    public T Value
    {
        get => field.CurrentValue;
        set => field.CurrentValue = value;
    }

    private FlagField<T> _field;

    public FlagField<T> field
    {
        get
        {
            if (flagBase != null) //有用GameFlag
            {
                if (_field == null)
                    _field = flagBase.FindField<T>(fieldName);
                return _field;
            }
            //主選單的選擇解析度是靠這個
            //[]: 會沒有存檔紀錄，第二次開很可能是錯的，有同步問題
            else //runtime only
            {
                if (_runtimeField == null)
                {
                    _runtimeField = new FlagField<T>();
                }

                return _runtimeField;
            }
        }
    }
}
// public abstract class AbstractFlagFieldBoolEntry : AbstractField<bool>
// {

// }
// public abstract class AbstractField<T>
// {
//     public abstract FlagField<T> field { get; }
//     public T Value => field.CurrentValue;
// }
[Serializable]
public class FlagFieldEntryInt : FlagFieldEntry<int>
{
}

[Serializable]
public class FlagFieldEntryString : FlagFieldEntry<string>
{
}