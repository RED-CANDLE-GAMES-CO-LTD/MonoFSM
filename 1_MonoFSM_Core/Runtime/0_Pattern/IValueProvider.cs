using System;

namespace MonoFSM.Core
{
    //IReferenceProvider?
    public interface IValueProvider //這個是不是太粗啊？
    {
        //FIXME: 這個有點討厭...
        // object GetValue { get; } //無法避免boxing, 不該存在？
        T Get<T>();

        Type ValueType { get; }

        //FIXME: 要在物件還沒拿到之前就知道型別？
        string Description { get; }
    }

    public interface ICompProvider<out T> : IValueProvider //where T : Component
    {
        // T1 IValueProvider.Get<T1>()
        // {
        //     if (typeof(T) != typeof(T1)) throw new InvalidCastException($"Cannot cast {typeof(T)} to {typeof(T)}");
        //     return (T1)(object)Get();
        // }
        T1 IValueProvider.Get<T1>()
        {
            var value = Get();
            if (value is T1 t1Value) return t1Value;
            throw new InvalidCastException($"Cannot cast {typeof(T)} to {typeof(T1)}");
        }

        T Get();
        // object IValueProvider.GetValue => Get<T>()<T>;


        Type IValueProvider.ValueType => typeof(T);
    }
}