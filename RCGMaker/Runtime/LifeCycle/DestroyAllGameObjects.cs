using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DestroyAllGameObjects : MonoBehaviour
{
    public static bool DestroyingAll = false;

    void Start()
    {
        StartCoroutine(StartClear());
    }

    public IEnumerator StartClear()
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
                
                //應該要只剩下這兩個
                
                if(obj.name == "WwiseGlobal")
                    continue;
                
                if(obj.name == "SteamAPI")
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
    {
        SceneManager.LoadScene("TitleScreenMenu");
    }


    private GameObject FindSteamAPIObj()
    {
        return GameObject.Find("SteamAPI");
        // for (var i = 0; i < allObjects.Length; i++)
        // {
        //     var go = allObjects[i];
        //     if (go != gameObject) //把其他人都刪光光
        //         if (go.gameObject.name == "SteamAPI")
        //         {
        //             Debug.Log("SteamAPIFound");
        //             return go; // = go;
        //         }
        // }
    }

    public IEnumerator DestroyAll()
    {
        var wwiseGlobal = FindObjectOfType<AkInitializer>().gameObject;
      
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

        Debug.Log("Destroying Everything");
        var SteamAPIOb = FindSteamAPIObj();

        foreach (var go in allObjects)
        {
            if (go != gameObject && go != SteamAPIOb && go != wwiseGlobal) //把其他人都刪光光
            {
                if (go != null)
                    go.SetActive(false);
            }
            else
            {
                Debug.Log("SteamAPI Not Destroyed");
            }
        }

        foreach (var go in allObjects)
            if (go != gameObject && go != SteamAPIOb && go != wwiseGlobal) //把其他人都刪光光
            {
                if (go != null)
                    Destroy(go);
            }
            else
            {
                Debug.Log("SteamAPI Not Destroyed");
            }




        return null;

    }



}
