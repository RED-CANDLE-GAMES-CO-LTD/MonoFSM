using RCGMaker.Core;
using UnityEngine;

public class SceneConnection : MonoBehaviour,IOnBuildSceneSavingCallbackReceiver
{

    public string ConnectionGUID => this.gameObject.TryGetCompOrAdd<GuidComponent>().Guid.ToString();

    public SceneConnectionData connectionData;
    
    public ConnectionRegisteredEntry FindDestinationEntry () => connectionData.FindConnectionDestinationData(this);
    
    public bool IsOnTransition => connectionData.IsTransitioning();
    public void OnBeforeBuildSceneSave()
    {
        connectionData.UpdateConnectionData(this);
    }
}
