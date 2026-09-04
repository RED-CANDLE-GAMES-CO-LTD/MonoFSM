using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
///     一份可重用的 GameData 清單（商店商品表之類）。
///     抽成 asset 的用途：同一台機器 prefab 只要換一顆 config，就換一整份清單，
///     不用為了改清單而在 prefab / variant 上疊 array override。
///     指到 VarListData._sourceConfig 即生效；沒指就走 VarListData 自己的 backing list。
///     疊層：_baseConfig 指到基底 config，本體的 _items 只存「額外加的東西」，
///     最終順序是「自己的 items 在前、base 的在後」（新加的先被看到）。
/// </summary>
[CreateAssetMenu(fileName = "GameDataListConfig", menuName = "GameData/GameData List Config",
    order = 1)]
public class GameDataListConfig : ScriptableObject
{
    //疊層來源，可留 null（就是沒有 base）。防循環：疊層深度上限 MaxDepth。
    [LabelText("Base Config（疊層來源）")]
    [SerializeField]
    private GameDataListConfig _baseConfig;

    [InfoBox("清單順序就是機台上左右切換的順序；有 base 時這裡只放額外追加的，會排在 base 的前面")]
    [SerializeField]
    private List<GameData> _items = new();

    //防循環引用（A.base = B, B.base = A）：超過就報錯停下
    private const int MaxDepth = 8;

    //疊層結果的快取，只有在有 base 時才會用到（沒 base 直接回 _items，零配置）
    private List<GameData> _mergedCache;

    /// <summary>
    ///     疊層後的最終清單。沒 base 時直接回傳內部 List，不做 copy（避免每次取值 GC）；
    ///     有 base 時回傳快取起來的合併結果，同樣不是每次都重建。
    /// </summary>
    public List<GameData> Items
    {
        get
        {
            if (_baseConfig == null)
                return _items;
#if UNITY_EDITOR
            //Editor 非 play 時資料隨時被改（含改到 base 那顆），每次重算才不會拿到舊的
            if (!Application.isPlaying)
                RebuildMerged();
#endif
            if (_mergedCache == null)
                RebuildMerged();
            return _mergedCache;
        }
    }

    [ShowInInspector]
    [PropertyOrder(100)]
    [LabelText("疊層後總數")]
    private int MergedCount => Items?.Count ?? 0;

    private void OnEnable()
    {
        _mergedCache = null;
    }

    private void OnValidate()
    {
        _mergedCache = null;
    }

    private void RebuildMerged()
    {
        _mergedCache ??= new List<GameData>();
        _mergedCache.Clear();
        AppendTo(_mergedCache, 0);
    }

    //自己的 items 先進 buffer，再往 base 疊；重複的（同一顆 GameData）以先進的為準，等同 override
    private void AppendTo(List<GameData> buffer, int depth)
    {
        if (depth >= MaxDepth)
        {
            Debug.LogError(
                $"[GameDataListConfig] _baseConfig 疊層超過 {MaxDepth} 層，可能有循環引用: {name}", this);
            return;
        }

        if (_items != null)
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null)
                    continue;
                if (!buffer.Contains(item))
                    buffer.Add(item);
            }

        if (_baseConfig == this)
        {
            Debug.LogError($"[GameDataListConfig] _baseConfig 指到自己: {name}", this);
            return;
        }

        if (_baseConfig != null)
            _baseConfig.AppendTo(buffer, depth + 1);
    }

#if UNITY_EDITOR
    /// <summary>
    ///     對本體建立一顆 variant：新 asset 的 _baseConfig 指回本體，_items 清空只留 delta。
    ///     語意跟 GameData.CreateVariant 一致（原 asset 完全不動）。
    /// </summary>
    [Button("建立 Variant（本體當 base）", ButtonSizes.Medium)]
    [PropertyOrder(101)]
    private void CreateVariant()
    {
        var path = AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[GameDataListConfig] 不是專案裡的 asset，無法建立 variant", this);
            return;
        }

        var dir = System.IO.Path.GetDirectoryName(path);
        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        var newPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{fileName} Variant.asset");
        if (!AssetDatabase.CopyAsset(path, newPath))
        {
            Debug.LogError($"[GameDataListConfig] CopyAsset 失敗：{path} → {newPath}", this);
            return;
        }

        AssetDatabase.ImportAsset(newPath);
        var variant = AssetDatabase.LoadAssetAtPath<GameDataListConfig>(newPath);
        if (variant == null)
        {
            Debug.LogError($"[GameDataListConfig] 複製後讀不回：{newPath}", this);
            return;
        }

        var so = new SerializedObject(variant);
        so.FindProperty(nameof(_baseConfig)).objectReferenceValue = this;
        so.FindProperty(nameof(_items)).ClearArray();
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(variant);
        AssetDatabase.SaveAssetIfDirty(variant);
        Selection.activeObject = variant;
        EditorGUIUtility.PingObject(variant);
        Debug.Log($"[GameDataListConfig] 建立 {newPath}，base = {name}", variant);
    }
#endif
}
