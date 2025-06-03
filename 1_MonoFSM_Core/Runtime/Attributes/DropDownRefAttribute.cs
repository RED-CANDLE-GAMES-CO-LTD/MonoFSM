using System;
using Sirenix.OdinInspector;

/// FIXME: 想要包含[MCPExtractable]...還是讓python也處理？
[Required] //沒道理要選結果沒選到東西？
[IncludeMyAttributes]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DropDownRefAttribute : Attribute
{
    //FIXME: 怎麼把value dropdown的內容抽過來？
    //限制可以選的範圍，應該會是以FSM為單位？或是吃不同參數來做
    public DropDownRefAttribute(Type parentType = null, string dynamicTypeGetter = "",bool findFromParentTransform = false) //FIXME: 寫死在code裏，不好
    {
        _parentType = parentType;
        _dynamicTypeGetter = dynamicTypeGetter;
        _findFromParentTransform = findFromParentTransform;
    }

    public Type _parentType; //default 會用 IVariableOwner, 寫在DropdownRefCompselector
    public string _dynamicTypeGetter;
    public bool _findFromParentTransform = false; 
}