using System.Collections;
using System.Collections.Generic;
using Mono.CSharp;
using UnityEngine;
using UnityEngine.SceneManagement;


public static class RCGLifeCycle
{
    public static void DontDestroyForever(GameObject gameObject)
    {
        GameObject.DontDestroyOnLoad(gameObject);
        DontDestroyObjList.Add(gameObject);
        gameObject.name += " (RCGLifeCycle)";
    }

    private static List<GameObject> DontDestroyObjList = new List<GameObject>();

    public static bool CanDestroy(GameObject g)
    {
        if (DontDestroyObjList.Contains(g))
        {
            return false;
        }
        return true;
    }
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
        yield return DestroyAll();
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
                
                if(this.CanDestroy(obj) == false)
                    continue;

                Debug.LogError("不該有其他東西！：" +obj.name);
            }
        }
        else
        {
            Debug.Log("乾乾淨淨");
        }
    }


    public void BackToTitle() 
        => SceneManager.LoadScene("TitleScreenMenu");

    private static GameObject FindSteamAPIObj() 
        => GameObject.Find("SteamAPI");

    public IEnumerator DestroyAll()
    {
        var gdkRunner = GameObject.Find("GdkRunner");
      
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

        Debug.Log("Destroying Everything");
        var SteamAPIOb = FindSteamAPIObj();

        foreach (var go in allObjects)
        {
            if (CanDestroy(go)) //把其他人都刪光光
            {
                go.SetActive(false);
            }
            else
            {
                Debug.Log("SteamAPI Not Destroyed");
            }
        }

        foreach (var go in allObjects)
            if (CanDestroy(go))
            {
                Destroy(go);
            }
            else
            {
                Debug.Log("SteamAPI Not Destroyed");
            }
        return null;
    }

    public bool CanDestroy(GameObject g)
    {
        if (g == null)
            return false;
        
        if (g == this.gameObject)
            return false;
        
        return RCGLifeCycle.CanDestroy(g);
    }
}
