using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public interface ICollection
{
    DescriptableData currentItem { get; }
    List<DescriptableData> rawCollection { get; }
    int currentIndex { get; }
}
public interface IHasNewItem
{
    public bool HasNewItem();
}

//FIXME: collection 要是 descriptable嗎？
public abstract class AbstractGameFlagCollection : DescriptableData, IHasNewItem
{
    public override void FlagAwake(TestMode mode)
    {
        // Debug.Log("AbstractGameFlagCollection FlagAwake "+this.name);
        rawCollection.RemoveAll((n) => n == null);
        base.FlagAwake(mode);
    }

    [Header("顯示未取得的欄位(佔有格子)")] 
    [HideInInlineEditors]
    public bool IsDisplayingNotAcquiredItems = false;
    public abstract DescriptableData currentItem { get; }
    // public abstract List<GameFlagDescriptable> collection { get; }
    public abstract List<DescriptableData> rawCollection { get; }

    public int AcquiredCount //已經拿到的數量
    {
        get
        {
            var count = 0;
            foreach (var item in rawCollection)
            {
                if (item.IsAcquired)
                    count++;
            }

            return count;
        }
    }

    // public virtual int CurrentIndex => currentIndex;
    int _currentIndex = -1; //runtime only

    protected readonly Dictionary<string, int> _saveIDToIndex = new();



   
    
    public bool Contains(DescriptableData flag)
    {
        return rawCollection.Contains(flag);
    }

    public virtual bool isAnyItemAcquired //任何一個東西拿到了
    {

        get
        {
            for (int i = 0; i < rawCollection.Count; i++)
            {
                if (rawCollection[i].IsAcquired)
                    return true;
            }
            return false;
        }
    }
    public int currentIndex
    {
        get
        {
            _currentIndex = index.CurrentValue;
            // if (indexFlag)
            // {
            //     _currentIndex = indexFlag.CurrentValue;
            //     // return indexFlag.CurrentValue;
            // }
            return _currentIndex;
        }
        set
        {
            _currentIndex = value;
            index.CurrentValue = value;
            // if (indexFlag)
            // {
            //     indexFlag.CurrentValue = value;
            // }
        }
    }

    //virtual Next
    public virtual void Next()
    {
        if (rawCollection.Count == 0) return;
        currentIndex = (currentIndex + 1) % rawCollection.Count;
    }

    public virtual void Previous()
    {
        if (rawCollection.Count == 0) return;
        currentIndex = (currentIndex - 1 + rawCollection.Count) % rawCollection.Count;
    }
    
    
    public FlagFieldInt index;
    // public GameFlagInt indexFlag;
    public virtual void SetCurrent(DescriptableData data)
    {

    }

    public bool HasNewItem()
    {
        if (rawCollection == null)
            return false;

        //這個如果cache會比較好？但就要監聽所有的viewed的變化
        foreach (var d in rawCollection)
        {
            if (d.IsNew) //有新的，就回傳true
                return true;
        }

        return false;
    }

    public virtual void UpdateIndexCheck()
    {
        foreach (var d in rawCollection)
        {
            if (d.IsEquipping())
            {
                currentIndex = rawCollection.IndexOf(d);
                return;
            }
        }
    }
}
