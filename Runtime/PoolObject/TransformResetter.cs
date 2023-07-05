using UnityEngine;

//第一次記住
public class TransformResetter : MonoBehaviour, IResetter
{
    private Vector3 initPosition;
    private Quaternion initRotation;
    private Transform initParent;
    private Vector3 initlocalScale;
    private bool isResetParametterInit = false;

    private bool ParameterInitCheck()
    {
        if (isResetParametterInit)
            return true;

        initPosition = transform.position;
        initRotation = transform.rotation;
        initParent = transform.parent;
        initlocalScale = transform.localScale;

        isResetParametterInit = true;

        return false;
    }

    public void EnterLevelReset()
    {
        if (ParameterInitCheck())
        {
            transform.position = initPosition;
            transform.rotation = initRotation;
            transform.SetParent(initParent);
            transform.localScale = initlocalScale;
        }
    }

    public void ExitLevelAndDestroy()
    {
    }
}