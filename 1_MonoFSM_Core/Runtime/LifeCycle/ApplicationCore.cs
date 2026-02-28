using Sirenix.OdinInspector;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class ApplicationCore : SingletonBehaviour<ApplicationCore>
{
    private void Start()
    {
        //FIXME: MonoPoolObj 會初始化了

        // SceneLifecycleManager.HandleGameLevelAwakeReverse(gameObject);
        // SceneLifecycleManager.HandleGameLevelAwake(gameObject);
        // SceneLifecycleManager.HandleGameLevelStartReverse(gameObject);
        // SceneLifecycleManager.HandleGameLevelStart(gameObject);
        // SceneLifecycleManager.ResetReload(gameObject);
        // Time.maximumDeltaTime = 0.03333f; //30fps, 這樣才不會simulation太多step
    }

    private bool IsInEditor => !gameObject.name.Contains("Custom");

#if UNITY_EDITOR
    [ShowIf("IsInEditor")]
    [Button("Custom Core")]
    void CreateCustomCore()
    {
        ApplicationCoreUtils.CopyApplicationCore();
    }
#endif
}
