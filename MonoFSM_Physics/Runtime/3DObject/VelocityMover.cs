using System;
using MonoFSM.Core.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Runtime.LevelDesign._3DObject
{
    //rigidbody給速度就好？
    //dynamic才可以... kinematic的話要自己管
    public class VelocityMover : MonoBehaviour //ISpawnEnable? ISceneAwake?
    {
        [PreviewInInspector] [AutoParent] private Rigidbody _rigidbody;
        public float _speed = 10f;


        //丟出來就決定方向就好
        public void Shoot()
        {
            var rot = transform.rotation;
            var forward = rot * Vector3.forward;
            if (_rigidbody == null)
            {
                Debug.LogError("Rigidbody is not assigned.", this);
                return;
            }

            SetVelocity(forward * _speed);
        }


        private void OnEnable()
        {
            //應該從哪call?
            Shoot();
        }

        private void SetVelocity(Vector3 velocity)
        {
            Debug.Log($"Setting velocity to: {velocity}", this);
            _rigidbody.linearVelocity = velocity;
        }
    }
}