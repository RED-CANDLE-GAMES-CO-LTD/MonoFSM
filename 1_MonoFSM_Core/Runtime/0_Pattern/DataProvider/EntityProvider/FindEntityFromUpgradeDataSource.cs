using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    ///     從「當前商品 GameData」的 UpgradeData 讀 _targetEntityTag，再到購買者 entity 的 scope 裡把那顆模組 entity 解出來。
    ///     跟 FindMonoEntityValueSource 的差別：tag 不是 prefab 上寫死的欄位，而是跟著商品資料跑，
    ///     所以同一台機器可以賣「電池升級」「移動速度升級」…，機台本身不知道有哪些升級。
    ///     scope 解法沿用 FindMonoEntityValueSource：Sub 走 source 的 _ownBinder，Sibling 走 source 所屬的 _parentBinder。
    /// </summary>
    public class FindEntityFromUpgradeDataSource : AbstractEntitySource
    {
        [PropertyOrder(-1)]
        [DropDownRef]
        [SerializeField]
        [Tooltip("購買者 entity（互動者），例：receiver 的 [local] Interact hitEntity")]
        private VarEntity _sourceVarEntity;

        [PropertyOrder(-1)]
        [DropDownRef]
        [SerializeField]
        [Tooltip("當前選到的商品 GameData")]
        private VarGameData _gameData;

        [PropertyOrder(-1)]
        [SerializeField]
        private EntityLookupMode _lookupMode = EntityLookupMode.Sibling;

        public override string SuggestDeclarationName => "Upgrade Target Entity";

        //節點自動命名用：別讓它變成 "=> FindEntityFromUpgradeDataSource"
        public override string Description => "Upgrade Target of UpgradeData";

        //解出來的 tag（給 Inspector 看的，_expectedEntityTag 這條路對這顆 source 沒有意義）
        [ShowInPlayMode]
        private MonoEntityTag TargetTag => UpgradeData.Of(_gameData?.Value)?.TargetEntityTag;

        [ShowInPlayMode]
        public override MonoEntity monoEntity
        {
            get
            {
                if (_sourceVarEntity == null || _gameData == null)
                    return null;
                var sourceEntity = _sourceVarEntity.Value;
                if (sourceEntity == null)
                    return null;
                var tag = TargetTag;
                if (tag == null)
                    return null;
                return _lookupMode == EntityLookupMode.Sub
                    ? sourceEntity.GetSubEntity(tag)
                    : sourceEntity.GetSiblingEntity(tag);
            }
        }
    }
}
