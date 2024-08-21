using System;

namespace Auto_Attribute.Runtime
{
    //Editor Only會比較好記?
    //被#if UNITY_EDITOR包起來的會被刪除, 所以掛著表示不要被cache
    [AttributeUsage(AttributeTargets.Field)]
    public class PreventAutoCacheAttribute : Attribute
    {
    }
}