using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

//場景空降玩家位置
public static class StartPointSelector
{
    //失敗了
    private static void MoveSpawnPointToMousePos(PlayerStartSpawnPoint playerStartSpawnPoint)
    {
        var mousePos = Event.current.mousePosition; //目前介面的位置, 現在focus來不及了唷

        //current window position?
        // var sceneViewPos = SceneView.lastActiveSceneView.cameraViewport
        // Debug.Log("sceneViewPos:" + sceneViewPos);

        //FIXME: 上面bar的高度，不知道怎麼判, 寫死
        mousePos.y -= 48;
        // SceneView.lastActiveSceneView.FixNegativeSize();
        //convert mouse position in world position
        var worldPosition = HandleUtility.GUIPointToWorldRay(mousePos).GetPoint(.1f);
        worldPosition.z = 0;
        //從ray拿到的點 z強迫設定為0

        if (playerStartSpawnPoint)
        {
            playerStartSpawnPoint.transform.position = worldPosition;
            if (Application.isPlaying)
                playerStartSpawnPoint.playerRef.RunTimeInstance.transform.position = worldPosition;
        }

        Debug.Log("static mousePos:" + mousePos);
    }

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
        var spawnPoint = Object.FindObjectOfType<PlayerStartSpawnPoint>();
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
        //move camera to playerstartspawnpoint
        var spawnPoint = Object.FindObjectOfType<PlayerStartSpawnPoint>();
        if (spawnPoint != null)
        {
            sceneView.LookAt(spawnPoint.transform.position);
            Debug.Log("SceneOpenedCallback" + spawnPoint.transform.position);
        }
    }

    private static void FocusOnScene()
    {
        if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text == "Game")
            // Debug.Log("Game Window is focused");
            return;

        // Debug.Log("FocusOnScene");
        SceneView.lastActiveSceneView.drawGizmos = true;
        EditorWindow.FocusWindowIfItsOpen<SceneView>();
    }

    [MenuItem("RCGMaker/SpawnPoint/Select SpawnPoint  _1", false, 0)]
    // [MenuItem("RCGMaker/SpawnPoint/Select SpawnPoint  _`", false, 0)]
    private static void DoSelectSpawnPoint()
    {
        FocusOnScene();
        // Debug.Log("DoSelectSpawnPoint: 1" + EditorWindow.focusedWindow);
        var spawnPoint = Object.FindObjectOfType<PlayerStartSpawnPoint>();
        // SceneView.duringSceneGui += (SceneView sceneView) =>
        // {
        // MoveSpawnPointToMousePos(spawnPoint);
        // };

        if (spawnPoint)
            Selection.activeGameObject = spawnPoint.gameObject;

        // if (Application.isPlaying)
        // {
        //     Debug.Log("DoSelectSpawnPoint");
        //     var mousePos = Event.current.mousePosition;
        //     //把玩家移過來
        //     var worldPosition = HandleUtility.GUIPointToWorldRay(mousePos).GetPoint(.1f);
        //     worldPosition.z = 0;
        //     //從ray拿到的點 z強迫設定為0
        //     Debug.Log(worldPosition);
        //     spawnPoint.playerRef.instance.transform.position = worldPosition;
        //     // Object.FindObjectOfType<Game>().SetPosition(worldPosition);
        //     // if (Player.i) playerStartSpawnPoint.transform.position = worldPosition;
        //
        //     return;
        // }
        else
            Selection.activeGameObject = GameObject.Find("SpawnPoint");
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
        target = Object.FindObjectOfType<PlayerStartSpawnPoint>();
    }

    private static PlayerStartSpawnPoint target;

    private static PlayerStartSpawnPoint GetTarget
    {
        get
        {
            var playerStartSpawnPoint = target;
            if (!playerStartSpawnPoint) playerStartSpawnPoint = Object.FindObjectOfType<PlayerStartSpawnPoint>();
            return playerStartSpawnPoint;
        }
    }

    private static void OnSceneGUI(SceneView obj)
    {
        if (Event.current.type == EventType.KeyDown)
            // Debug.Log(Event.current.type);
            // Check for specific keycodes
            if (Event.current.keyCode == KeyCode.Alpha1)
            {
                Selection.activeGameObject = GetTarget.gameObject;
                Debug.Log("OnSceneGUI keycode:" + Event.current.keyCode + " pos:" + Event.current.mousePosition);
                //FIXME: 2D遊戲用的...
                if (obj.in2DMode)
                {
                    MoveSpawnPointToMousePos(obj, Event.current.mousePosition);
                }
                else
                {
                    // Debug.Log("3D mode?");
                    // var ray =  obj.camera.ViewportPointToRay(Event.current.mousePosition);
                    // var ray =  obj.camera.ScreenPointToRay(Event.current.mousePosition);
                    var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    if (Physics.Raycast(ray, out var hit, 100000))
                    {
                        // Debug.Log("3D mode"+hit.point);
                        GetTarget.transform.position = hit.point;
                        GetTarget.EventReceived(hit.point);
                    }
                    else
                    {
                        Debug.Log("3D mode no hit");
                    }
                }


                // if (Event.current.shift)
                // {
                //     Debug.Log("OnSceneGUI shift" + Event.current.keyCode);
                //     MoveTransformToMousePos(obj,Selection.activeGameObject.transform, Event.current.mousePosition);
                // }
                Event.current.Use();
            }
    }

    private static void MoveSpawnPointToMousePos(SceneView obj, Vector3 mousePos)
    {
        var playerStartSpawnPoint = target;
        if (!playerStartSpawnPoint) playerStartSpawnPoint = Object.FindObjectOfType<PlayerStartSpawnPoint>();

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