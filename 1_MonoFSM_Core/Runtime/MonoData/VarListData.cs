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

        //警告只在 play 時印：Editor 序列化階段也會走到這裡，碰 Application.isPlaying 會丟 UnityException，
        //所以用 _hasWarned 讓每顆變數最多吵一次。
        private bool _hasWarnedEmptyConfig;

        protected override List<GameData> SourceList
        {
            get
            {
                if (_sourceConfig == null)
                    return base.SourceList;

                var items = _sourceConfig.Items;
                if (items == null || items.Count == 0)
                {
                    if (!_hasWarnedEmptyConfig)
                    {
                        _hasWarnedEmptyConfig = true;
                        Debug.LogWarning(
                            $"[VarListData] _sourceConfig ({_sourceConfig.name}) 的清單是空的，改用 prefab 上的 backing list",
                            this);
                    }

                    return base.SourceList;
                }

                return items;
            }
        }
    }
}
