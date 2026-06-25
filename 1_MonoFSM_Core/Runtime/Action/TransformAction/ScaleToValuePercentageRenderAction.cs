using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.TransformAction
{
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
    }
}
