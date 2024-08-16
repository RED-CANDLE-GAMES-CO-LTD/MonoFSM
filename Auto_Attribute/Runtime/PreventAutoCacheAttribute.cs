using System;

namespace Auto_Attribute.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PreventAutoCacheAttribute : Attribute
    {
    }
}