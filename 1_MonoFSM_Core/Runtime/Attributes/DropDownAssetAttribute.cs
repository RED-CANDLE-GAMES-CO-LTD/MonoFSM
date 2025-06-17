using System;

namespace MonoFSM.Runtime.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DropDownAssetAttribute:Attribute
    {
        
        public string FilterGetter;
        public DropDownAssetAttribute(string filterGetter)
        {
            FilterGetter = filterGetter;
            //how to get the function which the name is filter?
        }
       

    }
}