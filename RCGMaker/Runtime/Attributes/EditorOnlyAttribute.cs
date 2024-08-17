using System;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.Attributes
{
    [IncludeMyAttributes]
    [Conditional("UNITY_EDITOR")]
    public class EditorOnlyAttribute : Attribute //FIXME: 這個真的有用嗎？
    {
    }

    public interface IEditorOnly
    {
    }

    public interface IEditorOnlyGameObject
    {
        public GameObject gameObject { get; }
    }
}