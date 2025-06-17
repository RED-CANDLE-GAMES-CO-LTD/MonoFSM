using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using UnityEngine;

namespace Fusion.Addons.KCC.ECM2.Examples.Networking.Fusion_v2.Characters.Scripts.Input
{
    public class BoolToFloatMapping : MonoBehaviour, IFloatProvider
    {
        [PreviewInInspector] [Auto] private IBoolProvider _boolProvider;

        public object GetValue()
        {
            return _boolProvider.IsTrue ? 1.0f : 0.0f;
        }

        public T GetValue<T>()
        {
            if (typeof(T) == typeof(bool))
                return (T)(object)_boolProvider.IsTrue;
            if (typeof(T) == typeof(float))
                return (T)(object)(_boolProvider.IsTrue ? 1.0f : 0.0f);
            return default;
        }

        public string GetDescription()
        {
            return GetFloat().ToString();
        }

        public float GetFloat()
        {
            //FIXME: lerp? 應該用這個做嗎？
            return _boolProvider.IsTrue ? 1.0f : 0.0f;
        }

        public string Description { get; }
    }
}