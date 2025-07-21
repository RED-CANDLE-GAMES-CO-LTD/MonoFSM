using System;
using System.Collections.Generic;
using System.Reflection;
using MonoDebugSetting;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEngine;

namespace MonoFSM.Core.Attributes.Editor
{
    /// <summary> 用在Runtime的property上，會在playmode時顯示
    /// <seealso cref="T:MonoFSM.Core.Attributes.ShowInPlayModeAttribute" />
    /// </summary>
    public class ShowInPlayModeAttributeProcessor : OdinAttributeProcessor
    {
        //把ShowInPlayModeAttribute加上ShowInInspector
        public override void ProcessChildMemberAttributes(
            InspectorProperty parentProperty,
            MemberInfo member,
            List<Attribute> attributes)
        {
            var showInPlayModeAttribute = member.GetAttribute<ShowInPlayModeAttribute>();
            if (showInPlayModeAttribute == null)
                return;

            if (!Application.isPlaying)
                return;
            

            if (showInPlayModeAttribute.DebugModeOnly)
            {
                // Debug.Log("[ShowInPlayModeAttributeProcessor] DebugModeOnly");
                if (DebugSetting.IsDebugMode)
                {
                    attributes.Add(new ShowInInspectorAttribute());
                    attributes.Add(new EnableGUIAttribute());
                    attributes.Remove(member.GetAttribute<DisableIfAttribute>());
                }

                else
                {
                    attributes.Remove(member.GetAttribute<ShowInInspectorAttribute>());
                    attributes.Remove(member.GetAttribute<EnableGUIAttribute>());
                }
            }
            else
            {
                attributes.Remove(member.GetAttribute<DisableIfAttribute>());
                attributes.Add(new ShowInInspectorAttribute());
            }
            // var autoChildrenAttribute = member.GetAttribute<AutoChildrenAttribute>();
            
        }
    }
}