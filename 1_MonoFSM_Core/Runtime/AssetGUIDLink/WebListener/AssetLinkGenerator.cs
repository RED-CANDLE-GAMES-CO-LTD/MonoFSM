using System;
// using DiscordWebhook;
using UnityEngine;

namespace MonoFSM.Runtime.WebAppIntegrate
{
    //注意不要混到editor code
    public interface ICurrentSceneInformation
    {
        string scene_guid { get; }
        Vector3 player_position { get; }

        [Obsolete]
        byte[] GetZipSaveFileBytes();

        [Obsolete]
        string[] GetSaveFiles();

        // UniTask<AdditionalFile[]> GetSaveFilesForBugReport();

        public string Version { get; } //用commit hash當作版本號，build時自動寫入
    }

    public static class AssetLinkGenerator
    {
        //for scene guid & player position(Vector3)
        public static string GenerateURLParamForScene(string scene_guid, Vector3 pos)
        {
            // var posJSON = JSONTemplates.FromVector3(pos);
            //小心不可以留下空白
            //左括弧右刮鬍也會被當作是錯的
            var posStr = pos.ToString().Replace(" ", "");
            //replace (,) to ""
            posStr = posStr.Replace("(", "").Replace(")", "");
            var url = "scene_guid" + "=" + scene_guid + "&" + "pos" + "=" + posStr;
            return GetLocalWebhookURL(url);
        }


        public static string GenerateURLParamForAsset(string asset_guid)
        {
            var url = "asset_guid" + "=" + asset_guid;
            return GetLocalWebhookURL(url);
        }

        public static JSONObject ParseCommandRuntime(string command)
        {
            JSONObject obj;
            //1. url格式
            if (command.Contains("http"))
            {
                Debug.Log("pasted url:" + GUIUtility.systemCopyBuffer);
                obj = ParseURLParam(command);
            }
            else //2. json格式
            {
                var link = command;
                Debug.Log("pasted json:" + link);
                //strip for content start from { to }
                //FIXME: 檢查link格式
                link = link.Substring(link.IndexOf("{", StringComparison.Ordinal));
                obj = new JSONObject(link);
            }

            Debug.Log("parsed JSON obj" + obj.ToString());
            //試看看是不是scene
            return obj;
        }

        public static JSONObject ParseURLParam(string url)
        {
            var param = url.Split('?')[1];
            var paramList = param.Split('&');
            var json = new JSONObject();
            foreach (var p in paramList)
            {
                var keyValue = p.Split('=');

                switch (keyValue[0])
                {
                    // case "external": //ex: coda link
                    //     //do something
                    //     json.AddField("external", keyValue[1]);
                    //     break;
                    // case "scene_guid":
                    //     //do something
                    //     json.AddField("scene_guid", keyValue[1]);
                    //     break;
                    case "pos":
//the format of pos is "(-1.0, 0.0, 0.0)"
                        var pos = keyValue[1].Replace("(", "").Replace(")", "").Split(',');
                        var x = float.Parse(pos[0]);
                        var y = float.Parse(pos[1]);
                        var z = float.Parse(pos[2]);
                        var posVector = new Vector3(x, y, z);
                        json.AddField("pos", JSONTemplates.FromVector3(posVector));
                        //do something
                        break;
                    // case "asset_guid":
                    //     //do something
                    //     json.AddField("asset_guid", keyValue[1]);
                    //     break;
                    default:
                        json.AddField(keyValue[0], keyValue[1]);
                        break;
                }
            }

            return json;
        }

        public static string GetLocalWebhookURL(string param)
        {
            return localhostURL + "webhook?" + param;
        }

        public static string localhostURL = "http://localhost:8888/";

        private static string WrapJsonBlock(string code)
        {
            return "```json\n" + code + "\n```";
        }
    }
}