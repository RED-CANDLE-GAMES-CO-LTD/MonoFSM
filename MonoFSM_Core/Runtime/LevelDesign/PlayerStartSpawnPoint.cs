using MonoFSM_Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using RCGMaker.Core;
using Sirenix.OdinInspector;
using UnityEngine;

//Editor Debug用
public class PlayerStartSpawnPoint : MonoBehaviour, IBeforeBuildProcess,IActionParent,IResetStart
{
    public Transform editorPlayerRef;
    public Transform oriSpawnRef;
#if UNITY_EDITOR
    public InstanceReferenceData playerRef; //效能問題...
#endif
    // public GameObject InScenePlayer;
    [Button]
    public void ResetToOriPos()
    {
        if (oriSpawnRef == null)
            return;
        transform.position = oriSpawnRef.position;
    }
    

    [CompRef]
    [ShowInInspector] [AutoChildren] private IArgEventReceiver<Vector3> _onPlayerSpawn;

    [HideIf(nameof(oriSpawnRef))]
    [Button]
    private void CreateOriSpawnRef()
    {
        oriSpawnRef = new GameObject("oriSpawnRef").transform;
        oriSpawnRef.SetParent(transform.parent);
        oriSpawnRef.position = transform.position;
        oriSpawnRef.TryGetCompOrAdd<GizmoMarker>();
    }


    public void OnBeforeBuildProcess()
    {
        ResetToOriPos();
    }

    [SerializeField] Camera _camera;
    private void Update()
    {
        //Debug用，按`鍵，把player移到這個位置
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            Debug.Log("Alpha1 pRessed"+Input.mousePosition);
            if (Physics.Raycast(ray, out var hit)) _onPlayerSpawn.ArgEventReceived(hit.point);

            // var player = playerVar.Value;
            // Debug.Log(player,player);
            // player.transform.position = transform.position;
            // _onPlayerSpawn.EventReceived(transform.position);
        }
    }

    public void EventReceived(Vector3 arg)
    {
        // _onPlayerSpawn.EventReceived(arg);
        editorPlayerRef.position = arg;
    }

    public void ResetStart()
    {
        _onPlayerSpawn.ArgEventReceived(transform.position);
    }
}