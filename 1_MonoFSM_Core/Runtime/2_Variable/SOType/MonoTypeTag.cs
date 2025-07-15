using System;
using _1_MonoFSM_Core.Runtime._3_FlagData;
using MonoFSM.Core.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MonoFSM.Variable.TypeTag
{
    [CreateAssetMenu(fileName = "NewSOType", menuName = "MonoFSM/Variable/SOType")]
    public class MonoTypeTag : AbstractTypeTag<MonoBehaviour> //這個感覺有點討厭？
    {
        
    }

    public abstract class AbstractTypeTag<T> : AbstractTypeTag
    {
        // private void OnValidate()
        // {
        //     //reload domain時，會呼叫OnValidate?
        //     name = "[Type]" + _type.RestrictType.Name; //這樣就可以直接拿到Type的名稱了
        //     Debug.Log("TypeTag OnValidate: " + name);
        // }

        [InlineField]
        public MySerializedType<T> _type;
        public override Type Type => _type.RestrictType; //這樣就可以直接拿到Type了

        //存檔時，把name改成TypeName

        public override void OnBeforeSceneSave()
        {
#if UNITY_EDITOR
            //這個時候還沒存檔，還可以改
            var newname = "[Type] " + _type.RestrictType.Name; //這樣就可以直接拿到Type的名稱了
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(this), newname);
            Debug.Log("TypeTag OnBeforeSceneSave: " + newname);
#endif
        }
    }

    public abstract class AbstractTypeTag : MonoSOConfig
    {
        public abstract Type Type { get; }
    }
}