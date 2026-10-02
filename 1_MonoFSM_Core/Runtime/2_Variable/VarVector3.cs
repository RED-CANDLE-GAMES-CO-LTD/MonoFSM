    using MonoFSM.EditorExtension;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable
{
    /// <summary>
    /// Vector3 的 [Var]，常拿來裝方向或位置（例：move action 的 _moveDirOutput / _faceDirOutput 給 SimpleChController 吃）。
    /// </summary>
    public class VarVector3
        :AbstractFieldVariable<GameDataVector3, FlagFieldVector3, Vector3>
    {
        public override string ValueInfo => CurrentValue.ToString();
        public override bool IsDrawingValueInfo => true;

        protected override bool IsLocalValueExist => !IsNull;

        //FIXME: 另外寫nullable? 用一個bool過？hmmm 到了要清掉這樣嗎？
        [Button]
        void MoveTransformPosToValue()
        {
            transform.position = Value;
        }

        //要跟的話就裝一個TransformFollower
    }
}
