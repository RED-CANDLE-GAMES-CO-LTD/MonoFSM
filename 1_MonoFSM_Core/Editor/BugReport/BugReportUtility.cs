using System;
using System.Linq;
using MonoFSM.Runtime.WebAppIntegrate;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.PlayerEditor
{
    public static class BugReportUtility
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            WebhookServerListener.EditorServerCommandProcessorListener += ParseCommand;
        }

        private static Object _copyReference;

        public static void QuickPaste(InspectorProperty property)
        {
            var type = property.ValueEntry.TypeOfValue;
            if (type == null)
            {
                Debug.LogError("Type is null");
                return;
            }

            if (_copyReference is GameObject go)
            {
                if (go.TryGetComponent(type, out var comp))
                {
                    property.ValueEntry.WeakSmartValue = comp;
                    property.ValueEntry.WeakValues.ForceMarkDirty();
                }
                else
                {
                    Debug.LogError("Can't find component:" + type);
                }
            }
            //type can be assign
            else if (type.IsAssignableFrom(_copyReference.GetType()))
            {
                property.ValueEntry.WeakSmartValue = _copyReference;
                property.ValueEntry.WeakValues.ForceMarkDirty();
            }
            // else if (copyReference.GetType() == type)
            // {
            //     property.ValueEntry.WeakSmartValue = copyReference;
            //     property.ValueEntry.WeakValues.ForceMarkDirty();
            // }
            else
            {
                Debug.LogError("Type not match:" + type + " copyReference of:" + _copyReference.GetType());
            }
        }

        [MenuItem("CONTEXT/Object/Select Asset", false, 0)]
        private static void SelectAsset(MenuCommand command)
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GetAssetPath(command.context));
        }

        [MenuItem("CONTEXT/Object/Copy Reference 複製引用", false, 0)]
        private static void CopyReference(MenuCommand command)
        {
            _copyReference = command.context;
        }

        [MenuItem("Assets/Copy Reference 複製引用  &%_C", false, 19)]
        [MenuItem("GameObject/Copy Reference 複製引用  &%_C", false, -1)]
        private static void CopyReference()
        {
            _copyReference = Selection.activeObject;
        }

        //Shift 2
        [MenuItem("RCGMaker/Link/Parse PosLink From ClipBoard  #_2", false, 0)]
        public static void ParseCommandFromClipBoard()
        {
            if (Application.isPlaying)
                return;
            var command = GUIUtility.systemCopyBuffer;
            ParseCommand(command);
            // var mousePos = Event.current.mousePosition;
            // //把玩家移過來
            // var worldPosition = HandleUtility.GUIPointToWorldRay(mousePos).GetPoint(.1f);
            // worldPosition.z = 0;
            // //從ray拿到的點 z強迫設定為0
            // Debug.Log(worldPosition);
            // Selection.activeGameObject.transform.position = worldPosition;
        }

        //Shift 4
        [MenuItem("Tools/MonoFSM/Get AssetLink From Current Selection  #_4", false, 0)]
        private static void SaveCommandFromCurrentSelection()
        {
            var assetGUIDs = Selection.assetGUIDs;
            if (assetGUIDs == null || assetGUIDs.Length == 0)
            {
                GenerateGameObjectLink();
                return;
            }

            // var assetLink = GenerateLinkForAsset(assetGUIDs[0]);
            // Debug.Log("AssetLink:" + assetLink);
            // EditorUtility.DisplayDialog("生成Asset連結成功", assetLink, "OK");
            // GUIUtility.systemCopyBuffer = Selection.activeObject.name + "\n" + assetLink;
            GenerateAssetLink();
        }

        private static void GenerateGameObjectLink()
        {
            Debug.Log("currentSelection:" + Selection.activeGameObject.name, Selection.activeGameObject);
            string link;

            // Check if the selected GameObject has a component that might be PlayerStartSpawnPoint
            // Since we can't find the exact type, we'll use a generic approach
            var components = Selection.activeGameObject.GetComponents<Component>();
            var hasSpawnPoint = components.Any(c => c.GetType().Name.Contains("SpawnPoint"));

            if (hasSpawnPoint)
            {
                link = SavePositionOfCurrentSelection();
                UnityEditor.EditorUtility.DisplayDialog("SpawnPoint 連結生成成功 Link", link, "OK");
                GUIUtility.systemCopyBuffer = Selection.activeGameObject.name + "\n" + link;
                return;
            }

            var globalObjId = GlobalObjectId.GetGlobalObjectIdSlow(Selection.activeGameObject);
            link = GenerateURLParamForGameObject(globalObjId);

            Debug.Log("SceneLink:" + link);
            GUIUtility.systemCopyBuffer = Selection.activeGameObject.name + "\n" + link;
        }

        private static JObject ParseURLLink(string link)
        {
            Debug.Log("pasted url link:" + link);
            var jobj = AssetLinkGenerator.ParseURLParam(link);

            return jobj;
        }

        private static async void ParseCommand(string command, bool showPrompt = true)
        {
            JObject obj;
            //1. url格式
            if (command.Contains("http"))
            {
                obj = AssetLinkGenerator.ParseURLParam(command);
                //如果是coda link要怎麼做？
            }
            else //2. json格式
            {
                var link = command;
                Debug.Log("pasted json:" + link);
                //strip for content start from { to }
                //FIXME: 檢查link格式
                link = link.Substring(link.IndexOf("{", StringComparison.Ordinal));
                obj = JObject.Parse(link);
            }

            //FIXME: 好像不該runtime, editor 分開，應該走同樣流程，最末端實作才分開？
            // if (obj["external"] != null && obj["external"].ToString() == "coda")
            // {
            //     var tableID = obj["tableID"].ToString();
            //     var rowID = obj["rowID"].ToString();
            //     var rowObj = await CodaApi.GetRow(tableID, rowID);
            //     const string linkName = "unityLink";
            //     var unityLink = rowObj["values"][linkName]["url"].ToString();
            //     //FIXME: 只去某個scene? 怎麼loadsave at start?
            //     obj = AssetLinkGenerator.ParseURLParam(unityLink);
            // }

            Debug.Log("parsed JSON obj" + obj.ToString());
            //試看看是不是scene
            if (GoToBugLocation(obj, out var jumpToPos, showPrompt)) //FIXME要分
            {
                if (obj["guid"] != null)
                {
                    Debug.Log("ResolveGuid:" + obj["guid"]);
                    //有GUID Component, 找到對應的GameObject
                    var guid = Guid.Parse(obj["guid"].ToString());

                    var go = GuidManager.ResolveGuid(guid);
                    if (go == null)
                        Debug.LogError("Can't find GameObject with Guid:" + guid);
                    else
                        Debug.Log("ResolveGuid:" + go);
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                }
                else
                {
                    if (jumpToPos != Vector3.zero)
                        EditorApplication.EnterPlaymode();
                }
            }

            //試看看is不是asset
            else if (ParseAssetID(obj))
            {
                // EditorApplication.EnterPlaymode();
            }
            else
            {
                Debug.LogError($"pasted:{GUIUtility.systemCopyBuffer} is not valid link");
            }
        }

        [MenuItem("GameObject/分析GameObject數量", false, -1)]
        private static void CountGameObject()
        {
            var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            Debug.Log("GameObject數量:" + allGameObjects.Length);

            //component
            var compCount = Selection.activeGameObject.GetComponentsInChildren<Component>().Length;
            Debug.Log("Component數量:" + compCount);

            //find all gameobjects in children which include inactive
            var allcount = Selection.activeGameObject.GetComponentsInChildren<Transform>(true).Length;
            Debug.Log("包括關閉 GameObject數量:" + allcount);
            //component
            var allcompCount = Selection.activeGameObject.GetComponentsInChildren<Component>(true).Length;
            Debug.Log("包括關閉 Component數量:" + allcompCount);
        }


        public static string GenerateURLParamForGameObject(GlobalObjectId objectId)
        {
            // Debug.Log("objectId:" + objectId.ToString());
            var url = "globalId=" + objectId.ToString();
            return AssetLinkGenerator.GetLocalWebhookURL(url);
        }

        private static void GenerateAssetLink()
        {
            var assetGUIDs = Selection.assetGUIDs;
            var assetURL = AssetLinkGenerator.GenerateURLParamForAsset(assetGUIDs[0]);
            Debug.Log("AssetURL:" + assetURL);
            UnityEditor.EditorUtility.DisplayDialog("生成Asset網址成功", assetURL, "OK");
            GUIUtility.systemCopyBuffer = Selection.activeObject.name + "\n" + assetURL;
        }

        public static string GenerateLinkForScene(string scenePath, JObject json = null)
        {
            var sceneName = scenePath.Substring(scenePath.LastIndexOf("/", StringComparison.Ordinal) + 1);
            var sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            if (json == null)
                json = new JObject();
            json["scene_guid"] = sceneGuid;
            json["scene_name"] = sceneName;
            return WrapCode(json.ToString());
        }

        public static string GenerateLinkForAsset(string assetGuid)
        {
            var json = new JObject();
            json["asset_guid"] = assetGuid;
            return WrapCode(json.ToString());
        }


//FIXME: refactor到AssetLinkGenerator
        private static string WrapCode(string code)
        {
            return "```json\n" + code + "\n```";
        }

        //某個位置
        public static string SavePositionOfCurrentSelection()
        {
            var scenePath = SceneManager.GetActiveScene().path;
            var sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            return AssetLinkGenerator.GenerateURLParamForScene(sceneGuid,
                Selection.activeGameObject.transform.position);
        }

        public static void ProcessAssetGuid(string assetGuid)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null)
            {
                Debug.LogError("Can't find asset with guid:" + assetGuid);
                return;
            }

            //if asset is prefab
            if (PrefabUtility.IsPartOfPrefabAsset(asset))
                // Selection.activeGameObject = go;
                //open prefab Stage
                PrefabStageUtility.OpenPrefab(assetPath);
            else if (asset is SceneAsset)
                //open scene
                EditorSceneManager.OpenScene(assetPath);
            else
                //if asset is scriptable object
                Selection.activeObject = asset;
        }

        public static bool ParseAssetID(JObject obj)
        {
            // var obj = JObject.Parse(link);
            if (obj["asset_guid"] == null)
            {
                Debug.LogError("pasted link is not valid");
                return false;
            }

            var assetGuid = obj["asset_guid"].ToString();
            ProcessAssetGuid(assetGuid);

            return true;
        }

        public static bool GoToBugLocation(JObject obj, out Vector3 jumpTo, bool showPrompt = true)
        {
            var pos = Vector3.zero;
            jumpTo = pos;
            if (obj["scene_guid"] == null)
            {
                Debug.LogError("pasted link is not scene");
                return false;
            }

            var sceneGuid = obj["scene_guid"].ToString();
            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);

            if (obj["pos"] != null)
            {
                Debug.Log("obj has pos Field:" + obj["pos"]);
                //obj["pos"] is (x,y,z), turn it into Vector3
                var posObj = obj["pos"];
                pos = new Vector3(
                    posObj["x"]?.ToObject<float>() ?? 0,
                    posObj["y"]?.ToObject<float>() ?? 0,
                    posObj["z"]?.ToObject<float>() ?? 0
                );

                Debug.Log("scene: " + sceneGuid + ",pos" + pos);
                jumpTo = pos;
            }

            var currentScene = SceneManager.GetActiveScene();
            var currentScenePath = currentScene.path;
            var currentGuid = AssetDatabase.AssetPathToGUID(currentScenePath);

            if (currentGuid != sceneGuid)
            {
                if (!showPrompt)
                {
                    EditorSceneManager.OpenScene(scenePath);
                    PlacePlayerAt(pos);
                    return true;
                }

                var result = UnityEditor.EditorUtility.DisplayDialog("Goto Scene",
                    "Goto Scene: " + sceneGuid + " pos: " + pos,
                    "Yes", "No");
                if (result)
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(scenePath);
                    PlacePlayerAt(pos);
                }
                else
                {
                    Debug.LogError("User Cancelled");
                    return false;
                }
            }
            else
            {
                PlacePlayerAt(pos);
                Debug.Log("Move Spawn Point to pos: " + pos);
            }

            return true;
        }

        private static void PlacePlayerAt(Vector3 pos)
        {
            if (pos != Vector3.zero)
            {
                // Try to find any spawn point component generically
                var spawnPoints = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .Where(mb => mb.GetType().Name.Contains("SpawnPoint"))
                    .ToArray();

                if (spawnPoints.Length > 0)
                    spawnPoints[0].transform.position = pos;
                else
                    Debug.LogWarning("No spawn point found to place player at position: " + pos);
            }
        }
    }
}