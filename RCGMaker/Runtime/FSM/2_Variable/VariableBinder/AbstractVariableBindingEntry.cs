using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.VariableBinder
{
    public interface IName
    {
        string Name { get; }
    }

    public abstract class VariableBindingEntry<T> : AbstractVariableBindingEntry where T : IName
    {
        //What is the term that two variables which one is dependent to another
        [FormerlySerializedAs("bindingSource")]
        [FormerlySerializedAs("variableSource")]
        [FormerlySerializedAs("boolSource1")]
        public T WatchSource;

        [FormerlySerializedAs("boolSource2")] public T dependentVariable;

        [Button]
        void Rename()
        {
            name = $"When {WatchSource.Name} changed, set {dependentVariable.Name}";
        }
    }

    public abstract class AbstractVariableBindingEntry : MonoBehaviour, IGuidEntity
    {
        // public abstract string Name { get; }
        public abstract void Bind();
    }
}