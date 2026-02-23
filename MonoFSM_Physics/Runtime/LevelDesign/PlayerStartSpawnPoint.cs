using System.Linq;
using MonoFSM.Core;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.PhysicsWrapper;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

//Editor Debug用
public class PlayerStartSpawnPoint
    : AbstractDescriptionBehaviour,
        IUpdateSimulate,
        IBeforeBuildProcess,
        IActionParent,
        IResetStart
{
    // public override string Description { get; }
    protected override string DescriptionTag => "SpawnPoint";
    private void Start()
    {
        _camera = Camera.main;
    }

    // 靜態索引來跟踪當前選中的SpawnPoint
    private static int _currentSpawnPointIndex = 0;

    // 靜態方法來獲取所有SpawnPoint並按名稱排序
    public static PlayerStartSpawnPoint[] GetAllSpawnPoints()
    {
        return FindObjectsByType<PlayerStartSpawnPoint>(FindObjectsSortMode.None)
            .OrderBy(sp => sp.transform.GetSiblingIndex())
            .ToArray();
    }

    // 靜態方法來獲取當前選中的SpawnPoint
    public static PlayerStartSpawnPoint GetCurrentSpawnPoint()
    {
        var spawnPoints = GetAllSpawnPoints();
        if (spawnPoints.Length == 0)
            return null;

        // 確保索引在有效範圍內
        _currentSpawnPointIndex = Mathf.Clamp(_currentSpawnPointIndex, 0, spawnPoints.Length - 1);
        return spawnPoints[_currentSpawnPointIndex];
    }

    // 靜態方法來循環切換到下一個SpawnPoint
    public static PlayerStartSpawnPoint SwitchToNextSpawnPoint()
    {
        var spawnPoints = GetAllSpawnPoints();
        if (spawnPoints.Length == 0)
            return null;

        _currentSpawnPointIndex = (_currentSpawnPointIndex + 1) % spawnPoints.Length;
        return spawnPoints[_currentSpawnPointIndex];
    }

    // 靜態方法來重置到第一個SpawnPoint
    public static PlayerStartSpawnPoint ResetToFirstSpawnPoint()
    {
        _currentSpawnPointIndex = 0;
        return GetCurrentSpawnPoint();
    }

    [Required]
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

    //基本上就是瞬移玩家位置，
    [CompRef]
    [ShowInInspector]
    [AutoChildren]
    private IArgEventReceiver<Vector3> _playerTeleporter;

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

    [SerializeField]
    Camera _camera;

    [SerializeField]
    LayerMask _teleportHitLayerMask;

    public LayerMask TeleportHitLayerMask
    {
        get => _teleportHitLayerMask;
    }

    // [SerializeField]
    // private ValueProvider _currentPlayerEntityProvider;

    // private void Update()
    // {
    //
    // }

    private void ProcessTeleport(Vector3 point)
    {
        // _currentPlayerEntityProvider.GetSchema<Player>()
        _playerTeleporter?.ArgEventReceived(point);
    }

    // [Required]
    // [CompRef]
    private IRaycastProcessor _raycastProcessor => simulator.GetCompCache<IRaycastProcessor>();

    public void EventReceived(Vector3 arg)
    {
        // _onPlayerSpawn.EventReceived(arg);
        if (editorPlayerRef)
            editorPlayerRef.position = arg;
    }

    public void ResetStart()
    {
        //Network player都還沒生成
        // _onPlayerSpawn?.ArgEventReceived(transform.position);
    }

    public void Simulate(float deltaTime)
    {
        //Debug用，按`鍵，把player移到這個位置
        var keyboard = Keyboard.current;
        if (keyboard.backquoteKey.wasPressedThisFrame)
        {
            Debug.Log("backquoteKey Pressed", this);
            //第一人稱? 第三人稱？

            var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                // Create ray from camera through screen center
                ray = _camera.ScreenPointToRay(screenCenter);

                Debug.Log("backquoteKey Pressed at screen center", this);
            }

            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 10f);

            if (
                _raycastProcessor.Raycast(
                    ray.origin,
                    ray.direction,
                    out var hit,
                    1000,
                    _teleportHitLayerMask,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                //好無聊？寫死？character移動？DI問題?
                // _playerTeleporter?.ArgEventReceived(hit.point);
                ProcessTeleport(hit.point);
                Debug.Log("backquoteKey Pressed" + hit.point + hit.collider, hit.collider);
            }
            else
            {
                Debug.Log("No hit detected", this);
            }

            // var player = playerVar.Value;
            // Debug.Log(player,player);
            // player.transform.position = transform.position;
            // _onPlayerSpawn.EventReceived(transform.position);
        }
    }
}
