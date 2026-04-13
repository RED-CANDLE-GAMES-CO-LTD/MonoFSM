using MonoFSM.EditorExtension;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable
{
    public class VarVector3
        : AbstractFieldVariable<GameDataVector3, FlagFieldVector3, Vector3>, //可以改成?嗎？
            IHierarchyValueInfo
    {
        public override string ValueInfo => CurrentValue.ToString();
        public override bool IsDrawingValueInfo => true;

        public override bool IsValueExist => !IsNull;

        //FIXME: 另外寫nullable? 用一個bool過？hmmm 到了要清掉這樣嗎？
        [Button]
        void MoveTransformPosToValue()
        {
            transform.position = Value;
        }
    }
}
