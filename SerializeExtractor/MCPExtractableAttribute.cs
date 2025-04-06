using System;

namespace jerryee.UnityMCP
{
    /// <summary>
    /// for MCP to extract the data from the component
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class MCPExtractableAttribute : Attribute
    {
        //這個是拿來？
        public string DisplayName { get; set; }
    
        public MCPExtractableAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}