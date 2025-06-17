using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MonoFSM;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using MonoFSM.Core;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;

/// <summary>
/// 在Field上生成Button, 顯示Select來讓User添增Component
/// </summary>
[UsedImplicitly]
[AllowGUIEnabledForReadonly]
[DrawerPriority(1, 100, 0)]
public class ComponentAttributeDrawer : OdinAttributeDrawer<ComponentAttribute>
{
    private InspectorProperty baseMemberProperty;
    private MonoBehaviour bindComp;
    private bool isArray => Property.ValueEntry != null ? Property.ValueEntry.TypeOfValue.IsArray : false;

    protected override bool CanDrawAttributeProperty(InspectorProperty property)
    {
        return true;
    }

    protected override void Initialize()
    {
        //TODO: 要分三種，單獨Property, List Property, List Element Property
        //List和自己都會判...?
        // var p = this.Property;
        // var childrenCount = this.Property.Children.Count;


        // this.isElement = this.Property.Parent != null && this.Property.Parent.ChildResolver is IOrderedCollectionResolver;
        // Debug.Log("Property " + Property.Name + " is? " + isElement + " hasChildren?" + childrenCount + ",child Resolver:" + this.Property.ChildResolver);
        // var listProperty = isArray ?   Property.Parent:Property;
        baseMemberProperty =
            Property.Parent; //listProperty.FindParent(x => x.Info.PropertyType == PropertyType.Value, true);
        // this.globalSelectedProperty = this.baseMemberProperty.Context.GetGlobal("selectedIndex" + this.baseMemberProperty.GetHashCode(), (InspectorProperty)null);
        // parentGObj = baseMemberProperty.ParentValues[0] as GameObject;

        // var myYype = baseMemberProperty.ValueEntry.TypeOfValue;

        if (isArray)
        {
            // var parentType = baseMemberProperty.ParentValues[0].GetType();
            bindComp = baseMemberProperty.ParentValues[0] as MonoBehaviour;
            // var component = baseMemberProperty.ParentValues[0] as Component;
            // if (component)
            //     parentGObj = component.gameObject;


            // Debug.Log("isArray parentGObj" + parentComp);
        }
        else
        {
            // Debug.Log(Property.Parent);
            // Debug.Log(Property.ParentValues[0]);
            // Debug.Log(Property.Parent.ValueEntry.WeakSmartValue);
            bindComp = Property.ParentValues[0] as MonoBehaviour;
            // Debug.Log("isArray false" + parentComp);
        }
    }
    // public IEnumerable<Type> FindSubClassesOf<TBaseType>()
    // {
    //     var baseType = typeof(TBaseType);
    //     var assembly = baseType.Assembly;
    //
    //     return assembly.GetTypes().Where(t => t.IsSubclassOf(baseType) || (t == baseType && t.IsAbstract == false));
    // }

    // private static IEnumerable<Type> FindSubClassesOf(Type type)
    // {
    //     var baseType = type;
    //     var assembly = baseType.Assembly;
    //     return assembly.GetTypes().Where(t => t.IsSubclassOf(baseType) || (t == type && t.IsAbstract == false));
    // }

    void ShowSelector(string buttonStr)
    {
        //FIXME: 用這個就夠了
        if (Property.ValueEntry == null)
        {
            // type = Property.ValueEntry.TypeOfValue;
            Debug.LogError("Property.ValueEntry is null" + Property);
            return;
        }


        var type = Property.ValueEntry.TypeOfValue;
        if (type.IsArray)
        {
            type = type.GetElementType();
        }
        //localization strings?
        // var style = new GUIStyle(EditorStyles.toolbarButton);
        if (SirenixEditorGUI.SDFIconButton("Search：Add" + buttonStr + ":" + type.Name, 16, SdfIconType.Plus))
        {
            // Debug.Log("Parent Value:" + baseMemberProperty.ParentValues[0]);
            var selector = new ComponentTypeSelector(type);
            // selector.EnableSingleClickTselector.EnableSingleClickToConfirm();oSelect();
            // selector.SelectionChanged += col => { Debug.Log("SelectionChanged" + col.FirstOrDefault()); };
            selector.SelectionConfirmed += col =>
            {
                // Debug.Log(col);
                // Debug.Log(col.FirstOrDefault());
                var firstOrDefault = col.FirstOrDefault();
                if (buttonStr == "Parent")
                {
                    //add a new parent transform

                    var name = firstOrDefault.Name;
                    if (!Attribute.nameTag.IsNullOrWhitespace())
                        name = Attribute.nameTag + " " + firstOrDefault.Name; //FIXME: 重新命名的客製function?
                    var newParent = new GameObject(name);
                    newParent.transform.position = bindComp.transform.position;
                    newParent.transform.SetParent(bindComp.transform.parent);
                    newParent.transform.SetSiblingIndex(bindComp.transform.GetSiblingIndex());
                    newParent.transform.localScale = bindComp.transform.localScale;
                    newParent.transform.rotation = bindComp.transform.rotation;
                    Undo.RegisterCreatedObjectUndo(newParent, "Add Parent Component" + firstOrDefault.Name);
                    newParent.transform.AddComp(firstOrDefault);
                    bindComp.transform.SetParent(newParent.transform);
                    Selection.activeGameObject = newParent;
                }

                else if (buttonStr == "Child")
                    AddChildComp(firstOrDefault);
                else
                {
                    bindComp.AddComp(firstOrDefault);
                }
            };

            selector.EnableSingleClickToConfirm();
            selector.ShowInPopup();
        }
    }

    private void AddChildComp(Type type)
    {
        if (bindComp == null)
        {
            Debug.LogError("Parent GameObject is null");
            return;
        }

        if (type == null)
        {
            Debug.LogError("Type is null");
            return;
        }

        var name = type.Name;
        if (!Attribute.nameTag.IsNullOrWhitespace())
            name = Attribute.nameTag + " " + type.Name;
        var comp = bindComp.gameObject.AddChildrenComponent(type, name);

        //[]: 如果是單一Property，就直接設定值, 可以倒過來綁回對應的property，雙邊互綁
        //array會auto自動抓，也不需要add
        if (!isArray)
            Property.ValueEntry.WeakSmartValue = comp;
        Selection.activeGameObject = comp.gameObject;
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        // GUI.enabled = true;
        var monoAttribute = Attribute;
        var buttonStr = "";
        var autoAttribute = Property.GetAttribute<AutoAttribute>();

        //有點太囉唆了，addAt應該可以拿掉
        if (autoAttribute != null)
        {
            buttonStr = "Auto";
        }
        else if (Property.GetAttribute<AutoParentAttribute>() != null)
        {
            buttonStr = "Parent";
        }
        else if (monoAttribute.addAt == AddComponentAt.Parent)
        {
            buttonStr = "Parent";
        }
        else if (monoAttribute.addAt == AddComponentAt.Same)
        {
            buttonStr = "Same";
        }
        else if (monoAttribute.addAt == AddComponentAt.Children)
        {
            buttonStr = "Child";
        }
        else if (Property.GetAttribute<AutoChildrenAttribute>() != null)
        {
            buttonStr = "Children";
        }
        //check if it has AutoAttribute


        // else if (monoAttribute.addAt == AddComponentAt.Children)
        // {
        //     buttonStr = "Child";
        // }

        SirenixEditorGUI.BeginBox();
        // if (monoAttribute.IsDisplayProperty) //什麼時候不要display? 還是我以前都放在function上？
        // if (Property.ValueEntry != null) //掛在function上...好像不是很好，怎麼樣都該有array接著？
        var isFunction = Property.ValueEntry == null; //&& Property.ValueEntry.TypeOfValue.IsValueType == false &&
        //                  Property.ValueEntry.TypeOfValue.IsArray == false;
        if (!isFunction)
        {
            CallNextDrawer(label);
        }

        if (Property.ValueEntry != null && !isArray && (UnityEngine.Object)Property.ValueEntry.WeakSmartValue != null)
        {
            //單一Property有值，就不要顯示了
        }
        else
        {
            var lastEnabled = GUI.enabled;
            if (lastEnabled == false) GUI.enabled = true;
            ShowSelector(buttonStr);
            GUI.enabled = lastEnabled;
        }

        SirenixEditorGUI.EndBox();
    }
}