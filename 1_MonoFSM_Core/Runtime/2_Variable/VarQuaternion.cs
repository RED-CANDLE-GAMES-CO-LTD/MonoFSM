using MonoFSM.EditorExtension;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable
{
    public class VarQuaternion
        : AbstractFieldVariable<GameDataQuaternion, FlagFieldQuaternion, Quaternion>,
            IHierarchyValueInfo
    {
        public override string ValueInfo => CurrentValue.eulerAngles.ToString();
        public override bool IsDrawingValueInfo => true;

        public override bool IsValueExist => !IsNull;

        [Button]
        void MoveTransformRotationToValue()
        {
            transform.rotation = Value;
        }
    }
}
