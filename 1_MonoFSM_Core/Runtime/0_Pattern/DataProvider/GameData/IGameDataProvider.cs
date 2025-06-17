using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;

namespace MonoFSM.Core.DataProvider
{
    //FIXME: 改名？
    public interface IGameDataProvider //
    {
        // public DescriptableData GetGameData();
        public DescriptableData GameData { get; }
    }

    // [Serializable]
    // public class SODataVarProvider : IVariableProvider, IGameDataProvider
    // {
    //     [DropDownRef] public SODataVariable variable;
    //
    //     public DescriptableData GameData => variable?.Value;
    //     public AbstractMonoVariable VarRaw => variable;
    //     public Type GetValueType => variable?.ValueType;
    //
    //     public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
    //     {
    //         return variable as TVariable;
    //     }
    // }

    // [Serializable]
    // public class SODataVarProvider : VariableProvider<VarGameData, DescriptableData>, IGameDataProvider
    // // IDescriptableDataProvider
    // {
    //     // [DropDownRef] public VariableTag variableTag;
    //
    //     public DescriptableData GameData => Value;
    // }
    //
    // [Serializable]
    // public class GameDataProviderReference : IGameDataProvider
    // {
    //     public DescriptableData data;
    //
    //     public DescriptableData GameData => data;
    // }

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