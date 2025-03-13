using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

// [IncludeMyAttributes]
// [Required]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DropDownRefAttribute : Attribute
{
    //FIXME: 怎麼把value dropdown的內容抽過來？
    //限制可以選的範圍，應該會是以FSM為單位？或是吃不同參數來做
    public DropDownRefAttribute(Type parentType = null,string dynamicTypeGetter = "") //FIXME: 寫死在code裏，不好
    {
        _parentType = parentType;
        _dynamicTypeGetter = dynamicTypeGetter;
    }
    
    public Type _parentType; //default 會用 IVariableOwner, 寫在DropdownRefCompselector
    public string _dynamicTypeGetter; 
}