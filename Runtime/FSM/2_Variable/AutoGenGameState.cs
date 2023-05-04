using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

// [RequireComponent(typeof(GuidComponent))]

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
#endif
}

//TODO: 要直接用dictionary access嗎？unique id怎麼來？c?