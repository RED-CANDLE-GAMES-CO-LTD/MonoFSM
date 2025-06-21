using System;
using MonoFSM.Core.DataProvider;
using UnityEngine;

namespace MonoFSM.Core.DataType.Vector
{
    public class Vector3Literal : MonoBehaviour, IValueProvider<Vector3>
    {
        [SerializeField] private Vector3 _value;

        public enum VectorSpace
        {
            local,
            global
        }

        public VectorSpace vectorSpace = VectorSpace.global;

        public Vector3 GetValue() //
        {
            if (vectorSpace == VectorSpace.local)
            {
                var dir = transform.TransformDirection(_value);
                Debug.Log("Vector3Literal: Transforming local vector to global: " + dir, this);
                return dir;
            }

            return _value;
        }


        // public T GetValue<T>()
        // {
        //     return GetValue();
        // }

        public Vector3 Value => GetValue();
        public Type ValueType => typeof(Vector3);
        public string Description { get; }
    }
}