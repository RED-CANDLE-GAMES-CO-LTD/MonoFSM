using MonoFSM.Variable.Attributes;
using RCGMaker.Core;
using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace RCGMaker.Runtime.FSM.RCGStateMachine.Action.PhysicsAction
{
    //FIXME: 可能還需要經過一層運算...
    //運算要放在inspector上還是寫code? 支援寫數學式？
    public class CollisionValueProvider : MonoBehaviour, IValueProvider, IFloatProvider
    {
        [CompRef] [AutoParent] private ICollisionDataProvider _collisionDataProvider;

        public object GetValue()
        {
            //inject運算 AbstractCalculation[] _calculations;
            return _collisionDataProvider.GetCollision().impulse.magnitude;
        }

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