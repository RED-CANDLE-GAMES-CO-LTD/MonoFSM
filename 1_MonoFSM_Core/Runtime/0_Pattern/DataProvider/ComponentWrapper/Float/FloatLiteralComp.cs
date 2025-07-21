using System;
using System.Globalization;
using MonoFSM.Foundation;
using MonoFSM.Core.DataProvider;
using UnityEngine.Serialization;

namespace MonoFSM.DataProvider
{
    //FloatConstant?
    public class FloatLiteralComp : AbstractDescriptionBehaviour, IFloatProvider
    {
        // [MCPExtractable]
        [FormerlySerializedAs("literal")] public float _literal;

        public float GetFloat()
        {
            return _literal;
        }

        public override string Description => _literal.ToString(CultureInfo.CurrentCulture);

        protected override string DescriptionTag => "Float";

//         [Button("Rename")]
//         void Rename() //FIXME: rename可以包起來大家用？
//         {
// #if UNITY_EDITOR
//             UnityEditor.Undo.RecordObject(this, "Rename");
//             name = "[Float]" + _literal;
// #endif
//         }

        public float Value => _literal;
        public Type ValueType => typeof(float);

        public string GetDescription()
        {
            return Description;
        }
    }
}