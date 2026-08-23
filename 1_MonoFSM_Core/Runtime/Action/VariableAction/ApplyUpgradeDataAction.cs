using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    ///     把當前商品 UpgradeData 的 _levelDelta 累加到「目標 entity 上由 _levelVarTag 指定的那顆變數」。
    ///     var tag 跟著商品資料跑，所以機台不需要為每種升級各接一顆 action；
    ///     目標 entity 由 FindEntityFromUpgradeDataSource 解出來後用 VarEntity 傳進來。
    /// </summary>
    public class ApplyUpgradeDataAction : AbstractStateAction
    {
        [DropDownRef]
        [SerializeField]
        [Tooltip("當前選到的商品 GameData")]
        private VarGameData _gameData;

        [DropDownRef]
        [SerializeField]
        [Tooltip("要升級的目標 entity（用 FindEntityFromUpgradeDataSource 解）")]
        private VarEntity _targetEntity;

        public override string Description =>
            $"Upgrade {(_targetEntity != null ? _targetEntity.name : "null")} by {(_gameData != null ? _gameData.name : "null")}";

        protected override void OnActionExecuteImplement()
        {
            if (_gameData == null || _targetEntity == null)
            {
                Debug.LogError("[ApplyUpgradeData] _gameData 或 _targetEntity 沒設", this);
                return;
            }

            var upgradeData = UpgradeData.Of(_gameData.Value);
            if (upgradeData == null)
            {
                Debug.LogError(
                    $"[ApplyUpgradeData] 商品 {(_gameData.Value != null ? _gameData.Value.name : "null")} 沒掛 UpgradeData",
                    this);
                return;
            }

            var entity = _targetEntity.Value;
            if (entity == null)
            {
                Debug.LogError(
                    $"[ApplyUpgradeData] 解不到目標 entity（tag {(upgradeData.TargetEntityTag != null ? upgradeData.TargetEntityTag.name : "null")}），檢查 lookupMode 與購買者 entity",
                    this);
                return;
            }

            var levelVar = upgradeData.ResolveLevelVar(entity);
            if (levelVar == null)
            {
                Debug.LogError(
                    $"[ApplyUpgradeData] {entity.name} 上找不到 var {(upgradeData.LevelVarTag != null ? upgradeData.LevelVarTag.name : "null")}",
                    this);
                return;
            }

            //SetRaw / Get<float> 走 Unsafe.As reinterpret cast，指到別的型別會靜默寫壞值
            //等級用 VarFloat 是為了直接餵進 VariableStatModifier._valueVarRef（吃 VarFloatWrapper）
            if (levelVar is not VarFloat)
            {
                Debug.LogError(
                    $"[ApplyUpgradeData] {entity.name} 的 {levelVar.name} 不是 VarFloat（實際 {levelVar.GetType().Name}），_levelVarTag 指錯了",
                    this);
                return;
            }

            levelVar.SetRaw(levelVar.Get<float>() + upgradeData.LevelDelta, this);
        }
    }
}
