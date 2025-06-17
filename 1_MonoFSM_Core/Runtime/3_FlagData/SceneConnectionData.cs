using System;
using System.Collections.Generic;
using MonoFSM.Core;
using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif


[CreateAssetMenu(menuName = "RCG/ConnectionData/SceneConnectionData", fileName = "SceneConnectionData", order = 1)]
public class SceneConnectionData : ScriptableObject
{
    public List<ConnectionRegisteredEntry> allRegisterredEntries;

    private bool _isTransitioning = false;
    public void MarkTransitioning() => _isTransitioning = true;
    public void ResolveTransitioning() => _isTransitioning = false;
    
    public bool IsTransitioning() => _isTransitioning;

    private void OnValidate()
    {
        if (allRegisterredEntries.Count > 2)
        {
            allRegisterredEntries.RemoveRange(2, allRegisterredEntries.Count - 2);
        }
    }

    public bool IsValideBind(SceneConnection connection)
    {
        var entry = allRegisterredEntries.Find((e) => e.ConnectionGUID == connection.ConnectionGUID);
        if (entry == null)
        {
            if (allRegisterredEntries.Count >= 2)
            {
                Debug.LogError("SceneConnection 不成對？");
                return false;
            }
        }
        return true;
    }

    public void UpdateConnectionData(SceneConnection connection)
    {
#if UNITY_EDITOR
       var entry = allRegisterredEntries.Find((e) => e.ConnectionGUID == connection.ConnectionGUID);
       if (entry == null)
       {
           if (allRegisterredEntries.Count >= 2)
           {
               Debug.LogError("SceneConnection 不成對？");
               return;
           }

           entry = new ConnectionRegisteredEntry();
           entry.ConnectionGUID = connection.ConnectionGUID;
           allRegisterredEntries.Add(entry);
       }

       if (allRegisterredEntries.Count > 2)
       {
           Debug.LogError("allRegisterredEntries.Count > 2",this);
       }

       entry.sceneName = connection.gameObject.scene.name;
       entry.connectionPointPos = connection.transform.position;
       entry.yaw = connection.transform.eulerAngles.y;
       this.SetDirty();
#endif
    }

    public ConnectionRegisteredEntry FindConnectionDestinationData(SceneConnection from)
    {
        ConnectionRegisteredEntry destinationData =
            allRegisterredEntries.Find((e) => e.ConnectionGUID != from.ConnectionGUID);
        return destinationData;
    }

}


[System.Serializable]
public class ConnectionRegisteredEntry
{
    public string ConnectionGUID;
    public string sceneName;
    public Vector3 connectionPointPos;
    public float yaw;
}