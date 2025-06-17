using System;
using System.Collections;
using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
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

// [RequireComponent(typeof(GameStateRequireAtPrefabKind))]
public class AutoGenGameState : GuidComponent, ISceneSavingCallbackReceiver
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

    // [ShowInInspector] public string SaveID => SceneGUID + "_" + GetGuid();
    [ShowInInspector] public string SaveID => GetGuid() + "_" + SceneGUID;
    [ShowInInspector] public string MyGuid => GetGuid().ToString();

    [InfoBox("No GameStateOwner", InfoMessageType.Error, nameof(IsOwnerNull))]
    [ShowInInspector]
    private IGameStateOwner[] Owners => GetComponents<IGameStateOwner>();

    private bool IsOwnerNull => Owners == null || Owners.Length == 0;
    public void AutoGenCheck()
    {
        //FIXME: 一般的scene應該要ignore?沒有在build setting裡
        // Debug.Log("AutoGenCheck" + name, this);
        if (Application.isPlaying)
            return;

        if (IsAssetOnDisk())
        {
            Debug.LogError("AutoGenCheck: AssetOnDisk", this);
            return; //prefab就不可能auto gen?
        }

        if (EditorUtility.IsPersistent(this))
        {
            Debug.LogError("AutoGenCheck: Persistent", this);
            return;
        }

        if (!IsGuidAssigned()) //guid 0000的時候，不要gen，先等下面gen, 只是這個OnBeforeSerialize不會遞迴嗎... call stack有點醜的感覺
        {
            Debug.LogError("Guid not assigned", this);
            return;
        }
            
        // Debug.Log("Auto Gen When Save: " + gameObject.name);
        //改成ShowInInspector Property?
        if (Owners == null)
        {
            Debug.LogError("No GameStateOwner", this);
            return;
        }

        foreach (var o in Owners)
        {
            var owner = o as MonoBehaviour;
            if (owner == null)
                continue;
            //find property with attribute [GameState] in owner's class
            var fields = owner.GetType().GetFields();

            foreach (var field in fields)
            {
                var gameStateAttribute = field.GetAttribute<GameStateAttribute>();

                if (gameStateAttribute == null) continue;

                //check value of field is not null
                var value = field.GetValue(owner) as GameFlagBase;

                if (value != null)
                {
                    // Debug.Log("auto value gogo " + field.Name + " " + value.name, gameObject);
                    //檢查ID有沒有對
                    if (SaveID == value.GetSaveID)
                    {
                        // Debug.Log("SaveID == value.SaveID: " + field.Name + " " + value.name, gameObject);
                        continue;
                    }
                }
                //幫他生成
                //FIXME: 非正式scene的時候，不要生成？怎麼標記這件事，看有沒有在build setting?

                var data = field.FieldType.CreateGameStateSO(owner, gameStateAttribute.SubFolderName);
                if (data == null)
                    // Debug.LogError("Fail to create GameStateSO for " + field.Name, this);
                    continue;
                var gameStateData = data;
                Debug.Log("Auto Gen When Serialize: " + field.Name + " " + gameStateData.name, gameObject);
                field.SetValue(owner, gameStateData);
                var flagOwner = owner as IDataOwner;
                flagOwner?.FlagGeneratedPostProcess(gameStateData);
                owner.SetDirty();
                data.SafeSetDirty();
            }
        }
    }
#endif

    // public override void OnBeforeSerialize()
    // {
    //     base.OnBeforeSerialize();
    //
    //
    //     //[]: EditorSceneManager.sceneSaving 試試看
    //     
    //     //FIXME: 還是會遇到error...
    //     //UnityException: Calls to "AssetDatabase.LoadAssetAtPath" are restricted during domain backup. Assets may not be loaded while domain backup is running, as this will change the underlying state.
    //
    //     //從場景A 走到場景Ｂ 再走回場景Ａ SaveID 會變。 所以Application.IsPlaying 的狀態下不能做這件事。
    //     
    // }

    // public override void OnAfterDeserialize()
    // {
    //     base.OnAfterDeserialize();
    // }
    //TODO: 找到旁邊class裡的[GameState], 幫他gen掉 

// #endif
    public void OnBeforeSceneSave()
    {
#if UNITY_EDITOR   
        AutoGenCheck();
#endif
    }
}

//TODO: 要直接用dictionary access嗎？unique id怎麼來？c?