using System;
using System.Collections;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Attributes;
using MonoFSM.Variable;
using UnityEngine;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

//對外開放
public class InlineFavoriteComponent : MonoBehaviour, IEditorOnly
{
    // [DropZone]
    [InlineField] public QuickEntry[] favorites;

    // [DropZone] [InlineEditor] public Component[] favoriteComps;
    // [InlineEditor]
    // public List<Transform> transforms;

    // [InlineEditor]
    // public List<GameObject> gameObjs; //可能沒用啦
}

public interface IObjectReference
{
    Object EditorValue { get; set; }
    Type ObjectType { get; }
}


[System.Serializable]
public class QuickEntry
{
    //每個元件只需要expose一個欄位出來就好？
    // [InlineEditor]
    [TextArea] public string note;

    bool isISerializedFloatValue => comp is ISerializedFloatValue;

    [DropDownRef] public Component comp;

    [ShowInInspector]
    [ShowIf(nameof(isISerializedFloatValue))]
    public float FloatValue
    {
        get
        {
            if (comp is ISerializedFloatValue floatValue)
            {
                return floatValue.EditorValue;
            }

            return 0;
        }
        set
        {
            if (comp is ISerializedFloatValue floatValue)
            {
                floatValue.EditorValue = value;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(comp);
#endif
            }
        }
    }

    bool isCompObjectReference => comp is IObjectReference;

    Type GetObjectTypeFilter()
    {
        if (comp is IObjectReference reference)
        {
            return reference.ObjectType;
        }

        return typeof(Object);
    }

    [DropDownAsset(nameof(GetObjectTypeFilter))]
    [ShowInInspector]
    [ShowIf(nameof(isCompObjectReference))]
    public Object ObjectValue //FIXME: 型別沒辦法限制感覺也蠻難用的？還是要自己做下拉式選單
    {
        get
        {
            if (comp is IObjectReference)
            {
                return ((IObjectReference)comp).EditorValue;
            }

            return null;
        }
        set
        {
            if (comp is IObjectReference)
            {
                ((IObjectReference)comp).EditorValue = value;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(comp);
#endif
            }
        }
    }

    //怎麼依照comp的interface去撈到他的getter setter property?


    //用介面去撈一個Getter Setter Property來更動最重要的資料就好？
    //ref obj
    //int, float bool
    //string
    //塞模型的咧？塞Prefab放進來就塞到children?
}