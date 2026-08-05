using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    /// <summary>
    ///     回報「現在」的世界座標。給 debug / cheat 用（例如複製玩家當下位置成連結）。
    ///     由瞬移類 component 一併實作，讓「複製位置」和「貼上瞬移」共用同一個目標對象。
    /// </summary>
    public interface ICurrentPositionProvider
    {
        bool TryGetCurrentPosition(out Vector3 pos);
    }
}
