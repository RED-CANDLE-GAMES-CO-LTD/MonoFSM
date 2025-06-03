using System;
using UnityEngine;

public static class CoreInitHandler
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeGameLevelLoadAndPrepareCores()
    {
        LoadCore();
    }

    public static ApplicationCore LoadCore()
    {
        if (ApplicationCore.IsAvailable())
            return ApplicationCore.Instance;
        GameObject applicationCoreCandidate = Resources.Load<GameObject>("Configs/ApplicationCore");
        try
        {
           
            if(applicationCoreCandidate == null)
            {
                Debug.LogError("Can't found: Configs/ApplicationCore.prefab, make sure you have it in the Resources folder");
                return null;
            }
            applicationCoreCandidate.gameObject.SetActive(false);
            GameObject applicationCoreInstance = GameObject.Instantiate(applicationCoreCandidate);
            
            //Auto Reference & Awake
            AutoAttributeManager.AutoReferenceAllChildren(applicationCoreInstance);
            applicationCoreCandidate.gameObject.SetActive(true);
            applicationCoreInstance.gameObject.SetActive(true);
            
            GameObject.DontDestroyOnLoad(applicationCoreInstance);
            return applicationCoreInstance.GetComponent<ApplicationCore>();
        }
        catch (Exception e)
        {
            Debug.LogError("Something wrong: Configs/ApplicationCore.prefab",applicationCoreCandidate);
            return null;
        }
    }
}
