using System;
using UnityEngine;

namespace MonoFSM.Variable.TypeTag
{
    [CreateAssetMenu(fileName = "NewSOType", menuName = "MonoFSM/Variable/SOType")]
    public class MonoTypeTag : AbstractTypeTag<MonoBehaviour> //這個感覺有點討厭？
    {
    }

    public abstract class AbstractTypeTag<T> : AbstractTypeTag
    {
        public MySerializedType<T> _type;
        public override Type Type => _type.RestrictType; //這樣就可以直接拿到Type了
    }

    public abstract class AbstractTypeTag : ScriptableObject
    {
        public abstract Type Type { get; }
    }
}