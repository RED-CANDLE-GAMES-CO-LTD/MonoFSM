// using I2.Loc;
// using mixpanel;

using System;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.AddressableAssets;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Localization;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable.FieldReference;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

// [System.Serializable]
// public class Descriptable
// {
//     // [SerializeField]
//     // string title;
//     public LocalizedString titleStr;
//     [SerializeField] [TextArea(2, 10)] string description;
//     string summary;
//     public LocalizedString descriptionStr;
//
//     public LocalizedString typeStr;
//     public LocalizedString summaryStr;
// }

public interface IToggleable
{
    bool IsActivated { get; }

    bool UnEquipCheck();
    bool EquipCheck(bool force = false);
}

public interface IMonoDescriptableCollection : IValueOfKey<MonoEntityTag>
{
    public IList<IMonoDescriptable> MonoDescriptableList { get; }

    bool isActiveAndEnabled { get; }
    // public MonoDescriptableTag Tag { get; }
}

public interface IMonoDescriptable : IValueOfKey<MonoEntityTag>
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

//hmm
public interface IItemData : IDataFeature
{
    public float MaxStackCount { get; }
    void Use();
}

//Static資料，描述一個/種 東西的性質
//ConfigData?
//GameData?
//用has來額外加功能？ ListOfDataFunction? pickableData?
public interface IDataFeature
{
    GameData Owner { get; }
    void SetOwner(GameData owner);
}

public static class GameDataUtility
{
#if UNITY_EDITOR
    public static GameFlagBase CreateGameStateSO(
        this Type type,
        MonoBehaviour refObj,
        string subFolderName = ""
    )
    {
        //遊戲中不該建state
        if (Application.isPlaying)
            return null;
        if (!refObj.TryGetComponent<AutoGenGameState>(out var autoGenGameState))
        {
            //不是自動生的
            var gameStateSo =
                AssetDatabaseUtility.CreateScriptableObject(
                    type,
                    GameStateAttribute.GetFullPath(refObj.gameObject, subFolderName)
                ) as GameFlagBase;
            if (gameStateSo == null)
            {
                Debug.LogError("Create Scriptable Object Failed", refObj);
                return null;
            }

            return gameStateSo;
        }
        else
        {
            var folderRelativePath = GameStateAttribute.GetRelativePath(
                refObj.gameObject,
                subFolderName,
                true
            );
            var fileName =
                GameStateAttribute.GetFileName(refObj.gameObject)
                + autoGenGameState.MyGuid
                + ".asset";
            var gameStateSo =
                AssetDatabaseUtility.CreateScriptableObject(
                    type,
                    folderRelativePath + "/" + fileName
                ) as GameFlagBase;

            //自動生成的，SaveID另外做
            if (gameStateSo != null)
            {
                gameStateSo.gameStateType = GameFlagBase.GameStateType.AutoUnique;
                gameStateSo.SetSaveID(autoGenGameState.SaveID);
                ;
                Debug.Log("Assign SaveID for autoGen", refObj);

                return gameStateSo;
            }

            Debug.LogError("Create gameStateSo Auto Object Failed", refObj);
            return null;
        }
    }
#endif
}

[CreateAssetMenu(fileName = "Descriptable", menuName = "ScriptableObjects/Descriptable", order = 1)]
[Searchable]
[FormerlyNamedAs("DescriptableData")]
public class GameData
    : GameFlagBase,
        IDescriptableData,
        IMonoDescriptable,
        ISceneSavingCallbackReceiver,
        IFieldPathRootTypeProvider, IItemData
{
    [FormerlySerializedAs("descriptableTag")]
    public MonoEntityTag _entityTag;

    // [SerializeReference]
    // private AbstractDataFunction[] _dataFunctionsArray;

    //FIXME: 還是用 AbstractDataFunction 比較好？
    [OnCollectionChanged(nameof(RebuildDataFunctionDict))] [SerializeReference]
    public List<AbstractDataFunction> _dataFunctionList = new();

    [OnCollectionChanged(nameof(RebuildDataFunctionDict))] [SerializeReference]
    //empty
    private AbstractDataFunction[] _dataFunctions = Array.Empty<AbstractDataFunction>();

    private readonly Dictionary<Type, AbstractDataFunction> _dataFunctionDict = new();
    [ShowInDebugMode] public int dataFunctionCount => _dataFunctionDict.Count;

    [ShowInDebugMode]
    bool IsRebuildNeeded => _dataFunctions.Length != _dataFunctionDict.Count;


    // private readonly HashSet<IDataFunction> _hashSet = new();

    public T GetDataFunction<T>()
        where T : class, IDataFeature
    {

        if (_dataFunctionDict.TryGetValue(typeof(T), out var dataFunction))
            return dataFunction as T;

        Debug.LogError($"Data function of type {typeof(T)} not found in {name}", this);
        return null;
    }

    public override void FlagAwake(TestMode mode)
    {
        base.FlagAwake(mode);
        RebuildDataFunctionDict();
    }

    void RebuildDataFunctionCheck()
    {
        if (IsRebuildNeeded)
            RebuildDataFunctionDict();
    }

    private void RebuildDataFunctionDict()
    {
        // return;
        if (_dataFunctions == null && _dataFunctionList == null) //如果onchange清掉咧？
            return;
        _dataFunctionDict.Clear();
        // Debug.Log($"Rebuilding data function dict for {name}", this);
        if (_dataFunctions != null && _dataFunctions.Length > 0)
            foreach (var dataFunction in _dataFunctions)
            {
                if (dataFunction == null)
                {
                    Debug.LogError("DataFunction is null in " + name, this);
                    continue;
                }

                dataFunction.SetOwner(this);
                var type = dataFunction.GetType();
                if (!_dataFunctionDict.TryAdd(type, dataFunction))
                    Debug.LogError($"Duplicate data function of type {type} found in {name}", this);
            }

        if (_dataFunctionList != null && _dataFunctionList.Count > 0)
            foreach (var dataFunction in _dataFunctionList)
            {
                if (dataFunction == null)
                {
                    Debug.LogError("DataFunction is null in " + name, this);
                    continue;
                }

                dataFunction.SetOwner(this);
                var type = dataFunction.GetType();
                if (!_dataFunctionDict.TryAdd(type, dataFunction))
                    Debug.LogError($"Duplicate data function of type {type} found in {name}", this);
            }
    }

    public async void PreloadSprite()
    {
        if (SpriteRef == null)
            return;
        await SpriteRef.GetAssetAsync<Sprite>();
    }

    public void ReleaseSprite()
    {
        // Debug.Log("ReleaseSprite", this);
        SpriteRef?.Release();
    }

    //一個propertyName會對應到一個Getter Func, 輸入是IDescriptableData, 輸出是object
    //nested應該不行...
    public readonly Dictionary<string, Func<IDescriptableData, object>> _propertyCache = new();

    public Func<IDescriptableData, object> GetPropertyCache(string propertyName)
    {
        if (_propertyCache.TryGetValue(propertyName, out var info))
            return info;

        var propertyInfo = GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        // Debug.Log($"Property {propertyName} found in {sourceObject.GetType()}", sourceObject);

        if (propertyInfo == null)
        {
            _propertyCache[propertyName] = null;
            //FIXME: 可能因為unknownData所以有可能會找不到 有點危險？
            // Debug.LogError($"Property {propertyName} not found in {GetType()}");
            return null;
        }

        var getMethod = propertyInfo.GetGetMethod();
        if (getMethod == null)
        {
            Debug.LogError($"Property {propertyName} does not have a getter in {GetType()}");
            return null;
        }

        Func<IDescriptableData, object> _getMyProperty = (source) => getMethod.Invoke(source, null);
        _propertyCache[propertyName] = _getMyProperty;
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
            dropdownList.Add(
                property.Name + " (" + property.PropertyType.Name + ")",
                property.Name
            );
        }

        return dropdownList;
    }

    public static ValueDropdownList<string> GetProperties(
        List<Type> supportedTypes,
        GameData sampleData
    )
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
            dropdownList.Add(
                property.Name + " (" + property.PropertyType.Name + ")",
                property.Name
            );
        }

        return dropdownList;
    }

#if UNITY_EDITOR
    [ShowInInspector]
    [BoxGroup("CopyFrom")]
    private GameData toCopySource;

    //FIXME: 改成升級成不同type？meta id保留
    [BoxGroup("CopyFrom")]
    [Button]
    private void CopyFrom()
    {
        var source = toCopySource;
        Undo.RegisterCompleteObjectUndo(this, "CopyValue");
        EditorUtility.CopySerializedManagedFieldsOnly(source, this);
    }
#endif

    //類別，需要的自己用enum override掉
    public virtual int category => 0;

    [PreviewInInspector]
    public virtual MonoObj bindPrefab
    {
        get
        {
            if (!Application.isPlaying)
                RebuildDataFunctionCheck();

            if (_dataFunctionDict.TryGetValue(typeof(PickableData), out var dataFunction))
                return ((PickableData)dataFunction).EntityPrefab;
            // Debug.LogError("No PickableData found in " + name, this);
            return null;
        }
    } //FIXME: 要弄這個？

    //bind MonoEntityTag?

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

    //FIXME: 這個用到I2, 有點悲劇
    //FIXME: 以前這四個都是localized string
    public LocalizedString titleStr; //FIXME: 應該用interface？但這樣怎麼用別人的...從主專案再接過去嗎
    public LocalizedString descriptionStr;
    public LocalizedString typeStr;
    public LocalizedString summaryStr;

    [SerializeField]
    [TextArea(2, 10)]
    // [HideInInspector]
    private string description;

    private string summary;

    public virtual string ItemType => typeStr;

    //description attribute?

    [PreviewInInspector]
    public virtual string Title => titleStr.ToString();

    public virtual string Description =>
        descriptionStr.ToString().Length > 0 ? descriptionStr.ToString() : description;

    public virtual string Summary =>
        summaryStr.ToString().Length > 0 ? summaryStr.ToString() : summary;

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
    [Header("一開遊戲就會讀進來的")]
    public Sprite staticSprite;

    [InlineField]
    [SerializeField]
    public RCGAssetReference spriteRef;

    [InlineField]
    [SerializeField]
    public RCGAssetReference smallSpriteRef;

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
    protected async void AssignToUIImage(
        Image image,
        RCGAssetReference rcgAssetRef,
        Color loadedColor = default
    )
    {
        if (image == null)
        {
            //沒有image, 單純load圖
            var result = await rcgAssetRef.GetAssetAsync<Sprite>();
            if (result == null)
                Debug.LogError("AssignToUIImage: rcgAssetRef = null", this);

            return;
        }

        //不用清掉前一個 才不會閃白 讀取其實很快。
        //image.sprite = null;
        if (rcgAssetRef.IsAssetLoaded)
        {
            var newSprite = rcgAssetRef.GetAsset<Sprite>();
            if (image.sprite == newSprite)
                // Debug.Log("AssignToUIImage loaded same" + rcgAssetRef, this);
                return;

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
    public virtual Sprite FullSprite => staticSprite ? staticSprite : spriteRef?.GetAsset<Sprite>();

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
    public MonoEntityTag Key { get; }

    public MonoEntityTag[] GetKeys()
    {
        return new[] { _entityTag };
    }

    public IDescriptableData Descriptable => this;

    // public void OnUIEventReceived()
    // {
    //     //throw new NotImplementedException();
    // }

    public override void OnBeforeSceneSave()
    {
        RebuildDataFunctionDict();
        //FIXME:
        //自動改名、validation之類的
        // name = name.Replace("[", "(").Replace("]", ")");
    }

    public Type GetFieldPathRootType()
    {
        return typeof(GameData);
    }

    public GameData Owner => this;

    public void SetOwner(GameData owner)
    {
    }

    //TODO: 直接把數字寫在這？
    public float MaxStackCount => 10;

    public void Use()
    {
        //外包 Scriptable Handler?
    }
}
