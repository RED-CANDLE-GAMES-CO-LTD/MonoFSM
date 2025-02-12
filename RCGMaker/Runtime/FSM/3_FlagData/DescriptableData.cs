using System;
using System.Collections.Generic;
using System.Reflection;
using I2.Loc;
using mixpanel;
using RCGMaker.AddressableAssets;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Item_BuildSystem;
using RCGMaker.Runtime.Mono;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;


[System.Serializable]
public class Descriptable
{
    // [SerializeField]
    // string title;
    public LocalizedString titleStr;
    [SerializeField] [TextArea(2, 10)] string description;
    string summary;
    public LocalizedString descriptionStr;

    public LocalizedString typeStr;
    public LocalizedString summaryStr;
}

public interface IToggleable
{
    bool IsActivated { get; }

    bool UnEquipCheck();
    bool EquipCheck(bool force = false);
}

public interface IMonoDescriptableCollection : IValueOfKey<MonoDescriptableTag>
{
    public IList<IMonoDescriptable> MonoDescriptableList { get; }

    bool isActiveAndEnabled { get; }
    // public MonoDescriptableTag Tag { get; }
}

public interface IMonoDescriptable : IValueOfKey<MonoDescriptableTag>
{
    public IDescriptableData Descriptable { get; }

    // public void OnUIEventReceived(); //FIXME: 之後可以加參數？  ??
    // public IFloatValue GetFloatValue(VariableTypeTag tag);
    // public VariableBool GetBoolValue(VariableTypeTag tag);
    // public VariableString GetStringValue(VariableTypeTag tag);
    // public VariableInt GetIntValue(VariableTypeTag tag);
}

public interface IDescriptableData : IProperty
{
    string Title { get; }
    string Description { get; }
    string Summary { get; }
    Sprite FullSprite { get; }
    Sprite SmallIcon { get; }
    bool IsRevealed { get; } //UI看得到 => 技能樹上看得到，但還沒拿到
    bool IsAcquired { get; } //在身上了
    string ItemType { get; }
    void LoadAndSetIconForImage(Image image, Color loadedColor = default);
    void LoadAndSetSpriteForImage(Image image, Color loadedColor = default);
}

public interface IProperty
{
    Func<IDescriptableData, object> GetPropertyCache(string knownFieldName);
    object GetProperty(string knownFieldName);
    ValueDropdownList<string> GetProperties(List<Type> supportedTypes);
    ValueDropdownList<string> GetProperties<T>();
}

public interface IItem : IDescriptableData
{
    public int SlotStackCount { get; }
    void Use();
}

//Static資料，描述一個/種 東西的性質
//ConfigData?
//GameData?
[CreateAssetMenu(fileName = "Descriptable", menuName = "ScriptableObjects/Descriptable", order = 1)]
[Searchable]
public class DescriptableData : GameFlagBase, IDescriptableData, IMonoDescriptable //以前是GameFlagDescriptable
{
    public async void PreloadSprite()
    {
        if (SpriteRef == null) return;
        await SpriteRef.GetAssetAsync<Sprite>();
    }

    public void ReleaseSprite()
    {
        // Debug.Log("ReleaseSprite", this);
        SpriteRef?.Release();
    }

    //一個propertyName會對應到一個Getter Func, 輸入是IDescriptableData, 輸出是object
    //nested應該不行...
    public Dictionary<string, Func<IDescriptableData, object>> propertyCache = new();

    public Func<IDescriptableData, object> GetPropertyCache(
        string propertyName)
    {
        if (propertyCache.TryGetValue(propertyName, out var info))
            return info;


        var propertyInfo = GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        // Debug.Log($"Property {propertyName} found in {sourceObject.GetType()}", sourceObject);

        if (propertyInfo == null)
        {
            propertyCache[propertyName] = null;
            //FIXME: 可能因為unknownData所以有可能會找不到 有點危險？
            // Debug.LogError($"Property {propertyName} not found in {GetType()}");
            return null;
        }

        var getMethod = propertyInfo.GetGetMethod();
        if (getMethod == null)
        {
            Debug.LogError($"Property {propertyName} does not have a getter in {GetType()}"
            );
            return null;
        }

        Func<IDescriptableData, object>
            _getMyProperty = (source) => getMethod.Invoke(source, null);
        propertyCache[propertyName] = _getMyProperty;
        return _getMyProperty;
    }

    public object GetProperty(string knownFieldName)
    {
        return GetPropertyCache(knownFieldName)?.Invoke(this);
    }

    public ValueDropdownList<string> GetProperties(List<Type> supportedTypes)
    {
        return GetProperties(supportedTypes, this);
    }

    public ValueDropdownList<string> GetProperties<T>()
    {
        return GetProperties<T>(GetType());
    }

    public static ValueDropdownList<string> GetProperties<T>(Type dataType)
    {
        // var fields = new List<string>();
        var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dropdownList = new ValueDropdownList<string>();
        foreach (var property in properties)
        {
            if (!typeof(T).IsAssignableFrom(property.PropertyType))
                continue;
            // fields.Add(property.Name);
            dropdownList.Add(property.Name + " (" + property.PropertyType.Name + ")", property.Name);
        }

        return dropdownList;
    }

    public static ValueDropdownList<string> GetProperties(List<System.Type> supportedTypes, DescriptableData sampleData)
    {
        // AppDomain.CurrentDomain.GetAssemblies().
        var type = sampleData.GetType();
        // Debug.Log(type);
        var fields = new List<string>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dropdownList = new ValueDropdownList<string>();
        foreach (var property in properties)
        {
            if (!supportedTypes.Contains(property.PropertyType))
                continue;
            fields.Add(property.Name);
            dropdownList.Add(property.Name + " (" + property.PropertyType.Name + ")", property.Name);
        }

        return dropdownList;
    }

    public void CopyFrom(DescriptableData source)
    {
#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(this, "CopyValue");
        EditorUtility.CopySerializedManagedFieldsOnly(source, this);
#endif
    }

    //類別，需要的自己用enum override掉
    public virtual int category => 0;
    [PreviewInInspector] public virtual PoolObject bindPrefab => null; //FIXME: 要弄這個？
    public FlagFieldBool unlocked; //在介面中可以看到的狀態，但可能還沒取得
    public virtual bool IsRevealed => unlocked.CurrentValue;
    [FormerlySerializedAs("aquired")] public FlagFieldBool acquired; //取得

    public virtual bool IsAcquired
    {
        get => acquired.CurrentValue;
        set => acquired.CurrentValue = value;
    }

    public virtual bool IsSelectableConditionValid => true;

    [Header("當有從按鈕上的bindInstance來顯示過這個物件的資訊，就當作看過了把通知移除")]
    public bool IsUpdateDescriptionAsViewed = true;

    public FlagFieldBool viewed; //玩家有沒有看過
    public FlagFieldBool promptViewed; //玩家有沒有看過
    public bool IsViewed => viewed.CurrentValue && acquired.CurrentValue;
    public bool IsNew => IsAcquired && !IsViewed;

    public bool SetViewed(Object byWho = null) //應該要用這個，然後把viewed改成private
    {
        if (!acquired.CurrentValue)
            return false;
        viewed.SetCurrentValue(true, byWho);
        return true;
    }

    //
    public bool IsImportantObject = false;


    // public bool isViewed => viewed.CurrentValue;

// [HideInInspector]
    // public string RawTitle => title;

    // [SerializeField]
    // string title;
    public LocalizedString titleStr;

    [SerializeField] [TextArea(2, 10)]
    // [HideInInspector]
    string description;

    string summary;
    public LocalizedString descriptionStr;
    public LocalizedString typeStr;
    public LocalizedString summaryStr;
    public virtual string ItemType => typeStr;

    public virtual string Title => titleStr.ToString();

    public virtual string Description =>
        descriptionStr.ToString().Length > 0 ? descriptionStr.ToString() : this.description;

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


    //ref到so就會load sprite
    [Header("一開遊戲就會讀進來的")] public Sprite staticSprite;
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

        //不用清掉前一個 才不會閃白 讀取其實很快。
        //image.sprite = null;
        if (rcgAssetRef.IsAssetLoaded)
        {
            var newSprite = rcgAssetRef.GetAsset<Sprite>();
            if (image.sprite == newSprite)
            {
                // Debug.Log("AssignToUIImage loaded same" + rcgAssetRef, this);
                return;
            }

            image.color = loadedColor == default ? Color.white : loadedColor;
            // Debug.Log("AssignToUIImage already loaded:" + rcgAssetRef, this);
            image.sprite = newSprite;
        }
        else
        {
            //FIXME: 可能會被animation key
            image.color = Color.clear;
            image.sprite = UIAssetConfig.i.EmptySprite;
            //還沒load好...
            //還是要用什麼方式先load好？
            //clear沒有用XDD因為動畫key到就暴雷了...要empty sprite才行
            // Debug.Log("AssignToUIImage:" + rcgAssetRef, this);
            // Debug.Log("AssignToUIImage:" + image, image);
            var loadedSprite = await rcgAssetRef.GetAssetAsync<Sprite>();
            image.color = loadedColor == default ? Color.white : loadedColor;
            image.sprite = loadedSprite;
        }
    }

    [PreviewInInspector]
    [PreviewField(100)]
    public virtual Sprite FullSprite => staticSprite ? staticSprite : spriteRef.GetAsset<Sprite>();

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
#if MIXPANEL
        _trackValue.OnRecycle();
        _trackValue.Add("name", name);
        _trackValue.Add("itemName", titleStr.ToString());
        _trackValue.Add("type", GetType().Name);
        this.Track("GameFlagDescriptable Acquired", _trackValue);
#endif
    }
#if MIXPANEL
    private readonly Value _trackValue = new();
#endif

    //FIXME: 亂寫看看
    public MonoDescriptableTag Key { get; }
    public IDescriptableData Descriptable => this;

    public void OnUIEventReceived()
    {
        //throw new NotImplementedException();
    }
}