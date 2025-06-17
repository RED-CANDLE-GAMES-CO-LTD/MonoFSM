using System;
using System.Collections;
using System.Collections.Generic;
using MonoFSM.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public interface IBackToMenuDestroy
{
    public void BackToTitle();
}
public static class RCGLifeCycle
{
    public static void DontDestroyForever(GameObject gameObject)
    {
        if (gameObject == null)
        {
            Debug.LogWarning("DontDestroyForever: gameObject is null");
            return;
        }


        if (DontDestroyObjList.Contains(gameObject) == false)
        {
            DontDestroyObjList.Add(gameObject);
            Object.DontDestroyOnLoad(gameObject);
            gameObject.name += " (RCGLifeCycle)";
        }

        foreach (var gObject in gameObject.GetComponentsInChildren<Transform>(true))
        {
            if (DontDestroyObjList.Contains(gObject.gameObject) == false)
            {
                DontDestroyObjList.Add(gObject.gameObject);
                // Object.DontDestroyOnLoad(gObject.gameObject);
                // gObject.gameObject.name += " (RCGLifeCycle)";
            }
        }
        
    }
    private static readonly List<GameObject> DontDestroyObjList = new();

    public static bool CanDestroy(GameObject g) 
        => !DontDestroyObjList.Contains(g);
}

public class DestroyAllGameObjects : MonoBehaviour
{
    public static bool DestroyingAll;

    private void Start() 
        => StartCoroutine(_StartClear());

    private IEnumerator _StartClear()
    {
        DestroyingAll = true;
        PrimeTween.Tween.StopAll();
        Time.timeScale = 1;
        // yield return new WaitForSeconds(1f);
        //母災為啥要叫兩次才會清乾淨
        // yield return new WaitForSeconds(0.1f);
        yield return _DestroyAll();
        // yield return new WaitForSeconds(0.1f);
        CheckList();

        DestroyingAll = false;
        // yield return new WaitForSeconds(0.1f);
        UpdateLoopManager.Instance.OnGameDestroy();
        Resources.UnloadUnusedAssets();
        GC.Collect();
        BackToTitle();
    }

    private void CheckList()
    {
        var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allObjects.Length > 0)
        {
            for (int i = 0; i < allObjects.Length; i++)
            {
                var obj = allObjects[i];
                
                if(_CanDestroy(obj) == false)
                    continue;

                Debug.LogError("不該有其他東西！：" +obj.name);
            }
        }
        else
        {
            Debug.Log("乾乾淨淨");
        }
    }

    private IEnumerator _DestroyAll()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        

        foreach (var go in allObjects)
        {
            try
            {
                if (_CanDestroy(go)) //把其他人都刪光光
                {
                    go.SetActive(false);
                }
                else
                {
                    var destroyItems = go.GetComponents<IBackToMenuDestroy>();
                    foreach (var destroyItem in destroyItems)
                    {
                        destroyItem.BackToTitle();
                    }

                    // if (go.activeSelf)
                    // {
                    //     go.SetActive(false);
                    //     go.SetActive(true);
                    // }
                    
                    // Debug.Log("SteamAPI Not Destroyed");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(go + " cant Desable?");
                Debug.LogError(e);
            }
        }

        foreach (var go in allObjects) //是不是不該全刪，只手動刪掉該刪的就好(GameCore, application core)？只刪gamecore?
        {
            if (_CanDestroy(go))
            {
                Destroy(go);
            }
        }

        return null;
    }

    private bool _CanDestroy(GameObject g)
    {
        if (g == null)
            return false;
        
        if (g == gameObject)
            return false;
        if (g.name == "PrimeTweenManager")
            return false;
        return RCGLifeCycle.CanDestroy(g);
    }
    
    
    public void BackToTitle() 
        => SceneManager.LoadScene("TitleScreenMenu");
}