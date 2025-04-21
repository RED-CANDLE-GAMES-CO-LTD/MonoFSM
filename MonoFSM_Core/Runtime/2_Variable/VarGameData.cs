using Sirenix.OdinInspector;

using RCGMaker.Core.Attributes;

namespace MonoFSM.Variable
{
    public class VarGameData : GenericUnityObjectVariable<DescriptableData>
    {
        // public MySerializedType type; //typewrapper, 提供給filter functio?
        //defaultvalue可以給 
        [ShowInInspector]
        [SOConfig("10_Flags/GameData")]
        private DescriptableData CreateDefault
        {
            set => _defaultValue = value; //沒有serialized耶...
        }

        public override GameFlagBase FinalData 
            => Value;
    }
}