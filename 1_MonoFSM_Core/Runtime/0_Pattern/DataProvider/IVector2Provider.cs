using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    public interface IVector2Provider : IValueProvider<Vector2> { }

    public interface IVector3Provider : IValueProvider<Vector3> { }

    //不需要純

    // [InlineProperty]
    // [Serializable]
    // public class VarVector2DropDownRef : IVector2Provider //FIXME: 改名成 DropdownVarVector2?
    // {
    //     [HideLabel]
    //     [DropDownRef]
    //     public VarVector2 _monoVar;
    //
    //     public Vector2 Value => _monoVar?.FinalValue ?? Vector2.zero;
    //
    //     public string Description => _monoVar?._varTag?.name;
    //
    //     public T GetValue<T>()
    //     {
    //         if (typeof(T) == typeof(Vector2)) return (T)(object)_monoVar?.FinalValue;
    //
    //         throw new InvalidCastException($"Cannot cast {typeof(float)} to {typeof(T)}");
    //     }
    //
    //     public Type ValueType => typeof(Vector2);
    //
    //     public string GetDescription()
    //     {
    //         return _monoVar?.FinalValue.ToString();
    //     }
    // }
}
