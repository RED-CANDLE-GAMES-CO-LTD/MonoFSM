using System.Collections.Generic;

namespace jerryee.UnityMCP
{
    //要撈attribute?
    //可以解釋發生什麼事的Component
    public interface IMCPExtractable
    {
        Dictionary<string, object> ExtractForMcp();
    }
}