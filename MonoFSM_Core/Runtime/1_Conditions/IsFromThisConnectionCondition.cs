using UnityEngine;

public class IsFromThisConnectionCondition : AbstractConditionComp
{
    public SceneConnection connection;
    protected override bool IsValid
    {
        get
        {
            return connection.IsOnTransition;
        }
    }
}
