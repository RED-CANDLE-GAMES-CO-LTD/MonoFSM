using MonoFSM.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class SceneConnection : MonoBehaviour,IOnBuildSceneSavingCallbackReceiver
{

    public string ConnectionGUID => this.gameObject.TryGetCompOrAdd<GuidComponent>().Guid.ToString();


    public SceneConnectionData connectionData;
    
    
#if UNITY_EDITOR
    public string sceneGUID = "";
    public string GetSceneGUID()
    {
        return AssetDatabase.AssetPathToGUID(this.gameObject.scene.path);
    }
#endif


    public ConnectionRegisteredEntry FindDestinationEntry () => connectionData.FindConnectionDestinationData(this);

    public bool IsOnTransition =>connectionData != null && connectionData.IsTransitioning();
    public void OnBeforeBuildSceneSave()
    {
        connectionData.UpdateConnectionData(this); 
    }

    private void OnValidate()
    {
        #if UNITY_EDITOR
        if (sceneGUID == "")
        {
            sceneGUID = GetSceneGUID();
            EditorUtility.SetDirty(this);
            
        }
        
        if (sceneGUID != GetSceneGUID())
        {
            connectionData = null;
            sceneGUID = GetSceneGUID();
            EditorUtility.SetDirty(this);
            return;
        }
        #endif
        
        if (connectionData!=null && connectionData.IsValideBind(this) == false)
        {
            connectionData = null;
            
        }
    }
}
