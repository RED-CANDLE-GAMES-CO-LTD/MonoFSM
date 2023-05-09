using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

// [RequireComponent(typeof(GuidComponent))]


//要用requireComponent嗎？

//疊積木：
//繼承：
//Component has Component [Auto]
//Component+RequireComponent
//Prefab=>Component+Component

//[]: 一定是景上才會有Auto Gen?
//Mode: InScene, InPrefab?
public class AutoGenGameState : GuidComponent
{
#if UNITY_EDITOR
    private string FindSceneGUID()
    {
        var scene = gameObject.scene;
        var path = scene.path;
        //get guid of scene
        var guid = AssetDatabase.AssetPathToGUID(path);


        return guid;
    }

    [ShowInInspector] private string SceneGUID => FindSceneGUID();
    [ShowInInspector] public string SaveID => SceneGUID + "_" + GetGuid();
    public string MyGuid => "" + GetGuid();
#endif
}

//TODO: 要直接用dictionary access嗎？unique id怎麼來？c?