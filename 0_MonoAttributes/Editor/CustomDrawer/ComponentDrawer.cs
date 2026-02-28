using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MonoFSM.Core;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

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
    private List<Type> candidateTypes;
    private bool isArray =>
        Property.ValueEntry != null ? Property.ValueEntry.TypeOfValue.IsArray : false;

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
        baseMemberProperty = Property.Parent; //listProperty.FindParent(x => x.Info.PropertyType == PropertyType.Value, true);
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
            var p = Property.FindParent(x => x.ParentType.IsSubclassOf(typeof(MonoBehaviour)),
                true);
            // Debug.Log("Found Parent MonoBehaviour Property:" + p);
            // Debug.Log("Value:" + p.ParentValues[0]);
            bindComp = p.ParentValues[0] as MonoBehaviour;
        }

        // 快取候選類型
        if (Property.ValueEntry != null)
        {
            var type = Property.ValueEntry.TypeOfValue;
            if (type.IsArray)
                type = type.GetElementType();
            candidateTypes = type.FilterSubClassOrImplementationFromDomain()
                .Where(t => t.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
                .ToList();
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

        if (candidateTypes == null || candidateTypes.Count == 0) return;

        var buttonLabel = candidateTypes.Count == 1
            ? "Add " + buttonStr + ":" + candidateTypes[0].Name
            : "Search：Add" + buttonStr + ":" + Property.ValueEntry.TypeOfValue.Name;

        if (
            SirenixEditorGUI.SDFIconButton(
                buttonLabel,
                16,
                SdfIconType.Plus
            )
        )
        {
            // 只有一個候選，直接添加
            if (candidateTypes.Count == 1)
            {
                ConfirmSelection(candidateTypes[0], buttonStr);
            }
            else
            {
                var selector = new ComponentTypeSelector(Property.ValueEntry.TypeOfValue);
                selector.SelectionConfirmed += col =>
                {
                    var firstOrDefault = col.FirstOrDefault();
                    ConfirmSelection(firstOrDefault, buttonStr);
                };

                selector.EnableSingleClickToConfirm();
                selector.ShowInPopup();
            }
        }
    }

    private void ConfirmSelection(Type selectedType, string buttonStr)
    {
        if (selectedType == null) return;

        if (buttonStr == "Parent")
        {
            var name = selectedType.Name;
            if (!Attribute.nameTag.IsNullOrWhitespace())
                name = Attribute.nameTag + " " + selectedType.Name;
            var newParent = new GameObject(name);
            newParent.transform.position = bindComp.transform.position;
            newParent.transform.SetParent(bindComp.transform.parent);
            newParent.transform.SetSiblingIndex(bindComp.transform.GetSiblingIndex());
            newParent.transform.localScale = bindComp.transform.localScale;
            newParent.transform.rotation = bindComp.transform.rotation;
            Undo.RegisterCreatedObjectUndo(
                newParent,
                "Add Parent Component" + selectedType.Name
            );
            newParent.transform.AddComp(selectedType);
            bindComp.transform.SetParent(newParent.transform);
            Selection.activeGameObject = newParent;
        }
        else if (buttonStr == "Child")
            AddChildComp(selectedType);
        else
        {
            bindComp.AddComp(selectedType);
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

        if (
            Property.ValueEntry != null
            && !isArray
            && (UnityEngine.Object)Property.ValueEntry.WeakSmartValue != null
        )
        {
            //單一Property有值，就不要顯示了
        }
        else
        {
            var lastEnabled = GUI.enabled;
            if (lastEnabled == false)
                GUI.enabled = true;
            ShowSelector(buttonStr);
            GUI.enabled = lastEnabled;
        }

        SirenixEditorGUI.EndBox();
    }
}
