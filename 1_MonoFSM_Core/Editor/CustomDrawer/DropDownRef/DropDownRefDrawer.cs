#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MonoFSM.Core;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;


[UsedImplicitly]
[DrawerPriority(0.0, 2.0, 0.25)]
// Place the drawer script file in an Editor folder or wrap it in a #if UNITY_EDITOR condition.
public class DropDownRefAttributeDrawer : OdinAttributeDrawer<DropDownRefAttribute>
{
    private ValueResolver<object> rawGetterDynamicType; //動態拿到type
    private Func<Type> getterDynamicType;

    private InspectorProperty baseMemberProperty;
    private Component _bindComp;
    private bool isArray => Property.ValueEntry != null ? Property.ValueEntry.TypeOfValue.IsArray : false;

    private bool _isValueDropDownAttribute;
    private bool _isInlineEditor;

    protected override void Initialize()
    {
        rawGetterDynamicType = ValueResolver.Get<object>(Property, Attribute._dynamicTypeGetter);
        getterDynamicType = () =>
        {
            object source = rawGetterDynamicType.GetValue();
            return source as Type;
        };


        _isValueDropDownAttribute = Property.GetAttribute<ValueDropdownAttribute>() != null;
        _isInlineEditor = Property.GetAttribute<InlineEditorAttribute>() != null;
        baseMemberProperty =
            Property.Parent;
        if (isArray)
        {
            _bindComp = baseMemberProperty.SerializationRoot.ParentValues[0] as Component;
        }
        else
        {
            _bindComp = Property.SerializationRoot.ParentValues[0] as Component;
        }

        if (_bindComp == null)
            throw new ArgumentNullException(nameof(Property.ParentValues));
    }

    public override bool CanDrawTypeFilter(Type type)
    {
        return true;
    }

    void ShowSelector()
    {
        //直接用property原本宣告的type來做filter
        //fixme: 可以filter某一部分？
        var filterType = Property.ValueEntry.BaseValueType; //FIXME: Base Type?
        if (getterDynamicType() != null)
        {
            // Debug.Log("getterDynamicType():" + getterDynamicType());
            filterType = getterDynamicType();
        }

        if (filterType.IsArray)
        {
            filterType = filterType.GetElementType();
        }
        var currentComp = Property.ValueEntry.WeakSmartValue as Component;
        //draw SDFIcon down arrow to the right of the button
        var buttonText = currentComp ? currentComp.name : "None";
        if (SirenixEditorGUI.SDFIconButton(buttonText, 16, SdfIconType.CaretDownFill, IconAlignment.RightEdge))
            // if (GUILayout.Button())
        {
            // Debug.Log("Parent Value:" + baseMemberProperty.ParentValues[0]);
            // Debug.Log("BindComp:" + _bindComp+"type"+type);
            var selector = new DropDownRefCompSelector(_bindComp, filterType, Attribute);
            // selector.EnableSingleClickTselector.EnableSingleClickToConfirm();oSelect();
            // selector.SelectionChanged += col => { Debug.Log("SelectionChanged" + col.FirstOrDefault()); };
            
            selector.SelectionConfirmed += col =>
            {
                // Debug.Log(col);

                // var type = col.FirstOrDefault();
                Property.ValueEntry.WeakSmartValue = col.FirstOrDefault();
            };

            selector.EnableSingleClickToConfirm();
            selector.ShowInPopup();
        }
    }

    private GUIContent label;

    // private OdinSelector<object> ShowSelector(Rect rect)
    // {
    //     // GenericSelector<object> selector = this.CreateSelector();
    //     rect.x = (float) (int) rect.x;
    //     rect.y = (float) (int) rect.y;
    //     rect.width = (float) (int) rect.width;
    //     rect.height = (float) (int) rect.height;
    //     // if (this.Attribute.AppendNextDrawer && !this.isList)
    //         rect.xMax = GUIHelper.GetCurrentLayoutRect().xMax;
    //     // selector.ShowInPopup(rect, new Vector2((float) this.Attribute.DropdownWidth, (float) this.Attribute.DropdownHeight));
    //     // return (OdinSelector<object>) selector;
    // }
    void AppendNextDrawer()
    {
        IEnumerable<object> objects;
        GUILayout.BeginHorizontal();
        float width = 15f;
        if (this.label != null)
            width += GUIHelper.BetterLabelWidth;
        GUIContent btnLabel = GUIHelper.TempContent("");
        if (this.Property.Info.TypeOfValue == typeof(System.Type))
            btnLabel.image = (Texture)GUIHelper.GetAssetThumbnail((UnityEngine.Object)null,
                this.Property.ValueEntry.WeakSmartValue as System.Type, false);
        var OnlyChangeValueOnConfirm = true;
        // objects = OdinSelector<object>.DrawSelectorDropdown(this.label, btnLabel, new Func<Rect, OdinSelector<object>>(this.ShowSelector), !this.OnlyChangeValueOnConfirm, GUIStyle.none, (GUILayoutOption[]) GUILayoutOptions.Width(width));
        if (Event.current.type == UnityEngine.EventType.Repaint)
        {
            Rect position = GUILayoutUtility.GetLastRect().AlignRight(15f);
            position.y += 4f;
            SirenixGUIStyles.PaneOptions.Draw(position, GUIContent.none, 0);
        }
        // GUILayout.BeginVertical();
        // bool inAppendedDrawer = true;
        // if (inAppendedDrawer)
        //     GUIHelper.PushGUIEnabled(false);
        // this.CallNextDrawer((GUIContent) null);
        // if (inAppendedDrawer)
        //     GUIHelper.PopGUIEnabled();
        // GUILayout.EndVertical();
        // GUILayout.EndHorizontal();
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        SirenixEditorGUI.BeginBox();
        //特規，客製寫拿Selection的方法
        // Debug.Log("DropDownRefAttributeDrawer:" + Property.ValueEntry.BaseValueType);
        if (_isValueDropDownAttribute)
        {
            CallNextDrawer(label);
        }
        else
        {
            // EditorGUILayout.BeginHorizontal();
            var widthRatio = 1f; //0.75f
            // var option = GUILayout.Width(EditorGUIUtility.currentViewWidth * widthRatio);
            // CallNextDrawer(label);
            // SirenixEditorGUI.BeginInlineBox();
            // AppendNextDrawer();
            GUILayout.BeginHorizontal();
            // CallNextDrawer(label);
            if (label != null)
                GUILayout.Label(label, GUILayout.Width(EditorGUIUtility.labelWidth * widthRatio));
            ShowSelector();
            GUILayout.EndHorizontal();
        }


        // SirenixEditorGUI.EndBox();
        // var labelRect = GUILayoutUtility.GetRect(label, null); // GetLastRect();
        var labelRect = GUILayoutUtility.GetLastRect();
        labelRect.width /= 2;
        //Double Click叫事件
        //ping?
        var target = Property.ValueEntry.WeakSmartValue as UnityEngine.Object;

        if (target != null && labelRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.clickCount == 1)
                EditorGUIUtility.PingObject(target);
            if (Event.current.clickCount == 3)
                Selection.activeObject = target;
        }

        // if (_isInlineEditor)
        //     CallNextDrawer(label);
        // else //這個可以拿掉？
        // {
        GUI.backgroundColor = Property.ValueEntry.WeakSmartValue == null
            ? new Color(0.2f, 0.2f, 0.3f, 0.1f)
            : new Color(0.35f, 0.3f, 0.1f, 0.2f);

        var newObj = SirenixEditorFields.UnityObjectField(
            Property.ValueEntry.WeakSmartValue as UnityEngine.Object,
            Property.ValueEntry.TypeOfValue, true); //GUILayout.Width(EditorGUIUtility.currentViewWidth) 這個會太肥噴掉
        if (newObj == _bindComp)
            // Debug.Log("newObj == Property.ParentValues[0]");
            Debug.LogError("newObj == Property.ParentValues[0], this should not happen, please check your code.");
        else
            Property.ValueEntry.WeakSmartValue = newObj;
        GUI.backgroundColor = Color.white;
        // }

        // }
        SirenixEditorGUI.EndBox();
    }
}

#endif