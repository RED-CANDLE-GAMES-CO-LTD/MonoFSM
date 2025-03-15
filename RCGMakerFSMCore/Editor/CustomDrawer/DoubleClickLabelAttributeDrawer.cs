using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using UnityEngine;

//why T?
public class DoubleClickLabelAttributeDrawer<T> : OdinAttributeDrawer<DoubleClickLabelAttribute, T>
{
    private ActionResolver onChangeAction;
    private ActionResolver doubleClickAction; //雙擊label觸發事件，超讚
    private bool subscribedToOnUndoRedo;

    protected override void Initialize()
    {
        if (Attribute.InvokeOnUndoRedo)
        {
            Property.Tree.OnUndoRedoPerformed += new Action(OnUndoRedo);
            subscribedToOnUndoRedo = true;
        }

        doubleClickAction = ActionResolver.Get(Property, Attribute.ActionName);
        // Action<int> action = new Action<int>(this.TriggerAction);
        // this.ValueEntry.OnValueChanged += action;


        // if (this.Attribute.IncludeChildren || typeof (T).IsValueType)
        //     this.ValueEntry.OnChildValueChanged += action;
        // if (!this.Attribute.InvokeOnInitialize || this.onChangeAction.HasError)
        //     return;
        // onChangeAction.DoActionForAllSelectionIndices();
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        // if (this.onChangeAction.HasError)
        //     SirenixEditorGUI.ErrorMessageBox(this.onChangeAction.ErrorMessage);
        // label.text += "TTTT";
        CallNextDrawer(label);
        var labelRect = GUILayoutUtility.GetLastRect();

        //Double Click叫事件
        if (Event.current.clickCount == 2 && labelRect.Contains(Event.current.mousePosition))
            doubleClickAction.DoAction();
        // Debug.Log("Double CLicked");
    }

    private void OnUndoRedo()
    {
        // for (int selectionIndex = 0; selectionIndex < this.ValueEntry.ValueCount; ++selectionIndex)
        //     this.TriggerAction(selectionIndex);
    }

    // private void TriggerAction(int selectionIndex)
    // {
    //     var handler = this.ValueEntry.Values[0] as Component;
    //     var component = Property.ParentValues[0] as MonoBehaviour;
    //     handler.GetComponent<IReferenceTarget>().RefOwner = component;
    // } //=>  this.onChangeAction.DoAction(selectionIndex);

    public void Dispose()
    {
        if (!subscribedToOnUndoRedo)
            return;
        Property.Tree.OnUndoRedoPerformed -= new Action(OnUndoRedo);
    }


    private InspectorProperty baseMemberProperty;
    // private PropertyContext<InspectorProperty> globalSelectedProperty;
    // private InspectorProperty selectedProperty;
    // private Action<object, int> selectedIndexSetter;
    // GameObject parentGObj;
    // protected override void Initialize()
    // {
    //     // Debug.Log("Attribute"+Attribute);
    //     
    //     var isElement = this.Property.Parent != null && this.Property.Parent.ChildResolver is IOrderedCollectionResolver;
    //     bool isList = !isElement;
    //     var listProperty = isList ? this.Property : this.Property.Parent;
    //     this.baseMemberProperty = listProperty.FindParent(x => x.Info.PropertyType == PropertyType.Value, true);
    //     // this.globalSelectedProperty = this.baseMemberProperty.Context.GetGlobal("selectedIndex" + this.baseMemberProperty.GetHashCode(), (InspectorProperty)null);
    //     foreach (var VARIABLE in Property.ParentValues)
    //     {
    //         Debug.Log("AutoRefAttributeDrawer ParentValues"+VARIABLE);
    //     }
    //      
    //     if (isList)
    //     {
    //         // var parentType = this.baseMemberProperty.ParentValues[0].GetType();
    //         var component = this.baseMemberProperty.ParentValues[0] as Component;
    //         // if (component)
    //         //     parentGObj = component.gameObject;
    //         // var entry = Property.ValueEntry as Component;
    //         var refTarget = component.GetComponent<IReferenceTarget>();
    //         // refTarget.RefOwner = 
    //     }
    //     //TODO: 要分三種，單獨Property, List Property, List Element Property
    //     //List和自己都會判...?
    //     // var p = this.Property;
    //     // var childrenCount = this.Property.Children.Count;
    //     // var target = Property.GetComponent<IReferenceTarget>();
    //     // this.isElement = this.Property.Parent != null && this.Property.Parent.ChildResolver is IOrderedCollectionResolver;
    //     // Debug.Log("Property " + Property.Name + " is? " + isElement + " hasChildren?" + childrenCount + ",child Resolver:" + this.Property.ChildResolver);
    //     // var listProperty = isList ? this.Property : this.Property.Parent;
    //     // this.baseMemberProperty = listProperty.FindParent(x => x.Info.PropertyType == PropertyType.Value, true);
    //     // this.globalSelectedProperty = this.baseMemberProperty.Context.GetGlobal("selectedIndex" + this.baseMemberProperty.GetHashCode(), (InspectorProperty)null);
    // }


    // protected override void DrawPropertyLayout(GUIContent label)
    // {
    //     base.DrawPropertyLayout(label);
    //     
    //     // GUI.enabled = true;
    //     // var monoAttribute = Attribute;
    //     // var buttonStr = "";
    //     // var comp = Property.ValueEntry as Component;
    //     // if(comp != null)
    //     //     Debug.Log("AutoRefAttributeDrawer DrawPropertyLayout"+comp);
    //     // // if (monoAttribute.addAt == AddComponentAt.Parent)
    //     // // {
    //     // //     buttonStr = "Parent";
    //     // // }
    //     // // else if (monoAttribute.addAt == AddComponentAt.Children)
    //     // // {
    //     // //     buttonStr = "Child";
    //     // // }
    //     // // SirenixEditorGUI.BeginBox();
    //     // if (GUILayout.Button("新增" + buttonStr + ":" + Attribute))
    //     // {
    //     //     
    //     //     // var types = FindSubClassesOf(Attribute.baseType);
    //     //     // genericMenu.ShowAsContext();
    //     // }
    // }
}