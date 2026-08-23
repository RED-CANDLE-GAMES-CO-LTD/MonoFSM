using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoValueProvider;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper.Float
{
    /// <summary>
    /// 兩個目標位置的距離，Getter 型（每次現算，不存狀態，Simulate/Render 讀到的都是當下正確值）
    /// 兩端都用 TargetPositionResolver，所以 VarVector3 / VarTransform / VarEntity / Transform 都能接
    /// 典型用法：手飛回 muzzle 的到達判定 → 配 VarFloatCompareConstCondition (<= 0.15) 做 transition
    /// </summary>
    public class DistanceValueSource : AbstractValueSource<float>, IFloatProvider
    {
        [BoxGroup("From")] [HideLabel] [SerializeField]
        private TargetPositionResolver _from;

        [BoxGroup("To")] [HideLabel] [SerializeField]
        private TargetPositionResolver _to;

        [Tooltip("任一端解不到目標時的回傳值，預設給極大值，避免距離條件被誤觸發")]
        [SerializeField] private float _invalidValue = float.MaxValue;

        public override float Value
        {
            get
            {
                if (_from.HasTarget == false || _to.HasTarget == false)
                    return _invalidValue;
                //兩端都確認 HasTarget 了，fallback 不會被用到
                return Vector3.Distance(
                    _from.GetTargetPosition(Vector3.zero),
                    _to.GetTargetPosition(Vector3.zero));
            }
        }

        public override string Description => $"|{_from.ActiveSource} - {_to.ActiveSource}|";
    }
}
