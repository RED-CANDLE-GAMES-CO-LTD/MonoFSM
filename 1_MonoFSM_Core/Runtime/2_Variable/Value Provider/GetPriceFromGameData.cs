using MonoFSM.Foundation;
using MonoFSM.Variable;

namespace MonoFSM.Core.DataProvider
{
    /// <summary>
    ///     取出 GameData 上 PriceData 的售價，讓 VarFloat 能拿到「目前選取商品的價格」
    ///     餵進既有的 compare / math action。寫法對照同資料夾的 GetBindPrefabFromGameData。
    /// </summary>
    public class GetPriceFromGameData : AbstractGetter, IValueProvider<float>
    {
        public VarGameData _gameData;

        //沒掛 PriceData 就當作沒有值，讓買得起的條件不成立（而不是變成免費）
        public override bool HasValue =>
            _gameData != null && _gameData.Value != null && _gameData.Value.HasPrice;

        public float Value => HasValue ? _gameData.Value.Price : 0f;
    }
}
