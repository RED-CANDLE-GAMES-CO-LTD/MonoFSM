using System.Collections.Generic;
using RCGMaker.Core;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonoFSMCore_SceneProcess: IProcessSceneWithReport
{
    public int callbackOrder { get; }
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        var buildProcesses = new List<IBeforeBuildProcess>();
        var rootobjs = scene.GetRootGameObjects();

        for (int i = 0; i < rootobjs.Length; i++)
        {
            buildProcesses.AddRange(rootobjs[i].GetComponentsInChildren<IBeforeBuildProcess>(true));
        }
        
        foreach (var iPreProcess in buildProcesses)
        {
            iPreProcess.OnBeforeBuildProcess();
        }
    }
}
