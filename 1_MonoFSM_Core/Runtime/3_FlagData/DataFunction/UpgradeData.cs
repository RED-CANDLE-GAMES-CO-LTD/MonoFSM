using System;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using UnityEngine;

/// <summary>
///     這個商品買下去要把哪顆模組的等級加幾級，掛在升級商品 GameData 的 _dataFunctions 上。
///     機台完全不需要知道有哪些升級：它只從當前商品的 UpgradeData 讀「目標 entity tag + 要寫的 var tag + 加幾級」，
///     所以加一種新升級＝加一顆 GameData，機台 prefab 不用動。
///     _targetEntityTag 指模組 entity（例 d_Fuel Upgrade Module）就是個人向，指 d_TeamStatus 就是全隊向。
///     價格仍走 GameData 的 config 表（d_Price），不放這裡。
/// </summary>
[Serializable]
public class UpgradeData : AbstractDataFunction
{
    [Tooltip("要升級哪顆模組：在購買者 entity 的 scope 裡用這個 tag 找目標 entity")]
    [SerializeField]
    private MonoEntityTag _targetEntityTag;

    [Tooltip("要寫目標 entity 上的哪顆變數（VarFloat，才能直接餵進 VariableStatModifier._valueVarRef）")]
    [SerializeField]
    private VariableTag _levelVarTag;

    [Tooltip("買下去之後把那顆變數加幾（累加，不是設定）。商品之間沒有先後依賴，各自可獨立購買")]
    [SerializeField]
    private int _levelDelta = 1;

    public MonoEntityTag TargetEntityTag => _targetEntityTag;
    public VariableTag LevelVarTag => _levelVarTag;
    public int LevelDelta => _levelDelta;

    /// <summary>
    ///     從 GameData 拿 UpgradeData（沒掛就回 null，不噴 error；GameData.GetDataFunction 會 LogError）。
    /// </summary>
    public static UpgradeData Of(GameData data) =>
        data != null && data.TryGetDataFunction<UpgradeData>(out var upgradeData) ? upgradeData : null;

    /// <summary>
    ///     解出「目標 entity 上那顆等級變數」，任一環節缺就回 null。
    /// </summary>
    public AbstractMonoVariable ResolveLevelVar(MonoEntity targetEntity)
    {
        if (targetEntity == null || _levelVarTag == null)
            return null;
        return targetEntity.GetVar(_levelVarTag);
    }
}
