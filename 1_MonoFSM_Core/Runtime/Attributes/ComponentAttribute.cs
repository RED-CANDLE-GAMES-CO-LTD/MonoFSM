using System;
using System.Diagnostics;

using Sirenix.OdinInspector;
using MonoFSM.Core.Attributes;

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
[PreviewInInspector]
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

    public string nameTag;
    public AddComponentAt addAt; //FIXME: 這個應該要自己判斷？如果遇到Auto就放在同一層
}