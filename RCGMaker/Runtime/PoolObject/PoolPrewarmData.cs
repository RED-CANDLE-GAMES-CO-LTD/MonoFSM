using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New PoolPrewarmData", menuName = "Boa/PoolManager/Create PoolPrewarmData", order = 3)]
public class PoolPrewarmData : ScriptableObject
{
    public List<PoolManager.PoolObjectEntry> objectEntries = new List<PoolManager.PoolObjectEntry>();

    public void UpdatePoolObjectEntry(PoolObject poolObject, int count)
    {
        //FIXME: prewarm太煩了
// #if UNITY_EDITOR
//         for (int i = 0; i < objectEntries.Count; i++)
//         {
//             if (objectEntries[i].prefab == poolObject)
//             {
//                 if (count > objectEntries[i].DefaultMaximumCount)
//                 {
//                     objectEntries[i].DefaultMaximumCount = count;
//                 }
//                 return;
//             }
//         }
//
//
//         PoolManager.PoolObjectEntry newEntry = new PoolManager.PoolObjectEntry();
//         newEntry.prefab = poolObject;
//         newEntry.DefaultMaximumCount = count;
//         objectEntries.Add(newEntry);
//
//         UnityEditor.EditorUtility.SetDirty(this);
// #endif
    }

    public void PrewarmObjects(PoolManager poolManager, MonoBehaviour owner)
    {
        poolManager.RegisterPoolPrewarmData(owner, this);

    }

}
