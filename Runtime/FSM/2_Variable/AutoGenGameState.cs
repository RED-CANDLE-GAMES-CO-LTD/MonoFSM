using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

// [RequireComponent(typeof(GuidComponent))]
public class AutoGenGameState : GuidComponent
{
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
}

//TODO: 要直接用dictionary access嗎？unique id怎麼來？c?