using System;
using System.Collections;
using System.Collections.Generic;
using I2.Loc;
using mixpanel;
using RCGMaker.AddressableAssets;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;


[System.Serializable]
public class Descriptable
{
    // [SerializeField]
    // string title;
    public LocalizedString titleStr;
    [SerializeField]
    [TextArea(2, 10)]
    string description;
    string summary;
    public LocalizedString descriptionStr;

    public LocalizedString typeStr;
    public LocalizedString summaryStr;
}

public interface IToggleable
{
    bool IsActivated
    {
        get;
    }

    bool UnEquipCheck();
    bool EquipCheck(bool force = false);
}
public interface IDescriptable
{
    string Title { get; }
    string Description { get; }
    string Summary { get; }
    Sprite Image { get; }
    Sprite SmallIcon { get; }
    bool IsRevealed { get; } //UI看得到 => 技能樹上看得到，但還沒拿到
    bool IsAcquired { get; } //在身上了
    string ItemType { get; }
    void LoadAndSetIconForImage(Image image, Color loadedColor = default);
    void LoadAndSetSpriteForImage(Image image, Color loadedColor = default);
}

// public class GameDataModifier{
//     
// }
//TODO: IDescriptable??
[CreateAssetMenu(fileName = "Descriptable", menuName = "ScriptableObjects/Descriptable", order = 1)]
[Searchable]
public class GameFlagDescriptable : GameFlagBase, IDescriptable
{
    // List<GameDataModifier> modifiers = new List<GameDataModifier>();
    // public bool IsShowInfoOnlyAquired = true;

    public void CopyFrom(GameFlagDescriptable source)
    {
#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(this, "CopyValue");
        EditorUtility.CopySerializedManagedFieldsOnly(source, this);
#endif
    }
    
    //類別，需要的自己用enum override掉
    public virtual int category => 0;
    public virtual PoolObject bindObject => null;
    public FlagFieldBool unlocked; //在介面中可以看到的狀態，但可能還沒取得
    public virtual bool IsRevealed => unlocked.CurrentValue;
    [FormerlySerializedAs("aquired")]
    public FlagFieldBool acquired; //取得

    public virtual bool IsAcquired
    {
        get => acquired.CurrentValue;
        set => acquired.CurrentValue = value;
    }

    public virtual bool IsSelectableConditionValid => true;
    public FlagFieldBool viewed;
    
    //
    public bool IsImportantObject = false;
    
    
    // public bool isViewed => viewed.CurrentValue;

// [HideInInspector]
    // public string RawTitle => title;

    // [SerializeField]
    // string title;
    public LocalizedString titleStr;
    [SerializeField]
    [TextArea(2, 10)]
    // [HideInInspector]
    string description;
    string summary;
    public LocalizedString descriptionStr;

    public LocalizedString typeStr;
    public LocalizedString summaryStr;
    public virtual string ItemType => typeStr;

    public virtual string Title => titleStr.ToString();
    public virtual string Description => descriptionStr.ToString().Length > 0 ? descriptionStr.ToString() : this.description;
    public virtual string Summary => summaryStr.ToString().Length > 0 ? summaryStr.ToString() : this.summary;

    // [DisableIf("@true")]
    // [SerializeField]
    // Sprite sprite;
    //
    // [DisableIf("@true")]
    // [SerializeField]
    // Sprite smallSprite;

    //FIXME: 舊規應該可以砍了？
    // [DisableIf("@true")]
    // [SerializeField] private AssetReferenceSprite spriteRefSprite;
    //
    // [DisableIf("@true")]
    // [SerializeField] private AssetReferenceSprite smallSpriteRefSprite;

    [InlineField] [SerializeField] public RCGAssetReference spriteRef;

    [InlineField] [SerializeField] public RCGAssetReference smallSpriteRef;

    public virtual void LoadAndSetIconForImage(Image image, Color loadedColor = default)
    {
        if (!smallSpriteRef.IsRuntimeKeyValid)
        {
            // Debug.LogError("smallSpriteRef.assetReference.RuntimeKeyIsValid() == false");
            //FIXME:沒有的要挑出來？還是就fallback
            AssignToUIImage(image, spriteRef, loadedColor);
            return;
        }

        AssignToUIImage(image, smallSpriteRef, loadedColor);
    }

    public virtual void LoadAndSetSpriteForImage(Image image, Color loadedColor = default)
    {
        AssignToUIImage(image, spriteRef, loadedColor);
    }

//FIXME: 這個是不是太越權
    protected async void AssignToUIImage(Image image, RCGAssetReference rcgAssetRef, Color loadedColor = default)
    {
        if (image == null)
        {
            //沒有image, 單純load圖
            var result = await rcgAssetRef.GetAssetAsync<Sprite>();
            if (result == null)
            {
                Debug.LogError("AssignToUIImage: rcgAssetRef = null", this);
            }
            return;
        }

        image.color = Color.clear;
        //還沒load好...
        //還是要用什麼方式先load好？
        //clear沒有用XDD因為動畫key到就暴雷了...要empty sprite才行
        
        //不用清掉前一個 才不會閃白 讀取其實很快。
        //image.sprite = null;
        if (rcgAssetRef.IsAssetLoaded)
        {
            image.color = loadedColor == default ? Color.white : loadedColor;
            image.sprite = rcgAssetRef.GetAsset<Sprite>();
        }
        else 
        {
            Debug.Log("AssignToUIImage:" + rcgAssetRef, this);
            Debug.Log("AssignToUIImage:" + image, image);
            var loadedSprite = await rcgAssetRef.GetAssetAsync<Sprite>();
            image.color = loadedColor == default ? Color.white : loadedColor;
            image.sprite = loadedSprite;
        }
    }

    public virtual Sprite Image => spriteRef.GetAsset<Sprite>();
    public virtual Sprite SmallIcon => IconSpriteRef.GetAsset<Sprite>();
    public virtual RCGAssetReference SpriteRef => spriteRef;

    public virtual RCGAssetReference IconSpriteRef => spriteRef;

    //FIXME: Deprecated 要dynamic load
 

#if UNITY_EDITOR
    [Button]
    public void FixAddressable()
    {
        // var settings = AddressableAssetSettingsDefaultObject.Settings;
        spriteRef.CreateAssetReference();
        smallSpriteRef.CreateAssetReference();
    }
    // [Button]
    // public void UpgradeSpriteToAddressable()
    // {
    //     var settings = AddressableAssetSettingsDefaultObject.Settings;
    //     // var path = AssetDatabase.GetAssetPath(this);
    //     // var guid = AssetDatabase.AssetPathToGUID(path);
    //     // var assetRef = settings.CreateAssetReference(guid);
    //     // if (name.Contains("[") || name.Contains("]"))
    //     // {
    //     //     settings.FindAssetEntry(guid).address = path.Replace("[", "(").Replace("]", ")");
    //     // }
    //
    //     //
    //     //
    //     //
    //     //
    //     try
    //     {
    //         if (sprite)
    //         {
    //             AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out var guid1, out long localId);
    //             var asset = settings.CreateAssetReference(guid1);
    //
    //             spriteRefSprite = new AssetReferenceSprite(guid1);
    //             spriteRefSprite.SetEditorSubObject(sprite);
    //
    //             sprite = null;
    //         }
    //
    //         if (smallSprite)
    //         {
    //             AssetDatabase.TryGetGUIDAndLocalFileIdentifier(smallSprite, out var guid2, out long localId);
    //             var asset2 = settings.CreateAssetReference(guid2);
    //             smallSpriteRefSprite = new AssetReferenceSprite(guid2);
    //             smallSpriteRefSprite.SetEditorSubObject(smallSprite);
    //             smallSprite = null;
    //         }
    //
    //
    //         //最新規，用自己的wrapper
    //         if (spriteRefSprite != null)
    //         {
    //             // spriteRef.assetReference = spriteRefSprite;
    //             spriteRef.editorAsset = spriteRefSprite.editorAsset;
    //         }
    //
    //         if (smallSpriteRefSprite != null)
    //         {
    //             // smallSpriteRef.assetReference = smallSpriteRefSprite;
    //             smallSpriteRef.editorAsset = smallSpriteRefSprite.editorAsset;
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.LogError(e, this);
    //         throw;
    //     }
    //
    //     FixNameCheck();
    //     //
    //     EditorUtility.SetDirty(this);
    // }

    private void FixNameCheck()
    {
        //if name contains '[' ']', change to '(' ')'
        var name = this.name;
        if (name.Contains("[") || name.Contains("]"))
        {
            name = name.Replace("[", "(");
            name = name.Replace("]", ")");
        }

        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(this), name);
        Debug.Log("Rename to " + name);
    }

#endif


    public virtual void PlayerPicked()
    {
        // spriteRefSprite.LoadAssetAsync<Sprite>().Completed += handle =>
        // {
        //     sprite = handle.Result;
        // };
        unlocked.CurrentValue = true;
        acquired.CurrentValue = true;
        _trackValue.OnRecycle();
        _trackValue.Add("name", name);
        _trackValue.Add("itemName", titleStr.ToString());
        _trackValue.Add("type", GetType().Name);
        this.Track("GameFlagDescriptable Acquired", _trackValue);
    }

    private readonly Value _trackValue = new();
}
