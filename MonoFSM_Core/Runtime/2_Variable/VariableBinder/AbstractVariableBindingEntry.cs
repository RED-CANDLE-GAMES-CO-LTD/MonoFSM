using UnityEngine;

using Sirenix.OdinInspector;

namespace MonoFSM.Variable.VariableBinder
{
    public interface IName
    {
        string Name { get; }
    }

    public abstract class VariableBindingEntry<T> : AbstractVariableBindingEntry where T : IName, IRebindable
    {
        //What is the term that two variables which one is dependent to another
        [DropDownRef]
        public T WatchSource;

        [DropDownRef]
        public T dependentVariable;

        [Button]
        private void Rename() 
            => name = $"When {WatchSource.Name} changed, set {dependentVariable.Name}";
    }

    public abstract class AbstractVariableBindingEntry : MonoBehaviour, IGuidEntity
    {
        public abstract void Bind();
    }
}