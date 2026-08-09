using System.Linq;
using MonoFSM.Core;
using MonoFSM.Core.DataProvider;
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
        // 編輯器非播放中：拖曳 spawnpoint 時，讓 editorPlayerRef 即時跟著位置+旋轉（只在有變化時寫入，避免一直 dirty）
        if (!Application.isPlaying && editorPlayerRef != null &&
            (editorPlayerRef.position != transform.position ||
             editorPlayerRef.rotation != transform.rotation))
            FollowSpawnPoint();
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

    /// <summary>
    ///     正式遊戲的出生位置：有設 oriSpawnRef 就用它，否則退回這個節點目前的位置。
    ///     （oriSpawnRef 只記位置，旋轉一律沿用 SpawnPoint 本身的 rotation）
    /// </summary>
    public Vector3 OriSpawnPosition => oriSpawnRef != null ? oriSpawnRef.position : transform.position;

    /// <summary>
    ///     測試用出生位置：關卡設計者把 SpawnPoint 拖到哪就從哪出生。
    /// </summary>
    public Vector3 PlayTestSpawnPosition => transform.position;

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
        if (_playerTeleporter == null)
        {
            Debug.LogWarning("[SpawnPoint] 找不到 _playerTeleporter(IArgEventReceiver<Vector3>)，瞬移沒有作用。", this);
            return;
        }

        // _currentPlayerEntityProvider.GetSchema<Player>()
        _playerTeleporter.ArgEventReceived(point);
    }

    //=== Cheat: 貼上 pos 連結即時瞬移 ===
    //連結格式沿用 AssetLinkGenerator.GenerateURLParamForScene：
    //  http://localhost:8888/webhook?scene_guid=xxx&pos=1.00,2.00,3.00
    //也接受純座標字串 "1,2,3"。這裡自己解析字串，避免 MonoFSM_Physics 多依賴 Json / MonoFSM-Pro。
    [Title("Cheat")]
    [Tooltip("PlayMode 中按 Ctrl/Cmd + V，讀剪貼簿的 pos 連結把玩家瞬移過去（不改出生點）")]
    [SerializeField]
    private bool _enablePasteTeleportCheat = true;

    [Tooltip("PlayMode 中按 Ctrl/Cmd + C，把玩家當下位置複製成 pos 連結到剪貼簿")]
    [SerializeField]
    private bool _enableCopyPosCheat = true;

    [Tooltip("PlayMode 中按 Ctrl/Cmd + Alt + R，soft reset 關卡並把玩家瞬移回 SpawnPoint 當下位置")]
    [SerializeField]
    private bool _enableTeleportToSpawnCheat = true;

    //場上多個 SpawnPoint 都會跑 Simulate，同一幀只處理一次貼上
    private static int _lastPasteTeleportFrame = -1;
    private static int _lastCopyPosFrame = -1;
    private static int _lastTeleportToSpawnFrame = -1;

    private static bool IsCtrlPressed(Keyboard keyboard)
    {
        return keyboard.leftCtrlKey.isPressed
               || keyboard.rightCtrlKey.isPressed
               || keyboard.leftCommandKey.isPressed
               || keyboard.rightCommandKey.isPressed;
    }

    private static bool IsAltPressed(Keyboard keyboard)
    {
        return keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
    }

    //Ctrl/Cmd + Alt + R：soft reset(由 CheatManager 觸發) + 把玩家瞬移回 SpawnPoint 當下位置。
    //跟 Cmd+R(soft reset，玩家不動) / Cmd+Shift+R(hard reset，回 oriSpawnRef) 形成三段互補。
    //瞬移跟 reset 的先後不影響結果：soft reset 的 ResetStateRestore(false) 本來就不動玩家位置。
    private void ProcessTeleportToSpawnCheat(Keyboard keyboard)
    {
        if (!IsCtrlPressed(keyboard) || !IsAltPressed(keyboard) ||
            !keyboard.rKey.wasPressedThisFrame)
            return;

        //場上多個 SpawnPoint 都會跑 Simulate，同一幀只處理一次
        if (_lastTeleportToSpawnFrame == Time.frameCount)
            return;
        _lastTeleportToSpawnFrame = Time.frameCount;

        var current = GetCurrentSpawnPoint();
        if (current == null)
        {
            Debug.LogWarning("[SpawnPoint] Cmd/Ctrl+Alt+R 瞬移：場上找不到 PlayerStartSpawnPoint", this);
            return;
        }

        var pos = current.PlayTestSpawnPosition;
        Debug.Log($"[SpawnPoint] Cmd/Ctrl+Alt+R reset 關卡並瞬移玩家回 SpawnPoint 位置 {pos}", current);
        current.ProcessTeleport(pos);
    }

    //Ctrl/Cmd + C：把玩家當下位置寫進剪貼簿，格式跟貼上端(TryParsePosFromLink)相容
    private void ProcessCopyPosCheat(Keyboard keyboard)
    {
        if (!IsCtrlPressed(keyboard) || !keyboard.cKey.wasPressedThisFrame)
            return;

        if (_lastCopyPosFrame == Time.frameCount)
            return;
        _lastCopyPosFrame = Time.frameCount;

        if (_playerTeleporter is not ICurrentPositionProvider posProvider)
        {
            Debug.LogWarning(
                $"[SpawnPoint] Ctrl+C 複製位置：_playerTeleporter({_playerTeleporter}) 沒實作 ICurrentPositionProvider",
                this);
            return;
        }

        if (!posProvider.TryGetCurrentPosition(out var pos))
        {
            Debug.LogWarning("[SpawnPoint] Ctrl+C 複製位置：取不到玩家當下位置", this);
            return;
        }

        var text = $"pos={pos.x:F2},{pos.y:F2},{pos.z:F2}";
        GUIUtility.systemCopyBuffer = text;
        Debug.Log($"[SpawnPoint] Ctrl+C 已複製玩家位置：{text}", this);
    }

    private void ProcessPasteTeleportCheat(Keyboard keyboard)
    {
        if (!IsCtrlPressed(keyboard) || !keyboard.vKey.wasPressedThisFrame)
            return;

        if (_lastPasteTeleportFrame == Time.frameCount)
            return;
        _lastPasteTeleportFrame = Time.frameCount;

        var clipboard = GUIUtility.systemCopyBuffer;
        if (!TryParsePosFromLink(clipboard, out var pos))
        {
            Debug.LogWarning($"[SpawnPoint] Ctrl+V 瞬移：剪貼簿解析不出 pos，內容='{clipboard}'", this);
            return;
        }

        Debug.Log($"[SpawnPoint] Ctrl+V 瞬移到 {pos}", this);
        ProcessTeleport(pos);
    }

    /// <summary>
    ///     從 webhook 連結（?pos=x,y,z）或純座標字串解析出位置。
    /// </summary>
    public static bool TryParsePosFromLink(string text, out Vector3 pos)
    {
        pos = Vector3.zero;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var payload = text.Trim();
        var posKeyIndex = payload.IndexOf("pos=", System.StringComparison.OrdinalIgnoreCase);
        if (posKeyIndex >= 0)
        {
            payload = payload.Substring(posKeyIndex + "pos=".Length);
            var ampersandIndex = payload.IndexOf('&');
            if (ampersandIndex >= 0)
                payload = payload.Substring(0, ampersandIndex);
        }

        payload = payload.Replace("(", "").Replace(")", "").Replace(" ", "");
        var parts = payload.Split(',');
        if (parts.Length < 3)
            return false;

        if (!float.TryParse(parts[0], out var x) ||
            !float.TryParse(parts[1], out var y) ||
            !float.TryParse(parts[2], out var z))
            return false;

        pos = new Vector3(x, y, z);
        return true;
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
        if (editorPlayerRef == null)
            return;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(editorPlayerRef, "Update Player Ref Rotation");
#endif
        editorPlayerRef.rotation = transform.rotation;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(editorPlayerRef);
#endif
    }

    // 讓 editorPlayerRef 同步 spawnpoint 的位置與旋轉（編輯器拖曳預覽用）
    [Button]
    void FollowSpawnPoint()
    {
        if (editorPlayerRef == null)
            return;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(editorPlayerRef, "Follow Spawn Point");
#endif
        editorPlayerRef.SetPositionAndRotation(transform.position, transform.rotation);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(editorPlayerRef);
#endif
    }

    public void Simulate(float deltaTime)
    {
        //Debug用，按`鍵，把player移到這個位置
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (_enableCopyPosCheat)
            ProcessCopyPosCheat(keyboard);

        if (_enablePasteTeleportCheat)
            ProcessPasteTeleportCheat(keyboard);

        if (_enableTeleportToSpawnCheat)
            ProcessTeleportToSpawnCheat(keyboard);

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
