using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RCGMaker.Core
{
    [DefaultExecutionOrder(10000)]
    public class LevelRunner : MonoBehaviour// SingletonBehaviour<LevelRunner>
    {
        //FIXME: 
        [MenuItem("RCG/ResetLevel %R")]
        public static void TestResetLevel()
        {

            if (Application.isPlaying)
            {
                Debug.Log("ResetLevel CMD+Shift+R");
                FindObjectOfType<LevelRunner>().ResetLevel();
            }
            else
            {
                #if UNITY_EDITOR
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                #endif
            }
                
        }
        
        public void ResetLevel()
        {
            PoolManager.Instance.ResetFromRoot(level);
        }
        
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // public static void Init()
        // {
        //     SceneManager.sceneLoaded -= OnSceneLoaded;
        //     SceneManager.sceneLoaded += OnSceneLoaded;
        // }
        //
        // private static void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        private GameObject level;
        void Start()
        {
            // Application.targetFrameRate = 60;
            // Debug.Log("OnSceneLoaded" + arg0.name);
            var arg0 = gameObject.scene;
            // var level = new GameObject("Level");
            var allObjs = arg0.GetRootGameObjects();
            level = gameObject;
            //put all objects into level
            foreach (var obj in allObjs)
            {
                obj.transform.SetParent(level.transform);
            }
            
            //FIXME: 這個導致不好debug...

            
            //只做一次awake, start
            PoolManager.HandleGameLevelAwakeReverse(level);
            PoolManager.HandleGameLevelAwake(level);
            PoolManager.HandleGameLevelStartReverse(level);
            PoolManager.HandleGameLevelStart(level);

            Debug.Log("LevelRunner Start");
            //每次重置都要做的, LevelReset, LevelResetAfter?
            PoolManager.Instance.ResetFromRoot(level);
            //EnterLevelReset
        }
    }
}