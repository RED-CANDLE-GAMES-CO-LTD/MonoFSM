using RCGMaker.Core;
using UnityEngine;

namespace MonoFSM_Core.Runtime
{
    //FIXME: network level runner...
    /// <summary>
    /// Manages the lifecycle of a Unity level, including initialization, setup, and reset functionality.
    /// </summary>
    /// <remarks>
    /// This class is responsible for:
    /// <list type="bullet">
    ///   <item>Setting up the scene hierarchy with all root game objects parented to a single level object</item>
    ///   <item>Managing the execution order of Awake and Start events</item>
    ///   <item>Providing functionality to reset the level state</item>
    /// </list>
    /// The class has a high execution order (10000) to ensure it runs after other components have initialized.
    /// In the editor, it adds a menu item for resetting the level with a keyboard shortcut (CMD+Shift+R).
    /// </remarks>
    [DefaultExecutionOrder(10000)]
    public class LevelReseter : MonoBehaviour
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("MonoFSM/ResetLevel %R")]
        public static void TestResetLevel()
        {
            if (Application.isPlaying)
            {
                Debug.Log("ResetLevel CMD+Shift+R");
                FindFirstObjectByType<LevelReseter>().ResetLevel();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
#endif
            }
        }
#endif
        public void ResetLevel()
        {
            PoolManager.Instance.ResetReload(level);
        }
        private GameObject level;
        public void OnLevelStart()
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
            ResetLevel(); //FIXME: network的時間點要在playerspawn之後?重新整理
            //EnterLevelReset
        }
    }
}