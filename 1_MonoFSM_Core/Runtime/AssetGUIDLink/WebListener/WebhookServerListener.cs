using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using MonoFSM.Runtime.WebAppIntegrate;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace MonoFSM.Core
{
    [Serializable]
    public class WebhookServerListener  
    {
        private HttpListener _httpListener;

        [ShowInInspector]
        private bool _isServerRunning;

#if UNITY_EDITOR
        public static bool IsEditorServerRunning => serverListener is { _isServerRunning: true } &&
                                                    currentEditorState == PlayModeStateChange.EnteredEditMode;
        // public static bool IsRuntimeServerRunning => editorListener is { _isServerRunning: true };

        [InitializeOnLoadMethod]
        
        public static void Init()
        {
            // if (serverListener == null)
            //     serverListener = new WebhookServerListener();
            // serverListener.InitServer();
            // // //kill all server when editor is closed
            // serverListener.StopServer();
            // serverListener.StartServer();
            // EditorApplication.playModeStateChanged += state => { currentEditorState = state; };
        }
        private static PlayModeStateChange currentEditorState = PlayModeStateChange.EnteredEditMode;
#endif

        [RuntimeInitializeOnLoadMethod]
        public static void InitRuntime()
        {
#if RCG_DEV
            if (serverListener == null)
            {
                serverListener = new WebhookServerListener();
                serverListener.InitServer();
                serverListener.StartServer();
            }


            // //kill all server when editor is closed
            // listener.StopServer();

#endif
        }

        // private static WebhookServerWindow listener;
        private static WebhookServerListener serverListener;

        //?fileName=LinkNextMoveStateWeight.cs&line=1
        // 1, not 0, to skip the current method

        // private string fileName =
        //     "submodules/RCGMakerCore/RCGMaker/Editor/PlayerEditor/WebhookServer/WebhookServerWindow.cs";
        //
        // private int lineNumber = 1;

//         private void GoToFile()
//         {
//             // var stackTrace = new StackTrace(true);
//             // var frame = stackTrace.GetFrame(0);
//             //
//             // fileName = frame.GetFileName();
//             // lineNumber = frame.GetFileLineNumber();
//             // Debug.Log("fileName:" + fileName + " lineNumber:" + lineNumber);
// #if UNITY_EDITOR
//             InternalEditorUtility.OpenFileAtLineExternal(fileName, 1);
// #endif
//         }

        private void BringUpApplication()
        {
#if UNITY_EDITOR_OSX
            // var script = @"tell application ""System Events""
            //     set appName to ""Finder""
            //     if exists process appName then
            //     tell process appName to set frontmost to true
            //     end if
            //     end tell";
            // Debug.Log("script: " + script);
            // var process = new Process();
            // process.StartInfo.FileName = "/usr/bin/osascript";
            // process.StartInfo.Arguments = $"-e \"{script}\"";
            //
            // process.OutputDataReceived += (sender, args) => Debug.Log("Output: " + args.Data);
            // process.ErrorDataReceived += (sender, args) => Debug.LogError("Error: " + args.Data);
            // process.Start();
            //
            // process.WaitForExit();
            // var info = new FileInfo("/Applications/TextEdit.app/Contents/MacOS/TextEdit");
            // Process.Start(info.FullName);

            var reOpenTerminalScript = $"tell application \\\"Terminal\\\" to if not (exists window 1) then reopen";
            var activateTerminalScript = $"tell application \\\"Terminal\\\" to activate";
            var runMyScript = $"tell application \\\"Terminal\\\" to do script \\\"echo hello\\\" in window 1";
            var osaScript =
                $"osascript -e \'{reOpenTerminalScript}\' -e \'{activateTerminalScript}\' -e \'{runMyScript}\'";
            var bashCommand = $" -c \"{osaScript}\"";

            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = "/bin/bash",
                CreateNoWindow = false,
                Arguments = bashCommand
            };
            Process.Start(processStartInfo);
#endif
        }

        private void StartServer()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(AssetLinkGenerator.localhostURL);
            _httpListener.Start();
            _httpListener.BeginGetContext(OnRequest, _httpListener);
            _isServerRunning = true;
            Debug.Log("Server started, listening on http://localhost:8888/");
        }

        private void InitServer()
        {
            // EditorApplication.on
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(AssetLinkGenerator.localhostURL);
        }
        private void StopServer()
        {
            //kill AssetLinkGenerator.localhostURL
            _httpListener.Stop();
            _httpListener.Close();
            
            _isServerRunning = false;
            Debug.Log("Server stopped.");
        }

        //http://localhost:8080/webhook?command={asset:}

        private NameValueCollection ParseQueryString(string queryString)
        {
            var queryParameters = new NameValueCollection();
            var querySegments = queryString.Split('&');
            foreach (var segment in querySegments)
            {
                var parts = segment.Split('=');
                if (parts.Length > 0)
                {
                    var key = parts[0].Trim(new char[] { '?', ' ' });
                    var val = parts[1].Trim();

                    queryParameters.Add(key, val);
                    Debug.Log("key: " + key + " val: " + val);
                }
            }

            return queryParameters;
        }

        public interface IServerCommandProcessor
        {
            public void ReceiveCommand(string command);
        }

        public static Action<string, bool> EditorServerCommandProcessorListener;

        public static IServerCommandProcessor RuntimeServerCommandProcessor;

        private void EditorProcessUnityLink(string commandAssetGuid)
        {
            Debug.Log("Received assetGuid: " + commandAssetGuid);
            //FIXME: 如果是 editor, 
            // CommandParser.ParseRuntimeCommand(commandAssetGuid, false);
#if UNITY_EDITOR
            EditorServerCommandProcessorListener?.Invoke(commandAssetGuid, false);
#endif
            // if (RuntimeServerCommandProcessor != null)
            //     RuntimeServerCommandProcessor.ReceiveCommand(commandAssetGuid);
#if UNITY_EDITOR_OSX
                var script = @"tell application ""System Events""
                set appName to ""Unity""
                if exists process appName then
                tell process appName to set frontmost to true
                end if
                end tell";
                Debug.Log("script: " + script);
                var process = new Process();
                process.StartInfo.FileName = "/usr/bin/osascript";
                process.StartInfo.Arguments = $"-e \"{script}\"";
                process.Start();
#endif
// #if UNITY_EDITOR_OSX
//                 var script = "tell application \"Unity\" to activate";
//                 var process = new Process();
//                 process.StartInfo.FileName = "/usr/bin/osascript";
//                 process.StartInfo.Arguments = $"-e \"{script}\"";
//                 process.Start();
// #endif
        }

        public static string WebCommandToProcess;
        private void OnRequest(IAsyncResult result)
        {
            if (_httpListener is not { IsListening: true })
                return;

            var context = _httpListener.EndGetContext(result);
            var request = context.Request;
            //parse get parameters
            Debug.Log("Request URL: " + request.Url);
            var query = WebUtility.UrlDecode(request.Url.Query);
            Debug.Log("Request Query: " + query);
            // Get the command from the query string
            try
            {
                var queryDict = ParseQueryString(query);
                var assetGuid = queryDict["asset_guid"] ?? queryDict["scene_guid"];
                //queue up the command to be processed in the main thread
#if UNITY_EDITOR

                // if (assetGuid != null) {
                    //editor另外插進去 update loop
                    if (currentEditorState == PlayModeStateChange.EnteredEditMode)
                    {
                        _delayedMainThreadCall += () =>
                        {
                            _delayedMainThreadCall = null; //先清掉，避免後面error沒有清掉
                            EditorProcessUnityLink(request.Url.ToString());
                            // 不會彈窗，但至少工具列會有反應
                            UnityEditor.EditorUtility.FocusProjectWindow();
                        };
                        EditorApplication.update += DispatchMainThreadCalls;
                    }
                    else
                    {
                        //runtime cheat manager會去polling
                        WebCommandToProcess = request.Url.ToString();
                    }
                    // }
#else
    WebCommandToProcess = request.Url.ToString();
#endif
                // Debug.Log("Received assetGuid: " + assetGuid);
                // var asset = queryDict["asset"];

                //

                //there is no  HttpUtility

                // Read request data
                // string requestBody;
                // using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                // {
                //     requestBody = reader.ReadToEnd();
                // }

                // Debug.Log("Received webhook event: " + assetGuid);

                // Send response
                var response = context.Response;
                var responseString = "<html><body><h1>Webhook Received</h1></body></html>";
                //make response to close window of the browser
                responseString += "<script>window.close();</script>";

                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var responseOutput = response.OutputStream;
                responseOutput.Write(buffer, 0, buffer.Length);
                responseOutput.Close();

                // Continue listening for more requests
                _httpListener.BeginGetContext(OnRequest, _httpListener);
                // GUIUtility.systemCopyBuffer = command;
                // BugReportUtility.ParseCommandFromClipBoard();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private static Action _delayedMainThreadCall;
#if UNITY_EDITOR
        private void DispatchMainThreadCalls() {
            _delayedMainThreadCall?.Invoke();
            _delayedMainThreadCall = null;
            EditorApplication.update -= DispatchMainThreadCalls;
        }
#endif
    }
}