using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RCGLifeCycle
{
    public static void DontDestroyForever(GameObject gameObject)
    {
        Object.DontDestroyOnLoad(gameObject);
        DontDestroyObjList.Add(gameObject);
        gameObject.name += " (RCGLifeCycle)";
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
        Time.timeScale = 1;
        // yield return new WaitForSeconds(1f);
        
        //母災為啥要叫兩次才會清乾淨
        // yield return new WaitForSeconds(0.1f);
        yield return _DestroyAll();
        // yield return new WaitForSeconds(0.1f);
        CheckList();

        DestroyingAll = false;
        // yield return new WaitForSeconds(0.1f);

        BackToTitle();
    }

    private void CheckList()
    {
        var allObjects = FindObjectsOfType<GameObject>(true);
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
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        
        Debug.Log("Destroying Everything");

        foreach (var go in allObjects)
        {
            if (_CanDestroy(go)) //把其他人都刪光光
            {
                go.SetActive(false);
            }
            else
            {
                Debug.Log("SteamAPI Not Destroyed");
            }
        }

        foreach (var go in allObjects) //是不是不該全刪，只手動刪掉該刪的就好(GameCore, application core)？只刪gamecore?
        {
            if (_CanDestroy(go))
            {
                Destroy(go);
            }
            else
            {
                Debug.Log("SteamAPI Not Destroyed");
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