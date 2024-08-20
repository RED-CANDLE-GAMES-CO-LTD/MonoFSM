using System;

namespace Auto_Attribute.Runtime
{
    //Editor Only會比較好記?
    [AttributeUsage(AttributeTargets.Field)]
    public class PreventAutoCacheAttribute : Attribute
    {
    }
}