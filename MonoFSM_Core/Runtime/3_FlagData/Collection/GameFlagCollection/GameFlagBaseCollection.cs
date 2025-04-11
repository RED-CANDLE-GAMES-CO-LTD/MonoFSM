using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public abstract class GameFlagBaseCollection<T> : AbstractGameFlagCollection where T : DescriptableData
{
    protected virtual bool isValidLastSavedFlag(DescriptableData f)
    {
        return true;
    }

    public void PickUpAllContents()
    {
        foreach (var content in gameFlagDataList)
            if (content != null)
                content.PlayerPicked();
    }
    
    public override void FlagAwake(TestMode mode)
    {
        // Debug.Log("GameFlagBaseCollection FlagAwake "+this.name);
        gameFlagDataList.RemoveAll((n) => n == null);
        base.FlagAwake(mode);
        
    }
    
    public override void FlagInitStart()
    {
        base.FlagInitStart();
        BuildSaveIDToIndex();
    }
    
    

    // public GameFlagInt indexFlag; //要存可以用這個
    public override List<DescriptableData> rawCollection => gameFlagDataList.ToList<DescriptableData>();
    public override DescriptableData currentItem => gameFlagDataList[currentIndex];


    protected virtual bool FlagBelongThisCollection(T t)//用一些條件去篩掉特定flag, 例如要做百科分類
    {
        return true;
    }
#if UNITY_EDITOR //TODO: find Asset 很貴嗎？
    [Button("Clear")]
    public void Clear()
    {
        gameFlagDataList.Clear();
        EditorUtility.SetDirty(this);
    }
#if UNITY_EDITOR
    [Button("Find Under Folder With OverrideTypeName")]
    public void FindUnderFolder()
    {
        gameFlagDataList = ScriptableHelper.FindAllSO<T>(this);
        EditorUtility.SetDirty(this);
    }
#endif

    [TextArea] public string note;
    [Button("FindAllFlags")]
    public void FindAllFlags()
    {
        gameFlagDataList.Clear();
        Debug.Log("Find GameFlag:" + typeof(T).FullName);
        var myPath = AssetDatabase.GetAssetPath(this);
        Debug.Log("Mypath" + name + ":" + myPath);

        var dirPath = Path.GetDirectoryName(myPath);
        Debug.Log("Mypath dir" + name + ":" + dirPath);
        string[] allProjectFlags = AssetDatabase.FindAssets("t:" + typeof(T).FullName, new[] { dirPath });

        for (int i = 0; i < allProjectFlags.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allProjectFlags[i]);
            T flag = AssetDatabase.LoadAssetAtPath<T>(path);
            if(flag is AbstractGameFlagCollection) //collection不算
                continue;
            //  自動生成pathName
            // var pathName = path.Substring(16, path.Length - 16);
            // if (flag.flagpath != pathName)
            // {
            //     flag.flagpath = pathName;
            //     EditorUtility.SetDirty(flag);
            // }\
            if (FlagBelongThisCollection(flag))
                gameFlagDataList.Add(flag);
        }
        EditorUtility.SetDirty(this);
    }
#endif
    [InlineEditor()]
    [SerializeField]
    public List<T> gameFlagDataList = new List<T>();

    
    #region LastSavedItem

    [TabGroup("lastSaveItem")] [SerializeField]
    protected FlagFieldString serializedLastSaveID; //存檔點最後一個碰到的

//用這個寫出去
    public void SetLastSaveItem(DescriptableData item)
    {
        
        if (item == null)
        {
            Debug.LogError("No Item");
            return;
        }
        
        if (this.isValidLastSavedFlag(item)==false)
        {
            return;
        }


        serializedLastSaveID.CurrentValue = item.FinalSaveID;
        //FIXME:好像不用多一個狀態...?
        LastSaveIndex = _saveIDToIndex[serializedLastSaveID.CurrentValue];
    }

    [TabGroup("lastSaveItem")] [ShowInInspector]
    protected int LastSaveIndex;

    

    private bool isLastSaveIndexValid => LastSaveIndex >= 0 && LastSaveIndex < rawCollection.Count;

    [TabGroup("lastSaveItem")]
    [ShowInInspector]
    public DescriptableData lastSaveItem => isLastSaveIndexValid ? rawCollection[LastSaveIndex] : null;

    private void BuildSaveIDToIndex() //從SaveID對回index的對照表
    {
        // Debug.Log("BuildSaveIDToIndex",this);
        _saveIDToIndex.Clear();
        for (var i = 0; i < rawCollection.Count; i++)
        {
            if (rawCollection[i] == null)
            {
                Debug.LogError("Null Reference in Teleport Point Collection",this);
                continue;
            }

            if (!string.IsNullOrEmpty(rawCollection[i].FinalSaveID))
                _saveIDToIndex.Add(rawCollection[i].FinalSaveID, i);


            // Debug.Log("SaveID:"+rawCollection[i].SaveID);
        }

        if (serializedLastSaveID == null || string.IsNullOrEmpty(serializedLastSaveID.CurrentValue))
        {
            LastSaveIndex = -1;
            return;
        }


        if (_saveIDToIndex.TryGetValue(serializedLastSaveID.CurrentValue, out var value))
            LastSaveIndex = value;
        else
        {
            LastSaveIndex = -1;
            Debug.LogError("No SaveID in BuildSaveIDToIndex", this);
        }


    }


    #endregion
}
