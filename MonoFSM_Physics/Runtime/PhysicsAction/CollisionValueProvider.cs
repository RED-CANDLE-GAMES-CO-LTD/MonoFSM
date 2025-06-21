using MonoFSM.Core;
using MonoFSM.Core.DataProvider;
using MonoFSM.Variable.Attributes;
using UnityEngine;
using System;

namespace MonoFSM.Runtime.Variable.Action.PhysicsAction
{
    //FIXME: 可能還需要經過一層運算...
    //運算要放在inspector上還是寫code? 支援寫數學式？
    public class CollisionValueProvider : MonoBehaviour, IValueProvider, IFloatProvider
    {
        public Type ValueType => typeof(float);
        [CompRef] [AutoParent] private ICollisionDataProvider _collisionDataProvider;

        public float Value => _collisionDataProvider.GetCollision().impulse.magnitude;

        public T GetValue<T>()
        {
            //先用
            if (typeof(T) == typeof(Collision))
                return (T)(object)_collisionDataProvider.GetCollision();
            if (typeof(T) == typeof(Vector3))
                return (T)(object)_collisionDataProvider.GetCollision().impulse;
            if (typeof(T) == typeof(float))
                return (T)(object)_collisionDataProvider.GetCollision().impulse.magnitude;
#if UNITY_EDITOR
            Debug.LogError("CollisionValueProvider: Unsupported type requested: " + typeof(T));
#endif
            return default;
        }

        public string GetDescription()
        {
            return "Collision Impulse Magnitude Provider";
        }

        public float GetFloat()
        {
            return _collisionDataProvider.GetCollision().impulse.magnitude;
        }

        public string Description => "Collision Impulse Magnitude";
    }
}