//主要給condition用的

using System;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

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
            if (field == null)
                return false;

            var result = Value;
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

    [ValueDropdown(nameof(GetFields))]
    public string fieldName;
    private FlagField<T> _runtimeField; //如果需要的話才要new

    private IEnumerable<string> GetFields()
    {
        if (flagBase != null)
        {
            var type = flagBase.GetType();
            var flagFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            // Debug.Log("flagFields Count" + flagFields.Length);
            var fieldNames = new List<string>();

            foreach (var flagField in flagFields)
            {
                Debug.Log(flagField.FieldType);
                //is inherited from FlagField<T>
                if (flagField.FieldType.IsSubclassOf(typeof(FlagField<T>)))
                    fieldNames.Add(flagField.Name);
            }

            // Debug.Log("fieldNames count" + fieldNames.Count);
            return fieldNames;
        }
        else
        {
            return Array.Empty<string>();
        }
    }
    
    //FIXME: 這個意思不是拿來compare用的...?
    [PreviewInInspector]
    public T Value
    {
        get => field!=null?field.CurrentValue:default(T);
        set => field.CurrentValue = value;
    }
    

    public FlagField<T> field
    {
        get
        {
            if (flagBase != null) //有用GameFlag
            {
                //用cache會拿到editor舊的值
                return flagBase.FindField<T>(fieldName);
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

[Serializable]
public class FlagFieldEntryFloat : FlagFieldEntry<float>
{
}