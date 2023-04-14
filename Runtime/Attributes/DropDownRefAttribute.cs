using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DropDownRefAttribute : Attribute
{
    public DropDownRefAttribute()
    {
    }
}