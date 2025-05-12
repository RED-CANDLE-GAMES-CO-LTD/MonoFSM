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

        try
        {
            GameObject applicationCoreCandidate = Resources.Load<GameObject>("Configs/ApplicationCore");
            GameObject applicationCoreInstance = GameObject.Instantiate(applicationCoreCandidate);
            GameObject.DontDestroyOnLoad(applicationCoreInstance);
            return applicationCoreInstance.GetComponent<ApplicationCore>();
        }
        catch (Exception e)
        {
            Debug.LogError("Can't found: Configs/ApplicationCore.prefab");
            return null;
        }
    }
}
