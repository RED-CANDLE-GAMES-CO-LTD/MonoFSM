using System;
using UnityEngine;

namespace MonoFSM.Core
{
    //IReferenceProvider?
    public interface IValueProvider //這個是不是太粗啊？
    {
        //FIXME: 這個有點討厭...
        // object GetValue { get; } //無法避免boxing, 不該存在？
        T1 Get<T1>();

        Type ValueType { get; }

        //FIXME: 要在物件還沒拿到之前就知道型別？
        string Description { get; }
    }

    public interface ICompProvider : IValueProvider
    {
        T1 IValueProvider.Get<T1>() //繼承關係的
        {
            var value = Get();
            if (value is T1 t1Value) return t1Value;
            throw new InvalidCastException($"Cannot cast {typeof(Component)} to {typeof(T1)}");
        }

        Component Get();

        Type IValueProvider.ValueType => typeof(Component);
        // object IValueProvider.GetValue => Get<T>();
        // T1 IValueProvider.Get<T1>() => Get();
        // Type IValueProvider.ValueType => typeof(T);
    }

    // out T沒什麼意義...
    public interface ICompProvider<out T> : ICompProvider where T : Component
    {
        T1 IValueProvider.Get<T1>()
        {
            var value = Get();
            if (value is T1 t1Value) return t1Value;
            throw new InvalidCastException($"Cannot cast {typeof(T)} to {typeof(T1)}");
        }

        new T Get();

        Component ICompProvider.Get()
        {
            return Get();
            // 確保Get()返回Component類型
        }
        // object IValueProvider.GetValue => Get<T>()<T>;


        Type IValueProvider.ValueType => typeof(T);
    }
}