using System.Linq;
using _1_MonoFSM_Core.Runtime.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

//場景空降玩家位置
public static class StartPointSelector
{
    //失敗了
    // private static void MoveSpawnPointToMousePos(PlayerStartSpawnPoint playerStartSpawnPoint)
    // {
    //     var mousePos = Event.current.mousePosition; //目前介面的位置, 現在focus來不及了唷
    //
    //     //current window position?
    //     // var sceneViewPos = SceneView.lastActiveSceneView.cameraViewport
    //     // Debug.Log("sceneViewPos:" + sceneViewPos);
    //
    //     //FIXME: 上面bar的高度，不知道怎麼判, 寫死
    //     mousePos.y -= 48;
    //     // SceneView.lastActiveSceneView.FixNegativeSize();
    //     //convert mouse position in world position
    //     var worldPosition = HandleUtility.GUIPointToWorldRay(mousePos).GetPoint(.1f);
    //     worldPosition.z = 0;
    //     //從ray拿到的點 z強迫設定為0
    //
    //     if (playerStartSpawnPoint)
    //     {
    //         playerStartSpawnPoint.transform.position = worldPosition;
    //         if (Application.isPlaying)
    //             playerStartSpawnPoint.playerRef.RunTimeInstance.transform.position = worldPosition;
    //     }
    //
    //     Debug.Log("static mousePos:" + mousePos);
    // }

    [MenuItem("RCGMaker/Toggle Global Position  _2", false, 0)]
    private static void ToggleGlobalPosition()
    {
        Tools.pivotRotation = PivotRotation.Global;
        EditorSnapSettings.gridSnapEnabled = true;
    }

    [MenuItem("RCGMaker/Select Scene  &_S", false, 0)]
    private static void SelectScene()
    {
        var scene = SceneManager.GetActiveScene();
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
        EditorGUIUtility.PingObject(sceneAsset);
    }

    [MenuItem("RCGMaker/Toggle Gizmo  _3", false, 0)]
    private static void ToggleGizmo()
    {
        //toggle on off of gizmo
        if (SceneView.lastActiveSceneView)
            SceneView.lastActiveSceneView.drawGizmos = !SceneView.lastActiveSceneView.drawGizmos;
    }

    [MenuItem("RCGMaker/Focus Player in SceneView  #_P", false, 0)]
    private static void FocusPlayerInSceneView()
    {
        var spawnPoint = Object.FindFirstObjectByType<PlayerStartSpawnPoint>();
        var player = spawnPoint.playerRef.RunTimeInstance;
        if (player)
        {
            Selection.activeGameObject = player.gameObject;
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.Focus();
        }
    }

    // [MenuItem("RCGMaker/SpawnPoint/Reset Spawn Point to Ori #_1", false, 200)]
    // private static void ResetSpawnPoint()
    // {
    //     var spawnPoint = Object.FindObjectOfType<PlayerStartSpawnPoint>();
    //     spawnPoint.ResetToOriPos();
    // }


    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorSceneManager.sceneOpened += SceneOpenedCallback;
    }

    private static void SceneOpenedCallback(Scene scene, OpenSceneMode mode)
    {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) //build ignore
            // Debug.LogWarning("SceneView is null, cannot move camera to spawn point.");
            return;
        //move camera to playerstartspawnpoint
        var spawnPoint = Object.FindFirstObjectByType<PlayerStartSpawnPoint>();
        if (spawnPoint != null)
        {
            sceneView.LookAt(spawnPoint.transform.position);
            Debug.Log("SceneOpenedCallback" + spawnPoint.transform.position);
        }
    }

    private static void FocusOnScene()
    {
        if (
            EditorWindow.focusedWindow != null
            && EditorWindow.focusedWindow.titleContent.text == "Game"
        )
            // Debug.Log("Game Window is focused");
            return;

        // Debug.Log("FocusOnScene");
        SceneView.lastActiveSceneView.drawGizmos = true;
        EditorWindow.FocusWindowIfItsOpen<SceneView>();
    }

    // 靜態索引來跟踪當前選中的SpawnPoint
    private static int _currentSpawnPointIndex;

    // 靜態方法來獲取所有SpawnPoint並按名稱排序
    public static PlayerStartSpawnPoint[] GetAllSpawnPoints()
    {
        return Object
            .FindObjectsByType<PlayerStartSpawnPoint>(FindObjectsSortMode.None)
            .OrderBy(sp => sp.name)
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

    [MenuItem("RCGMaker/SpawnPoint/Select SpawnPoint  _`", false, 0)]
    // [MenuItem("RCGMaker/SpawnPoint/Select SpawnPoint  _`", false, 0)]
    private static void DoSelectSpawnPoint()
    {
        //FIXME:Application.isplaying才跑這個？
        if (Application.isPlaying)
            return;
        FocusOnScene();
        // Debug.Log("DoSelectSpawnPoint: 1" + EditorWindow.focusedWindow);
        var spawnPoint = GetCurrentSpawnPoint();
        // SceneView.duringSceneGui += (SceneView sceneView) =>
        // {
        // MoveSpawnPointToMousePos(spawnPoint);
        // };

        if (spawnPoint)
        {
            Selection.activeGameObject = spawnPoint.gameObject;
            Debug.Log(
                $"Selected SpawnPoint: {spawnPoint.name} (Current Index: {GetCurrentSpawnPointIndex()})"
            );
        }
        else
        {
            Selection.activeGameObject = GameObject.Find("SpawnPoint");
        }
    }

    [MenuItem("RCGMaker/SpawnPoint/Switch to Next SpawnPoint  #_`", false, 1)]
    private static void DoSwitchToNextSpawnPoint()
    {
        FocusOnScene();
        var spawnPoint = SwitchToNextSpawnPoint();

        if (spawnPoint)
        {
            Selection.activeGameObject = spawnPoint.gameObject;
            Debug.Log(
                $"Switched to SpawnPoint: {spawnPoint.name} (Current Index: {GetCurrentSpawnPointIndex()})"
            );
        }
        else
        {
            Debug.Log("No SpawnPoints found in scene.");
        }
    }

    [MenuItem("RCGMaker/SpawnPoint/Reset to First SpawnPoint  &_`", false, 2)]
    private static void DoResetToFirstSpawnPoint()
    {
        FocusOnScene();
        var spawnPoint = ResetToFirstSpawnPoint();

        if (spawnPoint)
        {
            Selection.activeGameObject = spawnPoint.gameObject;
            Debug.Log($"Reset to first SpawnPoint: {spawnPoint.name}");
        }
        else
        {
            Debug.Log("No SpawnPoints found in scene.");
        }
    }

    public static int GetCurrentSpawnPointIndex()
    {
        var allSpawnPoints = GetAllSpawnPoints();
        var currentSpawnPoint = GetCurrentSpawnPoint();

        if (currentSpawnPoint != null)
        {
            for (int i = 0; i < allSpawnPoints.Length; i++)
            {
                if (allSpawnPoints[i] == currentSpawnPoint)
                    return i;
            }
        }
        return 0;
    }
}

//FIXME: 如果inspector鎖住，沒有顯示PlayerStartSpawnPoint，會無法移動
#if UNITY_EDITOR
// [CustomEditor(typeof(PlayerStartSpawnPoint))]
public class PlayerStartSpawnPointEditor
{
    // private Vector3 mousePos;
    //FIXME: GIZMO壞掉也會壞掉？
    // [InitializeOnLoad] // Makes the static constructor be called as soon as the scripts are initialized in the editor
    // public class EditorMousePosition
    // {
    [InitializeOnLoadMethod]
    private static void EditorMousePosition()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorSceneManager.sceneOpened += SceneOpenedCallback;
    }

    private static void SceneOpenedCallback(Scene scene, OpenSceneMode mode)
    {
        _target = Object.FindFirstObjectByType<PlayerStartSpawnPoint>();
    }

    private static PlayerStartSpawnPoint _target;

    private static PlayerStartSpawnPoint GetTarget
    {
        get
        {
            var playerStartSpawnPoint = _target;
            if (!playerStartSpawnPoint)
                playerStartSpawnPoint = Object.FindFirstObjectByType<PlayerStartSpawnPoint>();
            return playerStartSpawnPoint;
        }
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private static void OnSceneGUI(SceneView obj)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        if (Event.current.type == EventType.KeyDown)
            // Debug.Log(Event.current.type);
            // Check for specific keycodes
            if (Event.current.keyCode == KeyCode.BackQuote)
            {
                // 如果同時按下Shift，則切換到下一個SpawnPoint
                if (Event.current.shift)
                {
                    var nextSpawnPoint = StartPointSelector.SwitchToNextSpawnPoint();
                    if (nextSpawnPoint)
                    {
                        Selection.activeGameObject = nextSpawnPoint.gameObject;
                        _target = nextSpawnPoint;
                        Debug.Log(
                            $"Switched to SpawnPoint: {nextSpawnPoint.name} (Current Index: {StartPointSelector.GetCurrentSpawnPointIndex()})"
                        );
                    }
                }
                else
                {
                    // 正常按`鍵，使用當前選中的SpawnPoint進行移動
                    var currentSpawnPoint = StartPointSelector.GetCurrentSpawnPoint();
                    if (currentSpawnPoint)
                    {
                        Selection.activeGameObject = currentSpawnPoint.gameObject;
                        _target = currentSpawnPoint;
                        Debug.Log(
                            $"Selected SpawnPoint: {currentSpawnPoint.name} (Current Index: {StartPointSelector.GetCurrentSpawnPointIndex()})"
                        );
                    }

                    // Debug.Log("OnSceneGUI keycode:" + Event.current.keyCode + " pos:" + Event.current.mousePosition);
                    //FIXME: 2D遊戲用的...
                    if (obj.in2DMode)
                    {
                        MoveSpawnPointToMousePos(Event.current.mousePosition);
                    }
                    else
                    {
                        // Debug.Log("3D mode?");
                        // var ray =  obj.camera.ViewportPointToRay(Event.current.mousePosition);
                        // var ray =  obj.camera.ScreenPointToRay(Event.current.mousePosition);
                        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        if (
                            Physics.Raycast(
                                ray,
                                out var hit,
                                10000,
                                layerMask: currentSpawnPoint.TeleportHitLayerMask
                            )
                        )
                        {
                            Debug.Log("Set spawnPoint at" + hit.collider, hit.collider);
                            GetTarget.transform.position = hit.point;
                            GetTarget.EventReceived(hit.point);
                        }
                    }
                }
                Event.current.Use();
            }
    }

    private static void MoveSpawnPointToMousePos(Vector3 mousePos)
    {
        var playerStartSpawnPoint = GetTarget;

        if (!playerStartSpawnPoint)
            return;

        Selection.activeGameObject = playerStartSpawnPoint.gameObject;

        // Convert mouse position to world position
        var mousePosition = Event.current.mousePosition;
        // Convert GUI position to screen space
        var sceneView = SceneView.lastActiveSceneView;
        mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y;

        //get world position of 2d mouse position 投影到z=0的平面上
        // var worldPosition = sceneView.camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0));

        // Debug.Log("worldPosition:" + worldPosition);
        //convert mouse position in world position
        var worldPosition = HandleUtility.GUIPointToWorldRay(mousePos).GetPoint(.1f);
        worldPosition.z = 0;
        //從ray拿到的點 z強迫設定為0

        if (playerStartSpawnPoint)
        {
            Undo.RecordObject(playerStartSpawnPoint.transform, "Move Spawn Point");
            playerStartSpawnPoint.transform.position = worldPosition;
            playerStartSpawnPoint.EventReceived(worldPosition);
            if (Application.isPlaying)
                playerStartSpawnPoint.playerRef.RunTimeInstance.transform.position = worldPosition;
        }

        Debug.Log("mousePos:" + mousePos);
    }
}
#endif
