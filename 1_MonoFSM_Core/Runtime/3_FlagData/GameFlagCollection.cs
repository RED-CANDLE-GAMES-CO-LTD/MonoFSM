using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
//所有的GameFlag, 存檔用
[CreateAssetMenu(fileName = "GameFlagCollection", menuName = "System/FlagCollection", order = 1)]
[Serializable]
[Searchable]
public class GameFlagCollection : ScriptableObject, ISelfValidator
{
#if UNITY_EDITOR 
    [Button("Clear")]
    public void Clear() //這個不太對
    {
        Flags.Clear();
        EditorUtility.SetDirty(this);
    }

    // [Button]
    // void UpgradeForAllGameFlagDescriptable()
    // {
    //     // AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
    //     // settings.CreateAssetReference(flag);
    //     EditorUtility.DisplayCancelableProgressBar("UpgradeForAllGameFlagDescriptable", "UpgradeForAllGameFlagDescriptable", 0);
    //     
    //     float progress = 0;
    //     foreach (var flag in Flags)
    //     {
    //         progress += 1f / Flags.Count;
    //         if (EditorUtility.DisplayCancelableProgressBar("UpgradeForAllGameFlagDescriptable",
    //                 flag.name, progress))
    //         {
    //             break;
    //         }
    //
    //         if (flag is DescriptableData descriptable)
    //         {
    //             // var descriptable = flag as GameFlagDescriptable;
    //             // descriptable.UpgradeSpriteToAddressable();
    //         }
    //         // else
    //         //     FixNameCheck(flag);
    //     }
    //
    //     EditorUtility.ClearProgressBar();
    // }
    private void FixNameCheck(ScriptableObject obj)
    {
        //if name contains '[' ']', change to '(' ')'
        var name = obj.name;
        if (name.Contains("[") || name.Contains("]"))
        {
            name = name.Replace("[", "(");
            name = name.Replace("]", ")");
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), name);
            EditorUtility.SetDirty(obj);
        }


        
    }

    [Button]
    private void FixAddressableSprite()
    {
        foreach (var flag in Flags)
        {
            if (flag is DescriptableData descriptable)
            {
                // var descriptable = flag as GameFlagDescriptable;
                // descriptable.UpgradeSpriteToAddressable();
                descriptable.FixAddressable();
            }
        }
    }

    [Button("FindAllInAsset")]
    public void FindAllInAsset()
    {
        Debug.Log("Find Scriptables:" + typeof(GameFlagBase).FullName);

        var allProjectFlags = AssetDatabase.FindAssets("t:" + typeof(GameFlagBase).FullName);
        Debug.Log("allProjectFlags:" + allProjectFlags.Length);
        //find not in Flags

        for (var i = 0; i < allProjectFlags.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(allProjectFlags[i]);
            var flag = AssetDatabase.LoadAssetAtPath<GameFlagBase>(path);
            if (!Flags.Contains(flag))
            {
                Debug.LogError("Not in Flags:" + flag.name, flag);
            }
        }
    }

    [Button]
    public void FindAllFlags()
    {
        Flags.Clear();
        // gameFlagDataList.Clear();
        // Debug.Log("Find GameFlag:" + typeof(T).FullName);
        var myPath = AssetDatabase.GetAssetPath(this);
        // Debug.Log("Mypath" + name + ":" + myPath);
        var dirPath = System.IO.Path.GetDirectoryName(myPath);
        var allProjectFlags = AssetDatabase.FindAssets("t:GameFlagBase", new[] { dirPath });
        //All 10_Flags
        // string[] allProjectFlags = AssetDatabase.FindAssets("t:GameFlagBase", new[] { "Assets/10_Flags" });
        for (int i = 0; i < allProjectFlags.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(allProjectFlags[i]);
            var flag = AssetDatabase.LoadAssetAtPath<GameFlagBase>(path);
            Flags.Add(flag);

        }
        EditorUtility.SetDirty(this);
    }
#endif

// #if UNITY_EDITOR
//     [NonSerialized] [ShowInInspector]
// #endif
    public List<GameFlagBase> Flags = new();

    public Dictionary<string, GameFlagBase> flagDict = new();


    public Dictionary<string, GameFlagBase> FlagDict
    {
        get
        {
            if (flagDict.Count == 0)
            {
                flagDict.Clear();
                foreach (var flag in Flags)
                {
                    if (flag.GetSaveID == "")
                    {
                        continue;
                    }

                    if (flagDict.TryGetValue(flag.FinalSaveID, out var value))
                    {
                        Debug.LogError(value.name + " saveID conflict with " + flag.name, value);
                        Debug.LogError(value.name + " saveID conflict with " + flag.name, flag);
                    }
                    else
                        flagDict.Add(flag.FinalSaveID, flag);
                }
            }

            return flagDict;
        }
    }

    public void AllFlagAwake(TestMode mode) //init  //TODO: 不該用傳的，static或是怎麼弄
    {
#if UNITY_EDITOR
        //註解掉這個 Editor Time 新增的Flags 都會錯....
        // FindAllFlags();//從resource撈出所有的flag
#endif
        flagDict.Clear();
        foreach (var flag in Flags)
        {
            flag.FlagAwake(mode);
            if (flag.GetSaveID == "")
            {
                continue;
            }

            if (flagDict.TryGetValue(flag.FinalSaveID, out var value))
            {
                Debug.LogError(value.name + " saveID conflict with " + flag.name, flag);
            }
            else
                flagDict.Add(flag.FinalSaveID, flag);
        }
        // Debug.Log("All Game Flag Loaded" + name);
    }
// [text](https://youtrack.jetbrains.com/)
    public void Reset()
    {
        // foreach (var flag in Flags)
        // {
        //     if (flag == null)
        //     {
        //         Debug.LogError("FlagsToReset[i]==null WTF?");
        //         continue;
        //     }
        //
        //     flag.Reset();
        // }AllFlagAwake(TestMode.)
    }

    public void AllFlagInitStartAndEquip()
    {
        foreach (var flag in Flags)
            try
            {
                flag.FlagInitStart();
            }
            catch (Exception e)
            {
                Debug.LogError("Flag Init 失敗!" + flag.name, flag);
                Debug.LogError(e);
            }

        foreach (var flag in Flags)
        {
            try
            {
                flag.FlagEquipCheck();
            }
            catch (Exception e)
            {
                Debug.LogError("Flag Equip Check 失敗!" + flag.name, flag);
                Debug.LogError(e);
            }
        }
    }

    private void OnValidate()
    {
        Flags.RemoveAll((a) => a == null);
    }

    // public void Validate(SelfValidationResult result)
    // {
    //     FindAllFlags();
    // }
    public void Validate(SelfValidationResult result)
    {
#if UNITY_EDITOR
        //check if flags in the list are equal to the flags in the folder
        var myPath = AssetDatabase.GetAssetPath(this);
        // FindAllFlags();
        var allProjectFlags =
            AssetDatabase.FindAssets("t:GameFlagBase", new[] { System.IO.Path.GetDirectoryName(myPath) });
        // Debug.Log("allProjectFlags:" + allProjectFlags.Length);
        //find not in Flags
        for (var i = 0; i < allProjectFlags.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(allProjectFlags[i]);
            var flag = AssetDatabase.LoadAssetAtPath<GameFlagBase>(path);
            if (!Flags.Contains(flag))
            {
                result.AddError("Not in Flags:" + flag.name).WithFix(() =>
                {
                    Flags.Add(flag);
                    EditorUtility.SetDirty(this);
                });
            }
        }
#endif
    }
}
