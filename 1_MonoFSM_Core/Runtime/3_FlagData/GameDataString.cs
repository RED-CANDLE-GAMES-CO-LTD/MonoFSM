using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGameDataStr", menuName = "GameData/String", order = 1)]
public class GameDataString : AbstractScriptableData<FlagFieldString, string> // GameFlagBase
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
