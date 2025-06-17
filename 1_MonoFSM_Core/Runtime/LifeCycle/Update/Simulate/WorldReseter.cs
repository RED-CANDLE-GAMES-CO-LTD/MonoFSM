using System;
using System.Collections.Generic;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
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
    [Obsolete]
    [DefaultExecutionOrder(10000)]
    public class WorldReseter : MonoBehaviour //current world? //這個才該放在Runner上？
    {
        [Required]
        [CompRef] [Auto] private ISpawnProcessor _spawnProcessor;

        //FIXME: 感覺怪怪der
        //如何找到一個「系統」
        //
        // public static GameObject Spawn(GameObject spawnFrom, GameObject obj, Vector3 position, Quaternion rotation)
        // {
        //     spawnFrom.scene.GetRootGameObjects(rootGameObjects);
        //
        //     //問題：要保證在worldReseter下面
        //     return spawnFrom.GetComponentInParent<WorldReseter>()._spawnProcessor.Spawn(obj, position, rotation);
        // }


        /// <summary>
        /// Cache? scene?
        /// </summary>
        /// 
        private static List<GameObject> rootGameObjects = new();

        //FIXME: 不可用staitc? local runner用
        private static WorldReseter _currentWorldManager;
        public static WorldReseter CurrentWorldManager => _currentWorldManager;
        [PreviewInInspector] private WorldReseter PreviewWorldReseter => _currentWorldManager;
        private void Awake()
        {
            _currentWorldManager = this; 
        }

        private GameObject level;

        //FIXME: 讓PoolObject reset就好
        public void OnLevelStart() //怎麼警告沒有start？
        {
            Debug.LogError("OnLevelStart is deprecated, use OnSceneLoaded instead.");
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

            // Debug.Log("LevelRunner Start");
            //每次重置都要做的, LevelReset, LevelResetAfter?
            // ResetLevel(); //FIXME: network的時間點要在playerspawn之後?重新整理
            //EnterLevelReset
        }
        
    }
}