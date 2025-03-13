using System;
using UnityEngine;

//第一次記住
//看到dynamic rigidbody就應該要有這個
public class TransformResetter : MonoBehaviour, IResetter
{
    private Vector3 initPosition;
    private Quaternion initRotation;
    private Transform initParent;
    private Vector3 initlocalScale;
    private bool isResetParametterInit = false;

    [AutoChildren(false)] private Rigidbody2D rigidbody2D;

    private void Awake()
    {
        if (rigidbody2D != null && rigidbody2D.mass > 1)
        {
            Debug.LogError("rigidbody2D.mass > 1, 怪怪mass? 檢查一下是scene還是prefab?", this);
        }
    }

    private bool ParameterInitCheck()
    {
        if (isResetParametterInit)
            return true;


        initParent = transform.parent;
        initPosition = transform.localPosition;
        initRotation = transform.localRotation;
        initlocalScale = transform.localScale;

        isResetParametterInit = true;

        return false;
    }

    public void EnterLevelReset()
    {
        if (ParameterInitCheck())
        {
            transform.SetParent(initParent);
            transform.localPosition = initPosition;
            transform.localRotation = initRotation;
            transform.localScale = initlocalScale;
            
        }
    }

    public void ExitLevelAndDestroy()
    {
    }
}