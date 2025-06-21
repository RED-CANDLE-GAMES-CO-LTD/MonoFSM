using System;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM_Physics.Runtime.PhysicsAction
{
    public class ParentRigidbodyProvider : MonoBehaviour, ICompProvider<Rigidbody>
    {
        [Required]
        [PreviewInInspector]
        [AutoParent] private Rigidbody _parentRigidbody;

        public Rigidbody Get()
        {
            if (_parentRigidbody == null)
            {
                Debug.LogError("No Rigidbody found on parent of " + gameObject.name, this);
                return null;
            }

            return _parentRigidbody;
        }

        public object GetValue()
        {
            return Get();
        }

        public Type ValueType => typeof(Rigidbody);

        public string Description =>
            _parentRigidbody != null ? "[Parent Rigidbody] " + _parentRigidbody.name : "No Parent Rigidbody assigned";
    }
}