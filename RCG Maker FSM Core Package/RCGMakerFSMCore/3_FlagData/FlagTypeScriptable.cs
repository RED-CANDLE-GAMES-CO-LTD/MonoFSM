using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "FlagTypeScriptable", menuName = "FlagTypeScriptable", order = 1)]
//綁定類別和動畫
public class FlagTypeScriptable : ScriptableObject
{
 
    //Header("中寶箱都先統計進大寶箱")
    public FlagTypeScriptable OverrideBy;

    public FlagTypeScriptable GetMyType()
    {
        if (OverrideBy != null)
        {
            return OverrideBy;
        }
        
        return this;
    }
}

//飛兵要用新的scriptable, 還是統一clip名稱??? (baseClipName)