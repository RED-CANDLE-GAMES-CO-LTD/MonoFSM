using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
///     一份可重用的 GameData 清單（商店商品表之類）。
///     抽成 asset 的用途：同一台機器 prefab 只要換一顆 config，就換一整份清單，
///     不用為了改清單而在 prefab / variant 上疊 array override。
///     指到 VarListData._sourceConfig 即生效；沒指就走 VarListData 自己的 backing list。
/// </summary>
[CreateAssetMenu(fileName = "GameDataListConfig", menuName = "GameData/GameData List Config",
    order = 1)]
public class GameDataListConfig : ScriptableObject
{
    [InfoBox("清單順序就是機台上左右切換的順序")]
    [SerializeField]
    private List<GameData> _items = new();

    //直接回傳內部 List，不做 copy（避免每次取值 GC）
    public List<GameData> Items => _items;
}
