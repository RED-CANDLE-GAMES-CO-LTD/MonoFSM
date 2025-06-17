using System;
using MonoFSM.Core.Runtime;
using UnityEngine;

namespace MonoFSM.Core
{
    public interface ILevelRunner
    {
        void LevelStart();
    }
    //怎麼檢查...
    
    //單機用
    [Obsolete]
    [RequireComponent(typeof(WorldReseter))]
    public class LocalLevelRunner : MonoBehaviour // SingletonBehaviour<LevelRunner>
    {
        [Auto] private WorldReseter _worldReseter;
        private void Start()
        {
            _worldReseter.OnLevelStart();
        }

        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // public static void Init()
        // {
        //     SceneManager.sceneLoaded -= OnSceneLoaded;
        //     SceneManager.sceneLoaded += OnSceneLoaded;
        // }
        //
        // private static void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)


        
    }
}