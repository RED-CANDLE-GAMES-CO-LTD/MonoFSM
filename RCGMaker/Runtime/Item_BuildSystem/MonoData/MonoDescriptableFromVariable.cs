using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace RCGMaker.Runtime.Mono
{
    //目的：想要灌sampleData, or data type
    public class MonoDescriptableFromVariable : MonoBehaviour, IMonoDescriptable
    {
        public MonoDescriptableTag Key { get; }
        public DescriptableData SampleData;

        public IDescriptableData Descriptable
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                {
                    return SampleData;
                }
#endif

                return _variableMonoDescriptableProvider.Value?.Descriptable;
            }
        }

        public VariableMonoDescriptableProvider _variableMonoDescriptableProvider;
    }
}