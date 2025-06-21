using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using UnityEngine;

namespace MonoFSM.Core.DataType.Vector
{
    public class Vector3FromHitData : MonoBehaviour, IValueProvider<Vector3>
    {
        [PreviewInInspector] [AutoParent] private IHitDataProvider _hitDataProvider;
        public string Description { get; }
        public Vector3 Value => _hitDataProvider.GetHitData().Dir;
    }
}