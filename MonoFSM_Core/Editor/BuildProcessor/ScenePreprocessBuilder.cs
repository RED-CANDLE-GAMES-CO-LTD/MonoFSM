using System.Collections.Generic;
using RCGMaker.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ScenePreprocessBuilder: IPreprocessBuildWithReport
{
    public int callbackOrder { get; }
    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("OnPreprocessBuild");
        
        var scenes = EditorBuildSettings.scenes;
        foreach (var scene in scenes)
        {
            if (scene.enabled) 
                OpenSceneAndPreProcessLevel(scene.path);
        }
    }
    
    private void OpenSceneAndPreProcessLevel(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
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

        EditorSceneManager.SaveScene(scene);
    }

}
