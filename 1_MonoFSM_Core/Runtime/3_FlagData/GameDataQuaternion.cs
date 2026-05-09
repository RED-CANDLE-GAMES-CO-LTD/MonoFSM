using UnityEngine;

[CreateAssetMenu(fileName = "NewQuaternionFlag", menuName = "GameFlag/Quaternion", order = 1)]
public class GameDataQuaternion : AbstractScriptableData<FlagFieldQuaternion, Quaternion>
{
    public override Quaternion CurrentValue
    {
        get => base.CurrentValue;
        set { base.CurrentValue = value; }
    }
}
