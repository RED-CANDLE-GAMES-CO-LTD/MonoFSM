using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitailActivator : MonoBehaviour, ILevelConfig
{
    public bool isActive = false;

    public void SetLevelConfig()
    {
        this.gameObject.SetActive(isActive);
    }


}
