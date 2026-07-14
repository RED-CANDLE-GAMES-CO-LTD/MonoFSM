using Sirenix.OdinInspector;
using UnityEngine;

//有需要把字串寫進去的case嗎？還是其實只要static就好了
public class VarString : AbstractFieldVariable<GameDataString, FlagFieldString, string>,
    IStringTokenVar
{
    // public override GameFlagBase FinalData => BindData;
    public override bool IsValueExist => !string.IsNullOrEmpty(CurrentValue);

    public override string ValueInfo => CurrentValue;
    public override bool IsDrawingValueInfo => true;

    // [OnValueChanged(nameof(OnRichTextChanged))] [TextArea]
    // public string _richText;
    //
    // void OnRichTextChanged()
    // {
    //     Field.SetCurrentValue(_richText, this);
    // }
}
