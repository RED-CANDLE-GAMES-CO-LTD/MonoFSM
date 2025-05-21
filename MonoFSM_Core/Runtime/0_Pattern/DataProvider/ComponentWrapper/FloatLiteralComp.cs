using System;
using jerryee.UnityMCP;
using RCGMaker.Core;
using RCGMaker.Core.DataProvider;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.DataProvider
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

        public object GetValue()
        {
            return _literal;
        }

        public T GetValue<T>()
        {
            if (typeof(T) == typeof(float)) return (T)(object)_literal;

            throw new InvalidCastException($"Cannot cast {typeof(float)} to {typeof(T)}");
        }

        public string GetDescription()
        {
            return _literal.ToString();
        }
    }
}