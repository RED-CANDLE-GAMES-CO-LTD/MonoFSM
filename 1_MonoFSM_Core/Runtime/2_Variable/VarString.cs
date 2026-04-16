using Sirenix.OdinInspector;
using UnityEngine;

public class VarString : AbstractFieldVariable<GameFlagString, FlagFieldString, string>,
    IStringTokenVar
{
    // public override GameFlagBase FinalData => BindData;
    public override bool IsValueExist => !string.IsNullOrEmpty(CurrentValue);
    // public string ValueInfo => CurrentValue;

    // [OnValueChanged(nameof(OnRichTextChanged))] [TextArea]
    // public string _richText;
    //
    // void OnRichTextChanged()
    // {
    //     Field.SetCurrentValue(_richText, this);
    // }
}
