using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

namespace MonoFSM.Core.Attributes.Editor
{
    /// <summary>
    /// 當 FlagField 的 T 是 string 時，自動為 ProductionValue 和 DevValue 加上 MultiLineProperty
    /// </summary>
    public class FlagFieldStringAttributeProcessor : OdinAttributeProcessor<FlagFieldString>
    {
        public override void ProcessChildMemberAttributes(
            InspectorProperty parentProperty,
            MemberInfo member,
            List<Attribute> attributes)
        {
            if (member.GetReturnType() == typeof(string))
            {
                attributes.Add(new MultiLinePropertyAttribute(3));
            }
        }
    }
}
