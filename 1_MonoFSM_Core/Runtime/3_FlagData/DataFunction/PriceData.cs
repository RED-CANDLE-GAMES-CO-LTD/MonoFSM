using System;
using UnityEngine;

/// <summary>
///     商品的販售價格，掛在 GameData 的 _dataFunctions 上。
///     由 GameData.Price / GameData.HasPrice 對外轉發（GameDataFieldProvider 只認 property，
///     而且進不了 DataFunction 內部，所以一定要走 GameData 上的 property）。
/// </summary>
[Serializable]
public class PriceData : AbstractDataFunction
{
    [SerializeField] private float _basePrice = 1; //基礎售價，折扣由機台的 runtime var 另外套

    public float BasePrice => _basePrice;
}
