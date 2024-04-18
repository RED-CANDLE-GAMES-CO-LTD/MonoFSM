
using System;
using System.Collections.Generic;
using System.Diagnostics;
using RCGSetting;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class DebugProvider : MonoBehaviour, IHierarchyItemDisplay, IOverrideHierarchyIcon //往上找
{
    public void Awake()
    {
        if(IsLogInChildren)
            Debug.Log("[DebugProvider] Is LogInChildren"+this.gameObject.name,this.gameObject);
        // SaveLog("Awake",this);
    }

    [AutoChildren] StateMachineOwner _stateMachineOwner;
    public GeneralState currentState => _stateMachineOwner?.FsmContext?.currentStateType;

    private bool IsNotDebugMode => !DebugSetting.IsDebugMode && IsLogInChildren;

    [InfoBox("Is Not DebugMode, Will Not Log", InfoMessageType.Warning, VisibleIf = "IsNotDebugMode")]
    public bool IsLogInChildren = false;
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
    public List<LogEntry> logEntries = new List<LogEntry>();

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
            logEntries.Add(logEntry);
            // }
    }

    public string IconName => "console.infoicon@2x";
    public bool IsDrawingIcon => IsLogInChildren && DebugSetting.IsDebugMode;
}

[Serializable]
public class LogEntry
{
    [ShowInInspector]
    public string messageStr => message != null ? message.ToString():"";
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
        
        this.fileName = frame.GetFileName();
        this.lineNumber = frame.GetFileLineNumber();

        // Debug.Log("fileName:"+fileName+" lineNumber:"+lineNumber);
        // Application.OpenURL("jetbrains://idea/navigate/reference?project=Assets&path=Assets/3_Script/MonsterStates/AttackStateTrick/LinkMove/LinkNextMoveStateWeight.cs");
    }
#if UNITY_EDITOR
    [Button]
    public void GotoFile()

    {
        // 1, not 0, to skip the current method
        InternalEditorUtility.OpenFileAtLineExternal(fileName, lineNumber);
    }
#endif
}