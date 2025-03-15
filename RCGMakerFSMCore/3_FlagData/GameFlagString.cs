using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBoolFlag", menuName = "GameFlag/String", order = 1)]
public class GameFlagString : AbstractScriptableData<FlagFieldString, string> // GameFlagBase
{
    // public FlagFieldString field;
    //
    // public string CurrentValue
    // {
    //     get
    //     {
    //         return field.CurrentValue;
    //     }
    //     set
    //     {
    //         field.CurrentValue = value;
    //     }
    // }
}