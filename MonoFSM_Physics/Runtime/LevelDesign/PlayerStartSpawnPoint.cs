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
        IEditorResetToPlayTest, IResetStateRestore
{
    // public override string Description { get; }
    protected override string DescriptionTag => "SpawnPoint";
    private void Start()
    {
        _camera = Camera.main;
    }

    [SerializeField] private float _facingGizmoLength = 1.5f;

    private void OnDrawGizmos()
    {
        // 出生面向：畫出 forward 箭頭，方便在編輯器確認玩家出生時的實際朝向
        DrawArrow.ForGizmo(transform.position, transform.forward * _facingGizmoLength, Color.cyan,
            0.3f);

#if UNITY_EDITOR
        // 編輯器非播放中：spawnpoint 旋轉時，讓 editorPlayerRef 即時跟著轉（只在有變化時寫入，避免一直 dirty）
        if (!Application.isPlaying && editorPlayerRef != null &&
            editorPlayerRef.rotation != transform.rotation)
            UpdatePlayerRotation();
#endif
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

    private LogicAnimator _playerAnimator;

    public void MovePlayerToSpawnPointPos()
    {
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

    [GUIColor(0.4f, 1f, 0.4f)]
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

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(transform, "Reset Spawn Point To Ori Pos");
#endif
        transform.position = oriSpawnRef.position;
        EventReceived(oriSpawnRef.position);
    }

    [ShowIf(nameof(oriSpawnRef))]
    [Button]
    public void MoveOriSpawnRefToCurrentPos()
    {
        if (oriSpawnRef == null)
            return;

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(oriSpawnRef, "Move OriSpawnRef To Current Pos");
#endif
        oriSpawnRef.position = transform.position;
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
        var go = new GameObject("oriSpawnRef");
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Ori Spawn Ref");
        UnityEditor.Undo.RecordObject(this, "Create Ori Spawn Ref");
#endif
        oriSpawnRef = go.transform;
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
        {
            editorPlayerRef.position = arg;
            UpdatePlayerRotation();
        }
    }

    [Button]
    void UpdatePlayerRotation()
    {
        editorPlayerRef.rotation = transform.rotation;
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

    public void OnEditorResetToPlayTest()
    {
        ResetToOriPos();
    }

    public void ResetStateRestore(bool isHardReset)
    {
        // _onResetState.EventHandle();
        if (isHardReset)
            _playerTeleporter?.ArgEventReceived(oriSpawnRef != null
                ? oriSpawnRef.position
                : transform.position);
    }

    private AbstractEventHandler _onResetState;
}
