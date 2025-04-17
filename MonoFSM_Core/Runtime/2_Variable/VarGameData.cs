using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;

namespace MonoFSM.Variable
{
    //FIXME: 可以不寫code就共用這個class然後restrict type嗎？
    //FIXME: GenCode也是一種想法
    public class VarGameData : GenericUnityObjectVariable<DescriptableData>
    {
        // public MySerializedType type; //typewrapper, 提供給filter functio?
        //defaultvalue可以給 
        [ShowInInspector]
        [SOConfig("10_Flags/GameData")]
        private DescriptableData CreateDefault
        {
            set { _defaultValue = value; } //沒有serialized耶...
        }

        public override GameFlagBase FinalData => Value;
    }
}