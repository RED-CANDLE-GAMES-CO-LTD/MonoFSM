using System.Collections.Generic;
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
    
    public void UpdateConnectionData(SceneConnection connection)
    {
#if UNITY_EDITOR
       var entry = allRegisterredEntries.Find((e) => e.ConnectionGUID == connection.ConnectionGUID);
       if (entry == null)
       {
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
#endif
    }

    public ConnectionRegisteredEntry FindConnectionDestinationData(SceneConnection from)
    {
        ConnectionRegisteredEntry destinationData =
            allRegisterredEntries.FindLast((e) => e.ConnectionGUID != from.ConnectionGUID);
        return destinationData;
    }

}


[System.Serializable]
public class ConnectionRegisteredEntry
{
    public string ConnectionGUID;
    public string sceneName;
    public Vector3 connectionPointPos;
}