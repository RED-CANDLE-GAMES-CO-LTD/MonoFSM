using System;
using System.Diagnostics;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;


// using UnityEngine;
// using System.Collections.Generic;
// using Sirenix.OdinInspector.Editor;
// using Sirenix.Utilities.Editor;
// using UnityEditor;
// using System.Linq;
// using Sirenix.OdinInspector;
// using Sirenix.Utilities;

// Example component demonstating how new generic context menus can be created with drawers.

// public class GenericMenuExample : MonoBehaviour
// {
//     [ColorPicker]
//     public Color Color;
// }


// [DontApplyToListElements]
// public class MonoAttribute : PropertyAttribute
// {
//     public MonoAttribute(Type baseType, string name)
//     {
//         this.baseType = baseType;
//         nameTag = name;
//     }
//     public Type baseType;
//     public string nameTag;
// }


// The Color picker attribute.
//AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property

// namespace RCGMaker.Core
// {
public enum AddComponentAt
{
    Same,
    Children,
    Parent
}

//可以加某種類別 (繼承某個Abstract) 的元件到children或是Parent
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
[Conditional("UNITY_EDITOR")]
[IncludeMyAttributes]
[ShowInInspector]
//rename AddCompAttribute??
//FIXME: 好像不該能夠掛在function上？
public class ComponentAttribute : ShowInInspectorAttribute //ShowInInspectorAttribute很重要
{
    //TODO: only 1, 只需要一個而已
    //FIXME: 不需要baseType, 除非想要指定？？？ 直接看property就知道了
    public ComponentAttribute(AddComponentAt addAt = AddComponentAt.Children,
        string nameTag = "")
    {
        // this.baseType = baseType;
        this.nameTag = nameTag;
        this.addAt = addAt;
    }

    public bool IsDisplayProperty = false;

    public string nameTag;

    // public Type baseType;
    public AddComponentAt addAt; //FIXME: 這個應該要自己判斷？如果遇到Auto就放在同一層
}

// }