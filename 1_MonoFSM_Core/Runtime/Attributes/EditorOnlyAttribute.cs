using System;
using System.Diagnostics;

using Sirenix.OdinInspector;

namespace MonoFSM.Core.Attributes
{
    [IncludeMyAttributes]
    [Conditional("UNITY_EDITOR")]
    public class EditorOnlyAttribute : Attribute { } // FIXME: 這個真的有用嗎？ 應該沒用
}