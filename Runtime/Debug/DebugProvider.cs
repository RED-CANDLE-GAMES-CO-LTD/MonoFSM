
using System;
using UnityEngine;

public class DebugProvider : MonoBehaviour, IHierarchyItemDisplay//往上找
{
    public void Awake()
    {
        if(IsLogInChildren)
            Debug.Log("[DebugProvider] Is LogInChildren"+this.gameObject.name,this.gameObject);
    }

    public bool IsLogInChildren = false;
    public bool IsBreak;
    public bool IsBreakWhenStateChange;
    public bool CanDrawInHierarchy
    {
        get
        {
#if UNITY_EDITOR
            return IsLogInChildren;
#else
            return false;
#endif
        }
    }

}
