using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Attributes
{
    
    /// <summary>
    ///  自動把parent MonoBehaviour assign到這個field上
    /// </summary>
    [IncludeMyAttributes]
    [DisableIf("@true")]
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeReferenceParentValidateAttribute:Attribute
    {
        
    }
}