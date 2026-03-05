using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    /// <summary>
    /// 將 target 的 local Y 設定為指定的 world Y 值
    /// localY = worldY - parent.worldY
    /// </summary>
    public class SetLocalYAction : AbstractStateAction
    {
        public Transform _target;
        public VarFloat _worldY;

        protected override void OnActionExecuteImplement()
        {
            var parentWorldY = _target.parent != null ? _target.parent.position.y : 0f;
            var localPos = _target.localPosition;
            localPos.y = _worldY.Value - parentWorldY;
            _target.localPosition = localPos;
        }
    }
}
