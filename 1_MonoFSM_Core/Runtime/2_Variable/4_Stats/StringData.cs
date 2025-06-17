using UnityEngine;
using MonoFSM.Core.Attributes;

//目的：想要在一個字串裡面，插入變數，最後組出來
[CreateAssetMenu(menuName = "ScriptableData/StringData")]
public class StringData : ScriptableObject
{
    public ScriptableObject[] variables;

    [Header("使用說明：依序用\"{n}\"來表達variables的字串，n是從0開始的index")]
    // public LocalizedString mainText;
    public string mainTextString;
    //[]: 不是很好檢查有沒有tag錯誤或漏？每個語言都要檢查和同步...


    [PreviewInInspector] public string Result => ReplaceVariableTag();

    private string ReplaceVariableTag()
    {
        var str = mainTextString;
        // var str = mainText.ToString();
        for (var i = 0; i < variables.Length; i++)
        {
            var token = "{" + i + "}";
            if (!str.Contains(token))
            {
                Debug.LogError($"{this} 沒有這個token:" + token, this);
                continue;
            }

            if (variables[i] is IStringData variableStr)
                str = str.Replace(token, variableStr.GetString());
        }


        return str;
    }

    public override string ToString() 
        => Result;
}

public interface IStringData
{
    string GetString();
}