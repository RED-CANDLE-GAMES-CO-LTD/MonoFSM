using MonoFSM_Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using RCGMaker.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

//Editor Debug用
public class PlayerStartSpawnPoint : MonoBehaviour, IBeforeBuildProcess,IActionParent,IResetStart
{
    public Transform editorPlayerRef; //如果player是放在場景上
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
    [SerializeField] LayerMask _teleportHitLayerMask;
    private void Update()
    {
        //Debug用，按`鍵，把player移到這個位置
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            //第一人稱? 第三人稱？
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                // Create ray from camera through screen center
                ray = _camera.ScreenPointToRay(screenCenter);
                Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 10f);
            }

            if (Physics.Raycast(ray, out var hit,1000, _teleportHitLayerMask))
            {
                _onPlayerSpawn.ArgEventReceived(hit.point);
                Debug.Log("Alpha1 Pressed"+hit.point+hit.collider,hit.collider);
            }

            // var player = playerVar.Value;
            // Debug.Log(player,player);
            // player.transform.position = transform.position;
            // _onPlayerSpawn.EventReceived(transform.position);
        }
    }

    public void EventReceived(Vector3 arg)
    {
        // _onPlayerSpawn.EventReceived(arg);
        if(editorPlayerRef)
            editorPlayerRef.position = arg;
    }

    public void ResetStart()
    {
        //Network player都還沒生成
        _onPlayerSpawn?.ArgEventReceived(transform.position);
    }
}