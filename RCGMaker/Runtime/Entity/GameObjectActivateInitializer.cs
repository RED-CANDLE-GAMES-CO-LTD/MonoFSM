using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitailActivator : MonoBehaviour, ILevelAwake
{
    public bool isActive = false;

    public void EnterLevelAwake()
    {
        Debug.Log("Initiial Activator!!");
        this.gameObject.SetActive(isActive);
    }
}
