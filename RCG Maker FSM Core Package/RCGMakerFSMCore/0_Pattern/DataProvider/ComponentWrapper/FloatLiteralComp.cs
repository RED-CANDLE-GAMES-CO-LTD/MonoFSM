using RCGMaker.Core.DataProvider;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGFSMCore._0_Pattern.DataProvider.ComponentWrapper
{
    public class FloatLiteralComp : MonoBehaviour, IFloatProvider
    {
        [FormerlySerializedAs("literal")] public float _literal;

        public float GetFloat()
        {
            return _literal;
        }

        public string Description => _literal.ToString();

        [Button("Rename")]
        void Rename()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Rename");
            name = "[FloatLiteral]" + _literal;
#endif
        }
    }
}