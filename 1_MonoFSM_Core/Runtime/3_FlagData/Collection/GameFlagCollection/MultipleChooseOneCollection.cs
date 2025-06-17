using System;
using System.Collections;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

//主角有多個這個東西，可以選一個用
public class MultipleChooseOneCollection<T> : GameFlagBaseCollection<T>, ICollection where T : DescriptableData, IToggleable
{
    public override DescriptableData currentItem => current;

    // _current = gameFlagDataList[CurrentIndex];
    // return gameFlagDataList[CurrentIndex];
    [ReadOnly]
    [ShowInInspector]
    private T _lastItem;
    [ReadOnly]
    [ShowInInspector]
    public T current => currentIndex != -1 && currentIndex < currentAcquiredList.Count
        ? currentAcquiredList[currentIndex]
        : null;
    
    [Header("[Runtime] 現在擁有的")]
    [ReadOnly]
    [NonSerialized]
    [PreviewInInspector]
    public List<T> currentAcquiredList = new(); //這個很爛！

    private void AggregateAcquiredList()
    {
        currentAcquiredList.Clear();
        foreach (var data in gameFlagDataList)
        {
            if (data.IsAcquired)
            {
                AddToCurrentAcquiredList(data);
            }
        }
    }

    private int acquiredCount => currentAcquiredList.Count;

    //
    // public void UpdateAcquiredList() //能力包Override的時候應該要更新這個..?
    // {
    //     FetchAllAcquiredAndListen();
    // }


    public override void FlagInitStart()
    {
        base.FlagInitStart();
        _lastItem = null;


        FetchAllAcquiredAndListen();

        //-1 就不會裝
        if (currentIndex < currentAcquiredList.Count && currentIndex >= 0)
        {
            // Debug.Log("Multiple Choose One Init" + name + ":" + currentAcquiredList[currentIndex], currentAcquiredList[currentIndex]);
            SetCurrent(currentAcquiredList[currentIndex]);
        }

        for (var i = 0; i < gameFlagDataList.Count; i++)
        {
            //一拿到新的...index會變，重新塞回去
            //NOTE: 看有沒有bug??

            var j = i;
            //這個once被移除了？？
            gameFlagDataList[j].acquired.AddListener((value) =>
            {
                Debug.Log("Acquired of Collection Check:" + gameFlagDataList[j].name + ":" + value);
                //如果是第一次拿到，就自動切過去
                if (acquiredCount <= 1 && value)
                {
                    Debug.Log("First Acquired:" + gameFlagDataList[j].name);
                    SetCurrent(gameFlagDataList[j]);
                }
            }, this);
        }
    }


    private void AddToCurrentAcquiredList(T data)
    {
        if (!currentAcquiredList.Contains(data))
        {
            // Debug.Log("MultipleChooseOneCollection AddToCurrentAcquiredList:" + data.name);
            currentAcquiredList.Add(data);
        }
    }

    private void RemoveFromCurrentAcquiredList(T data)
    {
        if (currentAcquiredList.Contains(data))
        {
            // Debug.Log("MultipleChooseOneCollection RemoveFromCurrentAcquiredList:" + data.name);
            currentAcquiredList.Remove(data);
        }
    }

    private T temp;
    //哪些拿到了
    [Button("Acquired Check")]
    private void FetchAllAcquiredAndListen()
    {
        currentAcquiredList.Clear();
        foreach (var data in gameFlagDataList)
        {
            if (data.IsAcquired)
            {
                AddToCurrentAcquiredList(data);
                OnAcquiredCollectionChange?.Invoke();
            }

            //runtime改就只能監聽？
            // Debug.Log("Acquired Check:" + data.name + ":" + data.IsAcquired);
            //為什麼之前可以解鎖弓箭？
            data.acquired.AddListener((value) =>
            {
                //會先拔掉穿雲，再拿到LV2, 所以會是
                var index = currentIndex;
                if (value) //獲得新的能力
                {
                    AddToCurrentAcquiredList(data);
                }
                else //交易就把他拿掉
                {
                    RemoveFromCurrentAcquiredList(data);
                }

                AggregateAcquiredList();
                if (index < currentAcquiredList.Count)
                {
                    //FIXME:記住last valid index?
                    //應該要virtual equip, 然後再抽換...
                    SetCurrent(currentAcquiredList[index]);
                    OnAcquiredCollectionChange?.Invoke();
                }
                else
                {
                    // 可能被拔掉了，先不管
                }
            }, this);
            
            //如果現在不能選，就不要加進去
            //EX: 升級弓箭後，把上一個等級的弓箭給關掉，看起來沒有辦法直接更新到
            // if (data.IsSelectableConditionValid == false)
            // {
            //     RemoveFromCurrentAcquiredList(data);
            //     OnAcquiredCollectionChange?.Invoke();
            // }
        }
    }

    private Action OnAcquiredCollectionChange;

    public virtual void SetCurrent(T data)
    {
        SetCurrent(data as DescriptableData);
    }
    [Button("InspectorSetCurrent")]
    public void InspectorSetCurrent()
    {
        SetCurrent(current);
    }


    public sealed override void SetCurrent(DescriptableData data)//選擇
    {
        //這個有點多餘？
        if (_lastItem)
            _lastItem.UnEquipCheck();

        //移除所有裝備
        // foreach (var item in currentAcquiredList)
        // {
        //     if (item is PlayerAbilityData abilityData)
        //         abilityData.UnEquipCheck();
        // }

        var findIndex = currentAcquiredList.FindIndex(0, currentAcquiredList.Count, (d) => d == data);
        currentIndex = findIndex;
        _lastItem = current;
        current.EquipCheck();

    }

    //只選已經拿到的東西
    public override void Next()
    {
        if (acquiredCount == 0) return;

        var index = currentIndex;
        currentAcquiredList.NextIndex(ref index);
        currentIndex = index;
        SetCurrent(currentAcquiredList[index]);
        // Debug.Log("N: CurrentWeaponIndex" + index);
        // currentAquiredWeapons[currentIndex].equiped.CurrentValue = true;
        // ChangeMagic(stateTypes[currentIndex]);
        
    }

    public override void Previous()
    {
        if (acquiredCount == 0) return;

        var index = currentIndex;
        currentAcquiredList.PreviousIndex(ref index);
        currentIndex = index;
        SetCurrent(currentAcquiredList[index]);
        // currentWeaponIndexFlag.CurrentValue = collection.gameFlagDataList.IndexOf(currentAquiredWeapons[index]);
        // Debug.Log("P: CurrentWeaponIndex" + index);
    }
}
