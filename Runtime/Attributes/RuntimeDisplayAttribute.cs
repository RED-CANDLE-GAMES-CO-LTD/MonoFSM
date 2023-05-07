using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RCGMaker.Core.Attributes
{
    [IncludeMyAttributes]
    // [HideInPlayMode] //NOTE: 沒用，還是會call property, 但是不會顯示
    [ReadOnly]
    [Conditional("UNITY_EDITOR")]
    public class RuntimeDisplayAttribute : Attribute
    {
    }

    [IncludeMyAttributes]
    [Conditional("UNITY_EDITOR")]
    public class EditorOnlyAttribute : Attribute
    {
    }

    [IncludeMyAttributes]
    [BoxGroup("設定")]
    [Conditional("UNITY_EDITOR")]
    public class ConfigAttribute : Attribute
    {
    }

    public class MyProcessedClassAttributeProcessor : OdinAttributeProcessor
    {
        // public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        // {
        //     // attributes.Add(new InfoBoxAttribute("Dynamically added attributes!"));
        //     // attributes.Add(new InlinePropertyAttribute());
        //     Debug.Log(property.Name);
        //     var memberInfo = property.Info.GetMemberInfo();
        //     // Debug.Log(memberInfo);
        //     var runtimeDisplayAttribute = property.Info.GetMemberInfo().GetAttribute<RuntimeDisplayAttribute>();
        //     // Debug.Log(runtimeDisplayAttribute);
        //     if (runtimeDisplayAttribute != null)
        //     {
        //         if (Application.isPlaying)
        //         {
        //             attributes.Add(new ShowInInspectorAttribute());
        //         }
        //     }
        // }

        public override void ProcessChildMemberAttributes(
            InspectorProperty parentProperty,
            MemberInfo member,
            List<Attribute> attributes)
        {
            var runtimeDisplayAttribute = member.GetAttribute<RuntimeDisplayAttribute>();
            if (runtimeDisplayAttribute != null)
            {
                if (Application.isPlaying)
                {
                    attributes.Add(new ShowInInspectorAttribute());
                }
            }
        }
    }
}