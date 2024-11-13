using UnityEngine;
using UnityEngine.SceneManagement;

namespace RCGMaker.Core
{
    [DefaultExecutionOrder(10000)]
    public class LevelRunner : MonoBehaviour
    {
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // public static void Init()
        // {
        //     SceneManager.sceneLoaded -= OnSceneLoaded;
        //     SceneManager.sceneLoaded += OnSceneLoaded;
        // }
        //
        // private static void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        void Start()
        {
            // Debug.Log("OnSceneLoaded" + arg0.name);
            var arg0 = gameObject.scene;
            // var level = new GameObject("Level");
            var allObjs = arg0.GetRootGameObjects();
            var level = gameObject;
            //put all objects into level
            foreach (var obj in allObjs)
            {
                obj.transform.SetParent(level.transform);
            }

            
            //只做一次awake, start
            PoolManager.HandleGameLevelAwakeReverse(level);
            PoolManager.HandleGameLevelAwake(level);
            PoolManager.HandleGameLevelStartReverse(level);
            PoolManager.HandleGameLevelStart(level);
            

            //每次重置都要做的, LevelReset, LevelResetAfter?
            PoolManager.LevelResetChildrenPrepareRuntimeData(level);
            //大便！
            PoolManager.HandleEnterLevelReset(level);
            //FIXME: 再重整一下
            PoolManager.LevelResetStart(level);
            //EnterLevelReset
        }
    }
}