using UnityEngine;

public class IsFromThisConnectionCondition : AbstractConditionComp
{
    public SceneConnection connection;
    protected override bool IsValid
    {
        get
        {
            if (connection == null)
                return false;
            
            return connection.IsOnTransition;
        }
    }
}
