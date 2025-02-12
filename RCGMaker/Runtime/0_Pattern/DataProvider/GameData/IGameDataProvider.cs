using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;

namespace RCGMaker.Core.DataProvider
{
    public interface IGameDataProvider
    {
        // public DescriptableData GetGameData();
        public DescriptableData GameData { get; }
    }

    [System.Serializable]
    public class GameDataProviderFromVariable : IGameDataProvider
    {
        [DropDownRef] public SODataVariable variable;

        public DescriptableData GameData => variable?.Value;
    }

    [System.Serializable]
    public class GameDataProviderFromVariableTag : VariableProvider<DescriptableData>, IGameDataProvider
    {
        // [DropDownRef] public VariableTag variableTag;

        public DescriptableData GameData => Value;
    }

    [System.Serializable]
    public class GameDataProviderReference : IGameDataProvider
    {
        public DescriptableData data;

        public DescriptableData GameData => data;
    }

    public interface IRandomGenerator<out T>
    {
        public T GetRandom();
    }

    [System.Serializable]
    public class GameDataProviderFromTable : IGameDataProvider
    {
        IRandomGenerator<DescriptableData> randomGenerator;

        public DescriptableData GameData => randomGenerator.GetRandom();
    }
}