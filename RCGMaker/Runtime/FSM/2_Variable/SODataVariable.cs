using RCGMaker.Core.Attributes;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    //FIXME: 可以不寫code就共用這個class然後restrict type嗎？
    //FIXME: GenCode也是一種想法
    public class SODataVariable : GenericUnityObjectVariable<DescriptableData>
    {
        // public MySerializedType type; //typewrapper, 提供給filter functio?
        //defaultvalue可以給 
        [SOConfig("10_Flags/GameData")]
        private DescriptableData CreateDefault
        {
            set { _defaultValue = value; }
        }
    }
}