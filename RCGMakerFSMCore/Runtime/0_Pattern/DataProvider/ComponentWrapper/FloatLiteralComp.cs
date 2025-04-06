using jerryee.UnityMCP;
using RCGMaker.Core.DataProvider;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGFSMCore._0_Pattern.DataProvider.ComponentWrapper
{
    //FloatConstant?
    public class FloatLiteralComp : MonoBehaviour, IFloatProvider
    {
        [MCPExtractable]
        [FormerlySerializedAs("literal")] public float _literal;

        public float GetFloat()
        {
            return _literal;
        }

        public string Description => _literal.ToString();

        [Button("Rename")]
        void Rename() //FIXME: rename可以包起來大家用？
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Rename");
            name = "[Float]" + _literal;
#endif
        }
    }
}