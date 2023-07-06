using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;

//目的：想要在一個字串裡面，插入變數，最後組出來
[CreateAssetMenu(menuName = "ScriptableData/StringData")]
public class StringData : ScriptableObject
{
    public ScriptableObject[] variables;
    public LocalizedString mainText;
    public string Result => ReplaceVariableTag();

    private string ReplaceVariableTag()
    {
        var str = mainText.ToString();
        for (var i = 0; i < variables.Length; i++)
            if (variables[i] is IStringData provider)
                str = str.Replace("[var:" + i + "]", provider.GetString());

        return str;
    }

    public override string ToString()
    {
        return Result;
    }
}

public interface IStringData
{
    string GetString();
}