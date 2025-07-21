using System;
using System.Collections.Generic;
using System.Diagnostics;
using MonoFSM.Core.Attributes;
using MonoDebugSetting;
using Sirenix.OdinInspector;
using UnityEditor;
#if UNITY_EDITOR
using RCGExtension;
using UnityEditorInternal;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class DebugProvider : MonoBehaviour, IEditorOnly
#if UNITY_EDITOR
    , IOverrideHierarchyIcon //往上找
#endif
{
#if UNITY_EDITOR
    [MenuItem("RCGMaker/Debug/Toggle DebugProvider %#_L")]
    public static void ToggleDebugProvider()
    {
        var debugProvider = Selection.activeGameObject.TryGetCompOrAdd<DebugProvider>();
        if (debugProvider)
        {
            debugProvider.IsLogInChildren = !debugProvider.IsLogInChildren;
            EditorUtility.SetDirty(debugProvider);
            Debug.Log("Toggle DebugProvider IsLogInChildren:" + debugProvider.IsLogInChildren);
        }

        EditorApplication.RepaintHierarchyWindow();
    }
#endif
    public void Awake()
    {
#if UNITY_EDITOR
        if (IsLogInChildren)
            Debug.Log("[DebugProvider] Is LogInChildren" + this.gameObject.name, this.gameObject);
#endif
        // SaveLog("Awake",this);
    }

    [AutoChildren] StateMachineOwner _stateMachineOwner;
    // public GeneralState currentState => _stateMachineOwner?.FsmContext?.currentStateType;

    private bool IsNotDebugMode => !DebugSetting.IsDebugMode && IsLogInChildren;

#if UNITY_EDITOR
    [InfoBox("Is Not DebugMode, Will Not Log", InfoMessageType.Warning, VisibleIf = "IsNotDebugMode")]
    public bool IsLogInChildren = false;
#else
    [NonSerialized]
     public bool IsLogInChildren = false;
#endif


    public bool IsBreak;
    public bool IsBreakWhenStateChange;

    public bool CanDrawInHierarchy
    {
        get
        {
#if UNITY_EDITOR
            return IsLogInChildren;
#else
            return false;
#endif
        }
    }

    // public List<LogEntry> logEntries = new List<LogEntry>();

    // [Button("Test")]
    // public void Test()
    // {
    //    SaveLog("Test",this);
    //    
    // }
    public void SaveLog(object message, Object context = null)
    {
        // if (IsLogInChildren)
        // {
        LogEntry logEntry = new LogEntry(message, context);
        // logEntries.Add(logEntry);
        // }
    }

    public string IconName => "console.infoicon@2x";
    public bool IsDrawingIcon => IsLogInChildren && DebugSetting.IsDebugMode;
    public Texture2D CustomIcon => null;
    public bool IsPosAtHead => true;
}

[Serializable]
public class LogEntry
{
    [ShowInInspector] public string messageStr => message != null ? message.ToString() : "";
    public object message;
    public Object context;
    public string fileName;
    public int lineNumber;
    public LogEntry(object message, Object context)
    {
        this.message = message;
        this.context = context;
        StackTrace stackTrace = new StackTrace(true);
        var frame = stackTrace.GetFrame(4);

        fileName = frame.GetFileName();
        lineNumber = frame.GetFileLineNumber();

        // Debug.Log("fileName:"+fileName+" lineNumber:"+lineNumber);
        // Application.OpenURL("jetbrains://idea/navigate/reference?project=Assets&path=Assets/3_Script/MonsterStates/AttackStateTrick/LinkMove/LinkNextMoveStateWeight.cs");
    }
#if UNITY_EDITOR
    [Button]
    public void GotoFile()

    {
        //?fileName=LinkNextMoveStateWeight.cs&line=1
        // 1, not 0, to skip the current method
        InternalEditorUtility.OpenFileAtLineExternal(fileName, lineNumber);
    }
#endif
}