using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/**
要定義Singleton時，繼承它就好
ex: FooBehaviour:SingletonBehaviour<FooBehaviour>
*/
public abstract class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static object s_Lock = new();
    private static T _instance = null;
    public static T InstanceRaw => _instance;
    private static bool _isInstanceCreated = false;

    protected void InSceneAwake(T t)
    {
        _isInstanceCreated = true;
        _instance = t;
        if (_instance && _instance.transform.parent == null)
        {
            if (Application.isPlaying)
                DontDestroyOnLoad(_instance);
        }
            
    }

    public static T Instance
    {
        get
        {
            
            if (Application.isPlaying == false)
            {
                var editorObjs = FindObjectsByType<T>(FindObjectsSortMode.None);
                if (editorObjs.Length > 0)
                    return editorObjs[0];
                Debug.LogError("No instance found of SingletonBehaviour in editor mode"+typeof(T).FullName);
                return null;
            }

            if (ApplicationIsQuiting)
                //   Debug.Log("Quiting");
                return null;

            if (DestroyAllGameObjects.DestroyingAll == true) return null;

            lock (s_Lock)
            {
                if (_isInstanceCreated)
                    return _instance;


                _instance = (T)FindFirstObjectByType(typeof(T));
                // TODO: Automatic creation
                if (_instance == null && destroyed == false)
                    // Debug.Log("Auto Generate" + typeof(T).FullName);
                    _instance = new GameObject(typeof(T).FullName + "(Singleton)", typeof(T)).GetComponent<T>();

                _isInstanceCreated = true;

                if (_instance && _instance.transform.parent == null)
                    DontDestroyOnLoad(_instance);
                return _instance;
            }
        }
    }

    public static bool IsAvailable() //不要直接叫Instance，會反而initialize
    {
        //FIXME: 不要在拿Instance的時候，initialize就好了
        // return true;
        return ApplicationIsQuiting == false && DestroyAllGameObjects.DestroyingAll == false && _isInstanceCreated ;
    }

    public static bool IsGameStopped => !IsAvailable();

    public static bool destroyed = false;

    public virtual void OnApplicationQuit()
    {
        ApplicationIsQuiting = true;
    }

    public virtual void OnDestroy()
    {
        // Debug.Log("Singleton ondestroy...");
        lock (s_Lock)
        {
            _instance = null;
            _isInstanceCreated = false;
        }
    }

#if UNITY_EDITOR
    //Editor關掉時
    public static bool ApplicationIsQuiting
    {
        get => PlayModeStateChangedExample.ApplicationIsQuiting;
        set => PlayModeStateChangedExample.ApplicationIsQuiting = value;
    }
#else
   private static bool ApplicationIsQuiting = false;
#endif
}

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class PlayModeStateChangedExample
{
    public static bool ApplicationIsQuiting = false;
    // register an event handler when the class is initialized

#if UNITY_EDITOR
    static PlayModeStateChangedExample()
    {
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }


    private static void LogPlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            ApplicationIsQuiting = false;
            DestroyAllGameObjects.DestroyingAll = false;
        }
    }
#endif
}