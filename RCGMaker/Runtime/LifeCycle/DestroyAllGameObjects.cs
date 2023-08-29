using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private static void CheckList()
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
                
                if(obj.name == "GdkRunner")
                    continue;
                
                if(obj.name == "DestroyAll")
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
        var wwiseGlobal = FindObjectOfType<AkInitializer>().gameObject;
        var gdkRunner = GameObject.Find("GdkRunner");
      
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

        Debug.Log("Destroying Everything");
        var SteamAPIOb = FindSteamAPIObj();

        foreach (var go in allObjects)
        {
            if (go != gameObject && 
                go != SteamAPIOb && 
                go != wwiseGlobal && 
                go != gdkRunner) //把其他人都刪光光
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
            if (go != gameObject && 
                go != SteamAPIOb && 
                go != wwiseGlobal && 
                go != gdkRunner) //把其他人都刪光光
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
