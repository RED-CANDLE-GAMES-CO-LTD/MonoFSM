using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    public class VarListData : VarList<GameData>, IGameDataProvider
    {
        public GameData GameData => CurrentListItem;

        //選配：把清單抽成獨立 asset。指了就用 asset 的內容，沒指就走 prefab 上的 backing list。
        //同一台機台 prefab 換一顆 config 就是換一整份商品清單，不用在 variant 上疊 array override。
        [SOConfig("List")]
        [SerializeField] private GameDataListConfig _sourceConfig;

        private bool _hasWarnedEmptyConfig;

        //SourceList 會在 OnAfterDeserialize（序列化執行緒）被叫到，不能碰 .name / Debug.Log context，
        //那時 _sourceConfig 也不保證已經 deserialize 好，Items 可能是空的 —— 所以這裡只回傳，不印警告。
        protected override List<GameData> SourceList
        {
            get
            {
                if (_sourceConfig == null)
                    return base.SourceList;

                var items = _sourceConfig.Items;
                if (items == null || items.Count == 0)
                    return base.SourceList;

                return items;
            }
        }

        //空 config 的警告改在 runtime reset 印（主執行緒、config 已載入），每顆變數最多吵一次
        public override void ResetStateRestore(bool IsHardReset)
        {
            if (!_hasWarnedEmptyConfig && _sourceConfig != null && (_sourceConfig.Items == null || _sourceConfig.Items.Count == 0))
            {
                _hasWarnedEmptyConfig = true;
                Debug.LogWarning(
                    $"[VarListData] _sourceConfig ({_sourceConfig.name}) 的清單是空的，改用 prefab 上的 backing list",
                    this);
            }

            base.ResetStateRestore(IsHardReset);
        }
    }
}
