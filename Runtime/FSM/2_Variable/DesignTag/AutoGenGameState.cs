using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
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
//[]: 必定 1對1，沒有要共用，有共用就不該Auto，應該手動生或用綁的
//[]: 我auto gen, 別人來綁我的
//Mode: InScene, InPrefab?

public interface IGameStateOwner
{
}

[RequireComponent(typeof(GameStateRequireAtPrefabKind))]
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

    [ShowInInspector] private MonoBehaviour _ownerMono => GetComponent<IGameStateOwner>() as MonoBehaviour;

    public override void OnBeforeSerialize()
    {
        base.OnBeforeSerialize();

        
        //從場景A 走到場景Ｂ 再走回場景Ａ SaveID 會變。 所以Application.IsPlaying 的狀態下不能做這件事。
        if (Application.isPlaying)
            return;
        
        if (IsAssetOnDisk()) return; //prefab就不可能auto gen?
        if (EditorUtility.IsPersistent(this)) return;
        // Debug.Log("Auto Gen When Save: " + gameObject.name);
        //改成ShowInInspector Property?
        if (_ownerMono == null)
            return;
        //find property with attribute [GameState] in owner's class
        var ownerType = _ownerMono.GetType();
        var fields = ownerType.GetFields();
        //FIXME: 這個在Inspector會一直叫，有點吵
        foreach (var field in fields)
        {
            var gameStateAttribute = field.GetAttribute<GameStateAttribute>();

            if (gameStateAttribute == null) continue;
            // Debug.Log("Auto Gen When Save: gameStateAttribute " + field.Name);
            //check value of field is not null
            var value = field.GetValue(_ownerMono) as GameFlagBase;
            if (value != null)
                //檢查ID有沒有對
                if (SaveID == value.SaveID)
                    continue;


            //幫他生成
            //if null, create new instance
            // var fieldType = field.FieldType;
            
            var gameStateData =
                field.FieldType.CreateGameStateSO(_ownerMono);
            if (gameStateData == null)
            {
                Debug.LogError("Fail to create GameStateSO for " + field.Name, this);
                continue;
            }

            // Debug.Log("Auto Gen When Save: " + field.Name + " " + gameStateData.name, gameStateData);
            field.SetValue(_ownerMono, gameStateData);
            _ownerMono.SetDirty();
        }
    }

    public override void OnAfterDeserialize()
    {
        base.OnAfterDeserialize();
    }
    //TODO: 找到旁邊class裡的[GameState], 幫他gen掉 
    
#endif
}

//TODO: 要直接用dictionary access嗎？unique id怎麼來？c?