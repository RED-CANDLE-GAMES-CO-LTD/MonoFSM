using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.TransformAction
{
    /// <summary>
    /// 每個 render frame 把 _percentageValue（0~1）線性映射到 _mappingScaleMin~Max 當成縮放值
    /// （血條式的脹縮）。注意它是**覆寫**而不是相乘：非 _applyOnYOnly 時直接寫成 Vector3.one * n，
    /// 吃不到 _target 原本的 localScale；而且 Mathf.Lerp 會 clamp，倍率超出 min~max 也拉不上去。
    /// 要「以 prefab 原尺寸為基準乘一個倍率」用別的（例：DismantledPartScaleRender）。
    /// </summary>
    public class ScaleToValuePercentageRenderAction : AbstractRenderBehaviour
    {
        public VarFloat _percentageValue;
        public Transform _target;
        public float _mappingScaleMin;
        public float _mappingScaleMax;
        public bool _applyOnYOnly;

        public override void OnEnterRenderImplement()
        {
            if (_target == null)
                return;
            var mappedScale = Mathf.Lerp(_mappingScaleMin, _mappingScaleMax, _percentageValue.Value);
            if (!_applyOnYOnly)
                _target.localScale = Vector3.one * mappedScale;
            else
            {
                var scale = _target.localScale;
                scale.y = mappedScale;
                _target.localScale = scale;
            }
        }

        public override void OnRenderImplement()
        {
            OnEnterRenderImplement();
        }
    }
}
