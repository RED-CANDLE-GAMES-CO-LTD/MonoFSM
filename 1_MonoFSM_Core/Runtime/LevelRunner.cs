using System;
using MonoFSM_Core.Runtime;
using UnityEngine;

namespace RCGMaker.Core
{
    public interface ILevelRunner
    {
        void LevelStart();
    }
    //怎麼檢查...
    
    //單機用
    [RequireComponent(typeof(LevelReseter))]
    public class LevelRunner : MonoBehaviour // SingletonBehaviour<LevelRunner>
    {
        [Auto] LevelReseter _levelReseter;
        private void Start()
        {
            _levelReseter.OnLevelStart();
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