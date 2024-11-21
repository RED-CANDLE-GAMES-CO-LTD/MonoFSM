using System;
using Sirenix.OdinInspector;

[IncludeMyAttributes]
[Required]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DropDownRefAttribute : Attribute
{
    //FIXME: 怎麼把value dropdown的內容抽過來？
    //限制可以選的範圍，應該會是以FSM為單位？或是吃不同參數來做
    public DropDownRefAttribute(Type type = null)
    {
    }
}