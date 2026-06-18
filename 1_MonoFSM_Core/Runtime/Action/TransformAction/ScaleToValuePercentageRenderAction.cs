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
        public int _mappingScaleMax;
        public bool _applyOnYOnly;

        public override void OnEnterRenderImplement()
        {
            if (_target == null)
                return;
            if (!_applyOnYOnly)
                _target.localScale = Vector3.one * _percentageValue.Value * _mappingScaleMax;
            else
            {
                var scale = _target.localScale;
                scale.y = _percentageValue.Value * _mappingScaleMax;
                _target.localScale = scale;
            }
        }
    }
}
